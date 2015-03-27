using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Reflection.Emit;
using MySql.Data.MySqlClient;
using System.Data.Common;

namespace DBWizard
{
	/// <summary>
	/// Represents an Object map storing information on how to serialize a given Object into the database.
	/// </summary>
	internal class CObjectMap
	{
		private enum EFieldType
		{
			none = 0,

			primitive = 10,

			one_to_one = 11,
			one_to_many = 12,
			many_to_many = 13,
		}

		private static Dictionary<Type, CObjectMap> _s_p_object_maps = new Dictionary<Type, CObjectMap>();
		/// <summary>
		/// Constructs or returns a previously constructed Object map for the given type.
		/// </summary>
		/// <param name="p_type">The type to construct or return the Object map from.</param>
		/// <returns>The Object map associated with the given type.</returns>
		internal static CObjectMap Get(Type p_type)
		{
			CObjectMap p_obj_map;
			if (_s_p_object_maps.TryGetValue(p_type, out p_obj_map)) return p_obj_map;

			p_obj_map = new CObjectMap();
			_s_p_object_maps.Add(p_type, p_obj_map);
			p_obj_map.Initialize(p_type);

			return p_obj_map;
		}

		internal static List<CObjectMap> GetExistingSubClasses(Type p_type)
		{
			List<CObjectMap> p_existing_sub_classes = new List<CObjectMap>();
			foreach (KeyValuePair<Type, CObjectMap> object_map_entry in _s_p_object_maps)
			{
				if (object_map_entry.Key.IsSubclassOf(p_type))
				{
					p_existing_sub_classes.Add(object_map_entry.Value);
				}
			}
			return p_existing_sub_classes;
		}

		internal static void ThrowOnIncompatibility(CDataBase p_data_base)
		{
			foreach (CObjectMap p_obj in _s_p_object_maps.Values)
			{
				foreach (SStorePrimitiveOptions primitive_options in p_obj.m_p_primitives_map.Values)
				{
					if (!p_data_base.m_p_supported_primitives.Contains(primitive_options.m_primitive_type))
					{
						throw new Exception("The primitive type \"" + primitive_options.m_primitive_type.ToString() + "\" used by type \"" + p_obj.m_p_object_type.FullName + "\" for field \"" + primitive_options.m_p_field.Name + "\" is not supported by database driver type \"" + p_data_base.m_driver_type.ToString() + "\".");
					}
				}
			}
		}

		/// <summary>
		/// The name of the table the Object map uses.
		/// </summary>
		internal String m_p_object_table { get; private set; }

		internal Type m_p_object_type { get; private set; }

		// set of primitives, set of relations:
		// primitives => in-place
		// single-relations => in-place reference
		// 1:many-relations => referenced directly
		// many:many-relations => referenced via relation table

		// question: how should completed_quest_offers.player_id be represented?
		// it's an external key that exists only if there is a player to associate the completed_quest_offer with
		// this means that the generated method needs to refer to both the player and the completed quest offer
		// => could either get the CPlayer instance (bad; the pk might be a private field)
		// => or could get the CDataBaseObject derived from the CPlayer instance (avoids the private field issue, but still doesnt feel good)
		// => or could get only the set of required knowledge (the value that associates back to the CPlayer Object)

		// 1:many => results in an SObjectLink linking the "this" map and the map of the target objects by a foreign key
		// many:many => results in an SObjectLink linking the "this" map and the map of the relation table by a foreign key,
		// whereas the relation tables map will yet again link from itself to the many target objects with an SObjectLink.

		// I need to save the attributes m_p_value_name
		// I cannot really save it in the SObjectLink since the way these are created is kind of random depending on what attribute exactly created it
		// 

		internal List<SStorePrimitiveOptions> m_p_unique_keys { get; private set; }
		internal Dictionary<String, SStorePrimitiveOptions> m_p_primitives_map { get; private set; }
		internal List<SObjectLink> m_p_object_links { get; private set; }
		internal List<String> m_p_linked_values_names { get; private set; }
		internal Dictionary<String, MethodInfo> m_p_user_load_callbacks { get; private set; }
		internal Dictionary<String, MethodInfo> m_p_user_save_callbacks { get; private set; }

		internal DynamicMethod m_p_map_to_method { get; private set; }
		internal DynamicMethod m_p_map_from_method { get; private set; }
		internal DynamicMethod m_p_assign_auto_increment_method { get; private set; }

		internal Action<Object> m_p_begin_load_call_back { get; private set; }
		internal Action<Object> m_p_end_load_call_back { get; private set; }
		internal Action<Object> m_p_begin_delete_call_back { get; private set; }
		internal Action<Object> m_p_end_save_call_back { get; private set; }
		
		/// <summary>
		/// Constructs an empty Object map.
		/// </summary>
		private CObjectMap()
		{
			m_p_unique_keys = new List<SStorePrimitiveOptions>();
			m_p_primitives_map = new Dictionary<String, SStorePrimitiveOptions>();
			m_p_object_links = new List<SObjectLink>();
			m_p_linked_values_names = new List<String>();
			m_p_user_load_callbacks = new Dictionary<String, MethodInfo>();
			m_p_user_save_callbacks = new Dictionary<String, MethodInfo>();
		}

		private String[] GetPrimaryKeyColumnNames()
		{
			return (from primary_key in m_p_unique_keys select primary_key.m_p_column_name).ToArray();
		}

		// I really hate this monolithic method... plzfix
		private void Initialize(Type p_type)
		{
			// retrieves Object table name and actual used type
			InitializeTypeRoot(ref p_type);

			BindingFlags binding_flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

			List<MethodInfo> p_method_infos = p_type.GetAllMethods(binding_flags);
			InitializeUserLoadCallBacks(p_method_infos); // figures out what functions the user wants to use as load-callbacks
			InitializeUserSaveCallBacks(p_method_infos);
			InitializeUserBeginLoadCallBack(p_method_infos); // triggers for the user
			InitializeUserEndLoadCallBack(p_method_infos);
			InitializeUserBeginSaveCallBack(p_method_infos);
			InitializeUserEndSaveCallBack(p_method_infos);
			InitializePrimitiveTiedMethods(p_method_infos); // figures out what functions the user wants to use as primitive-retrieval callbacks

			List<FieldInfo> p_field_infos = p_type.GetAllFields(binding_flags);
			Dictionary<EFieldType, List<FieldInfo>> p_field_type_dict = ScanFieldAttributes(p_field_infos);
			List<FieldInfo> p_primitive_fields = p_field_type_dict[EFieldType.primitive];
			List<FieldInfo> p_one_to_one_fields = p_field_type_dict[EFieldType.one_to_one];
			List<FieldInfo> p_one_to_many_fields = p_field_type_dict[EFieldType.one_to_many];
			List<FieldInfo> p_many_to_many_fields = p_field_type_dict[EFieldType.many_to_many];

			InitializePrimitiveFields(p_primitive_fields);
			InitializeOneToOneRelations(p_one_to_one_fields);
			InitializeOneToManyRelations(p_one_to_many_fields);
			InitializeManyToManyRelations(p_many_to_many_fields);

			GenerateMappingDelegates();
		}
		private void InitializeTypeRoot(ref Type p_type)
		{
			StoreAttributes.CTableAttribute p_store_table_attribute = p_type.GetCustomAttribute<StoreAttributes.CTableAttribute>();

			if (p_store_table_attribute == null && p_type.IsGenericType)
			{
				Type p_result;
				if (p_type.GetEnumeratedType(out p_result))
				{
					p_type = p_result; // use enumerated type
					p_store_table_attribute = p_type.GetCustomAttribute<StoreAttributes.CTableAttribute>();
				}
			}
			if (p_store_table_attribute == null)
			{
				throw new ArgumentException("Cannot create an Object map for type " + p_type.FullName + " because it has no table attribute.");
			}

			m_p_object_table = p_store_table_attribute.m_p_table_name;
			m_p_object_type = p_type;
		}
		private void InitializeUserLoadCallBacks(List<MethodInfo> p_available_methods)
		{
			for (Int32 i = 0; i < p_available_methods.Count; ++i)
			{
				MethodInfo p_method_info = p_available_methods[i];
				
				List<StoreAttributes.CUserLoadCallBackAttribute> p_user_load_callback_attributes =
					(from p_custom_attribute in p_method_info.GetCustomAttributes()
					 where p_custom_attribute is StoreAttributes.CUserLoadCallBackAttribute
					 select (StoreAttributes.CUserLoadCallBackAttribute)p_custom_attribute
					).ToList();

				for (Int32 j = 0; j < p_user_load_callback_attributes.Count; ++j)
				{
					String p_value_name = p_user_load_callback_attributes[j].m_p_value_name;
					if (m_p_user_load_callbacks.ContainsKey(p_value_name))
					{
						throw new Exception(
							"You can only specify one user-load callback per value name. You have specified multiple callbacks for the value name \"" + p_value_name + "\"."
						);
					}
					m_p_user_load_callbacks.Add(
						p_value_name,
						p_method_info
					);
				}
			}
		}
		private void InitializeUserSaveCallBacks(List<MethodInfo> p_available_methods)
		{
			for (Int32 i = 0; i < p_available_methods.Count; ++i)
			{
				MethodInfo p_method_info = p_available_methods[i];

				List<StoreAttributes.CUserSaveCallBackAttribute> p_user_save_callback_attributes =
					(from p_custom_attribute in p_method_info.GetCustomAttributes()
					 where p_custom_attribute is StoreAttributes.CUserSaveCallBackAttribute
					 select (StoreAttributes.CUserSaveCallBackAttribute)p_custom_attribute
					).ToList();

				for (Int32 j = 0; j < p_user_save_callback_attributes.Count; ++j)
				{
					String p_value_name = p_user_save_callback_attributes[j].m_p_value_name;
					if (m_p_user_save_callbacks.ContainsKey(p_value_name))
					{
						throw new Exception(
							"You can only specify one user-save callback per value name. You have specified multiple callbacks for the value name \"" + p_value_name + "\"."
						);
					}
					m_p_user_save_callbacks.Add(
						p_value_name,
						p_method_info
					);
				}
			}
		}
		private void InitializeUserBeginLoadCallBack(List<MethodInfo> p_available_methods)
		{
			for (Int32 i = 0; i < p_available_methods.Count; ++i)
			{
				MethodInfo p_method_info = p_available_methods[i];

				List<StoreAttributes.CBeginLoadCallBackAttribute> p_user_begin_load_callback_attributes = p_method_info.GetAttributes<StoreAttributes.CBeginLoadCallBackAttribute>();
				if (p_user_begin_load_callback_attributes.Count > 0)
				{
					if(p_method_info.GetParameters().Length != 0 || p_method_info.ReturnType != typeof(void))
					{
						throw new Exception("A method marked with the CBeginLoadCallBack attribute must not take any parameters and must return no value.");
					}
					if (m_p_begin_load_call_back != null)
					{
						throw new Exception("You may only mark one method with a CBeginLoadCallBack attribute per type. You marked more than one method in type " + m_p_object_type.FullName + ".");
					}
					Delegate p_delegate = p_method_info.CreateDelegate(typeof(Action<>).MakeGenericType(m_p_object_type));
					m_p_begin_load_call_back = (Object p_obj) => p_delegate.DynamicInvoke(p_obj);
				}
			}
		}
		private void InitializeUserEndLoadCallBack(List<MethodInfo> p_available_methods)
		{
			for (Int32 i = 0; i < p_available_methods.Count; ++i)
			{
				MethodInfo p_method_info = p_available_methods[i];

				List<StoreAttributes.CEndLoadCallBackAttribute> p_user_end_load_callback_attributes = p_method_info.GetAttributes<StoreAttributes.CEndLoadCallBackAttribute>();
				if (p_user_end_load_callback_attributes.Count > 0)
				{
					if (p_method_info.GetParameters().Length != 0 || p_method_info.ReturnType != typeof(void))
					{
						throw new Exception("A method marked with the CEndLoadCallBackAttribute attribute must not take any parameters and must return no value.");
					}
					if (m_p_begin_load_call_back != null)
					{
						throw new Exception("You may only mark one method with a CEndLoadCallBackAttribute attribute per type. You marked more than one method in type " + m_p_object_type.FullName + ".");
					}
					Delegate p_delegate = p_method_info.CreateDelegate(typeof(Action<>).MakeGenericType(m_p_object_type));
					m_p_end_load_call_back = (Object p_obj) => p_delegate.DynamicInvoke(p_obj);
				}
			}
		}
		private void InitializeUserBeginSaveCallBack(List<MethodInfo> p_available_methods)
		{
			for (Int32 i = 0; i < p_available_methods.Count; ++i)
			{
				MethodInfo p_method_info = p_available_methods[i];

				List<StoreAttributes.CBeginSaveCallBackAttribute> p_user_begin_load_callback_attributes = p_method_info.GetAttributes<StoreAttributes.CBeginSaveCallBackAttribute>();
				if (p_user_begin_load_callback_attributes.Count > 0)
				{
					if (p_method_info.GetParameters().Length != 0 || p_method_info.ReturnType != typeof(void))
					{
						throw new Exception("A method marked with the CBeginSaveCallBackAttribute attribute must not take any parameters and must return no value.");
					}
					if (m_p_begin_load_call_back != null)
					{
						throw new Exception("You may only mark one method with a CBeginSaveCallBackAttribute attribute per type. You marked more than one method in type " + m_p_object_type.FullName + ".");
					}
					Delegate p_delegate = p_method_info.CreateDelegate(typeof(Action<>).MakeGenericType(m_p_object_type));
					m_p_begin_delete_call_back = (Object p_obj) => p_delegate.DynamicInvoke(p_obj);
				}
			}
		}
		private void InitializeUserEndSaveCallBack(List<MethodInfo> p_available_methods)
		{
			for (Int32 i = 0; i < p_available_methods.Count; ++i)
			{
				MethodInfo p_method_info = p_available_methods[i];

				List<StoreAttributes.CEndSaveCallBackAttribute> p_user_end_load_callback_attributes = p_method_info.GetAttributes<StoreAttributes.CEndSaveCallBackAttribute>();
				if (p_user_end_load_callback_attributes.Count > 0)
				{
					if (p_method_info.GetParameters().Length != 0 || p_method_info.ReturnType != typeof(void))
					{
						throw new Exception("A method marked with the CEndSaveCallBackAttribute attribute must not take any parameters and must return no value.");
					}
					if (m_p_begin_load_call_back != null)
					{
						throw new Exception("You may only mark one method with a CEndSaveCallBackAttribute attribute per type. You marked more than one method in type " + m_p_object_type.FullName + ".");
					}
					Delegate p_delegate = p_method_info.CreateDelegate(typeof(Action<>).MakeGenericType(m_p_object_type));
					m_p_end_save_call_back = (Object p_obj) => p_delegate.DynamicInvoke(p_obj);
				}
			}
		}
		private void InitializePrimitiveTiedMethods(List<MethodInfo> p_available_methods)
		{
			for (Int32 i = 0; i < p_available_methods.Count; ++i)
			{
				MethodInfo p_method_info = p_available_methods[i];
				
				List<StoreAttributes.CPrimitiveAttribute> p_primitive_attributes =
						(from p_custom_attribute in p_method_info.GetCustomAttributes()
							where p_custom_attribute is StoreAttributes.CPrimitiveAttribute
							select (StoreAttributes.CPrimitiveAttribute)p_custom_attribute
						 ).ToList();

				for (Int32 j = 0; j < p_primitive_attributes.Count; ++j)
				{
					StoreAttributes.CPrimitiveAttribute p_store_primitive_attribute = p_primitive_attributes[j];

					SStorePrimitiveOptions primitive_options = new SStorePrimitiveOptions(
						p_store_primitive_attribute.m_p_column_name,
						p_store_primitive_attribute.m_primitive_type,
						null,
						p_method_info,
						p_store_primitive_attribute.m_auto_convert,
						p_store_primitive_attribute.m_store_options
					);
					if (primitive_options.m_store_options != EStoreOptions.user_callback)
					{
						throw new Exception("Primitives that are attached to methods must be specified as being stored by a user-callback. This is not the case for primitive named \"" + primitive_options.m_p_column_name + "\" in type " + m_p_object_type.FullName + ".");
					}
					m_p_primitives_map.Add(
						p_store_primitive_attribute.m_p_column_name,
						primitive_options
					);

					if (p_method_info.GetCustomAttribute<StoreAttributes.CUniqueKeyAttribute>() != null)
					{
						AddPrimaryKey(p_method_info.GetCustomAttribute<StoreAttributes.CUniqueKeyAttribute>(), primitive_options);
					}
				}
			}
		}
		private void AddPrimaryKey(StoreAttributes.CUniqueKeyAttribute p_attribute, SStorePrimitiveOptions primitive)
		{
			if (p_attribute.m_is_identity)
			{
				for (Int32 i = 0; i < m_p_unique_keys.Count; ++i)
				{
					if (m_p_unique_keys[i].m_p_field.GetCustomAttribute<StoreAttributes.CUniqueKeyAttribute>().m_is_identity)
					{
						throw new Exception("You have multiple identity values in type " + m_p_object_type.FullName);
					}
				}
				primitive.m_is_identity = true;
			}
			m_p_unique_keys.Add(primitive);
		}
		private void InitializePrimitiveFields(List<FieldInfo> p_primitive_fields)
		{
			for (Int32 i = 0; i < p_primitive_fields.Count; ++i)
			{
				FieldInfo p_field_info = p_primitive_fields[i];
				StoreAttributes.CPrimitiveAttribute p_store_primitive_attribute = p_field_info.GetCustomAttribute<StoreAttributes.CPrimitiveAttribute>();

				EDBPrimitive primitive_type = p_store_primitive_attribute.m_primitive_type;
				if (primitive_type == EDBPrimitive.infer)
				{
					if (!p_field_info.FieldType.ToDBPrimitive(out primitive_type))
					{
						throw new Exception("Database Primitive type cannot be automatically inferred for field \"" + p_field_info.Name + "\" in type \"" + m_p_object_type.FullName + "\". Please provide a database primitive type using a different overload of the constructor of the primitive attribute.");
					}
				}
				SStorePrimitiveOptions primitive_options = new SStorePrimitiveOptions(
					p_store_primitive_attribute.m_p_column_name,
					primitive_type,
					p_field_info,
					null,
					p_store_primitive_attribute.m_auto_convert,
					p_store_primitive_attribute.m_store_options
				);
				if (primitive_options.m_store_options == EStoreOptions.user_callback)
				{
					if (!m_p_user_load_callbacks.ContainsKey(primitive_options.m_p_column_name))
					{
						throw new Exception("You did not provide a user-callback that handles loading data of column \"" + primitive_options.m_p_column_name + "\" for type \"" + m_p_object_type.FullName + "\".");
					}
				}
				m_p_primitives_map.Add(
					p_store_primitive_attribute.m_p_column_name,
					primitive_options
				);

				if (p_field_info.GetCustomAttribute<StoreAttributes.CUniqueKeyAttribute>() != null)
				{
					AddPrimaryKey(p_field_info.GetCustomAttribute<StoreAttributes.CUniqueKeyAttribute>(), primitive_options);
				}
			}
		}
		private void InitializeOneToOneRelations(List<FieldInfo> p_relation_fields)
		{
			for (Int32 i = 0; i < p_relation_fields.Count; ++i)
			{
				FieldInfo p_field_info = p_relation_fields[i];
				CObjectMap p_target_map = CObjectMap.Get(p_field_info.FieldType);

				StoreAttributes.COneToOneAttribute p_store_one_to_one_attribute = p_field_info.GetCustomAttribute<StoreAttributes.COneToOneAttribute>();

				String[] p_target_column_names = p_store_one_to_one_attribute.m_p_foreign_column_names.ToArray();
				if (p_target_column_names.Length == 0)
				{
					p_target_column_names = p_target_map.GetPrimaryKeyColumnNames();
				}
				String[] p_source_column_names = p_store_one_to_one_attribute.m_p_source_column_names.ToArray();
				if (p_target_column_names.Length != p_source_column_names.Length)
				{
					throw new Exception(p_target_column_names.Length.ToString() + " foreign columns cannot be projected onto " + p_source_column_names.Length.ToString() + " source columns.");
				}

				for (Int32 j = 0; j < p_target_column_names.Length; ++j)
				{
					String p_foreign_column_name = p_target_column_names[j];


					SStorePrimitiveOptions target_primitive;
					if (!p_target_map.m_p_primitives_map.TryGetValue(p_foreign_column_name, out target_primitive))
					{
						throw new Exception("The foreign table does not contain a column named \"" + p_foreign_column_name + "\".");
					}
					SStorePrimitiveOptions source_primitive = new SStorePrimitiveOptions(
						p_source_column_names[j],
						target_primitive.m_primitive_type,
						null, // one-to-one "primtive" doesn't map to a field or method in the class!
						null,
						target_primitive.m_auto_convert,
						EStoreOptions.none
					);

					// only create new if primitive has not yet been explicitly created.
					if (!m_p_primitives_map.ContainsKey(p_source_column_names[j]))
					{
						m_p_primitives_map.Add(
							p_source_column_names[j],
							source_primitive
						);
					}

					if (p_field_info.GetCustomAttribute<StoreAttributes.CUniqueKeyAttribute>() != null)
					{
						AddPrimaryKey(p_field_info.GetCustomAttribute<StoreAttributes.CUniqueKeyAttribute>(), source_primitive);
					}
				}

				if (!m_p_user_load_callbacks.ContainsKey(p_store_one_to_one_attribute.m_p_value_name))
				{
					throw new Exception("You did not provide a user-callback that handles loading data of relation \"" + p_store_one_to_one_attribute.m_p_value_name + "\".");
				}

				m_p_linked_values_names.Add(p_store_one_to_one_attribute.m_p_value_name);

				m_p_object_links.Add(
					new SObjectLink(
						EObjectLinkType.one_to_one,
						p_field_info,
						p_target_map.m_p_object_type,
						new CForeignKey(
							m_p_object_table,
							p_source_column_names,
							p_target_map.m_p_object_table,
							p_target_column_names
						),
						this,
						p_target_map
					)
				);
			}
		}
		private void InitializeOneToManyRelations(List<FieldInfo> p_relation_fields)
		{
			for (Int32 i = 0; i < p_relation_fields.Count; ++i)
			{
				FieldInfo p_field_info = p_relation_fields[i];
				StoreAttributes.COneToManyAttribute p_store_one_to_many_attribute = p_field_info.GetCustomAttribute<StoreAttributes.COneToManyAttribute>();

				CObjectMap p_target_map;
				if (p_store_one_to_many_attribute.m_p_target_type == null)
				{
					p_target_map = CObjectMap.Get(p_field_info.FieldType);
				}
				else
				{
					p_target_map = CObjectMap.Get(p_store_one_to_many_attribute.m_p_target_type);
				}


				String[] p_source_columns = p_store_one_to_many_attribute.m_p_linked_columns.m_p_source_columns.ToArray();
				if (p_source_columns.Length == 1 && p_source_columns[0] == null)
				{
					p_source_columns = GetPrimaryKeyColumnNames();
				}
				String[] p_target_columns = p_store_one_to_many_attribute.m_p_linked_columns.m_p_target_columns.ToArray();
				if (p_target_columns.Length == 1 && p_target_columns[0] == null)
				{
					p_target_columns = p_target_map.GetPrimaryKeyColumnNames();
				}
				if (p_source_columns.Length != p_target_columns.Length)
				{
					throw new Exception(
						"The amount of source and target columns must be equal. " +
						"This is not the case for the relation \"" + p_store_one_to_many_attribute.m_p_value_name + "\" " +
						"from table " + m_p_object_table + " to " + p_target_map.m_p_object_table + "."
					);
				}

				for (Int32 j = 0; j < p_source_columns.Length; ++j)
				{
					if (!m_p_primitives_map.ContainsKey(p_source_columns[j]))
					{
						throw new Exception(
							"The source table \"" + m_p_object_table + "\" does not contain a column \"" + p_source_columns[j] + "\" to create a one-to-many relation with."
						);
					}
					else if (!p_target_map.m_p_primitives_map.ContainsKey(p_target_columns[j]))
					{
						throw new Exception(
							"The target table \"" + p_target_map.m_p_object_table + "\" does not contain a column \"" + p_target_columns[j] + "\" to create a one-to-many relation with."
						);
					}
				}

				if (!m_p_user_load_callbacks.ContainsKey(p_store_one_to_many_attribute.m_p_value_name))
				{
					throw new Exception("You did not provide a user-callback that handles loading data of relation \"" + p_store_one_to_many_attribute.m_p_value_name + "\".");
				}
				m_p_linked_values_names.Add(p_store_one_to_many_attribute.m_p_value_name);

				m_p_object_links.Add(
					new SObjectLink(
						EObjectLinkType.one_to_many,
						p_field_info,
						p_target_map.m_p_object_type,
						new CForeignKey(
							m_p_object_table,
							p_source_columns,
							p_target_map.m_p_object_table,
							p_target_columns
						),
						this,
						p_target_map
					)
				);
			}
		}
		private void InitializeManyToManyRelations(List<FieldInfo> p_relation_fields)
		{
			for (Int32 i = 0; i < p_relation_fields.Count; ++i)
			{
				FieldInfo p_field_info = p_relation_fields[i];

				StoreAttributes.CManyToManyAttribute p_store_many_to_many_attribute = p_field_info.GetCustomAttribute<StoreAttributes.CManyToManyAttribute>();

				CObjectMap p_target_map = CObjectMap.Get(p_field_info.FieldType);

				CForeignKey p_source_to_relation_link = p_store_many_to_many_attribute.m_p_source_to_relation_link;
				CForeignKey p_relation_to_target_link = p_store_many_to_many_attribute.m_p_relation_to_target_link;

				CObjectMap p_relation_map = new CObjectMap();
				p_relation_map.m_p_object_table = p_source_to_relation_link.m_p_destination_table;

				String[] p_source_columns = p_source_to_relation_link.m_p_source_columns.ToArray();
				String[] p_target_columns = p_relation_to_target_link.m_p_source_columns.ToArray();
				if (p_source_columns.Length == 1 && p_source_columns[0] == null)
				{
					p_source_columns = GetPrimaryKeyColumnNames();
					p_target_columns = p_target_map.GetPrimaryKeyColumnNames();
				}

				for (Int32 j = 0; j < p_source_columns.Length; ++j)
				{
					SStorePrimitiveOptions primitive_options;
					if (!m_p_primitives_map.TryGetValue(p_source_columns[j], out primitive_options))
					{
						throw new Exception(
							"The source table \"" + m_p_object_table + "\" does not contain a column \"" + p_source_columns[j] + "\" to create a many-to-many relation with."
						);
					}
					p_relation_map.m_p_primitives_map.Add(
						p_source_columns[j],
						primitive_options
					);
					p_relation_map.AddPrimaryKey(new StoreAttributes.CUniqueKeyAttribute(false), primitive_options);
				}
				for (Int32 j = 0; j < p_target_columns.Length; ++j)
				{
					SStorePrimitiveOptions primitive_options;
					if (!p_target_map.m_p_primitives_map.TryGetValue(p_target_columns[j], out primitive_options))
					{
						throw new Exception(
							"The target table \"" + p_target_map.m_p_object_table + "\" does not contain a column \"" + p_target_columns[j] + "\" to create a many-to-many relation with."
						);
					}
					p_relation_map.m_p_primitives_map.Add(
						p_target_columns[j],
						primitive_options
					);
					p_relation_map.AddPrimaryKey(new StoreAttributes.CUniqueKeyAttribute(false), primitive_options);
				}

				if (!m_p_user_load_callbacks.ContainsKey(p_store_many_to_many_attribute.m_p_value_name))
				{
					throw new Exception("You did not provide a user-callback that handles loading data of relation \"" + p_store_many_to_many_attribute.m_p_value_name + "\".");
				}
				m_p_linked_values_names.Add(p_store_many_to_many_attribute.m_p_value_name);

				m_p_object_links.Add(
					new SObjectLink(
						EObjectLinkType.many_to_many,
						p_field_info,
						p_target_map.m_p_object_type,
						new CForeignKey(
							m_p_object_table,
							p_source_columns,
							p_relation_map.m_p_object_table,
							p_source_columns
						),
						this,
						p_relation_map
					)
				);
				p_relation_map.m_p_object_links.Add(
					new SObjectLink(
						EObjectLinkType.many_to_many,
						null,
						p_target_map.m_p_object_type,
						new CForeignKey(
							p_relation_map.m_p_object_table,
							p_target_columns,
							p_target_map.m_p_object_table,
							p_target_columns
						),
						p_relation_map,
						p_target_map
					)
				);
				p_relation_map.m_p_linked_values_names.Add(p_store_many_to_many_attribute.m_p_value_name);
			}
		}
		private Dictionary<EFieldType, List<FieldInfo>> ScanFieldAttributes(List<FieldInfo> p_available_fields)
		{
			List<FieldInfo> p_primitive_fields = new List<FieldInfo>();
			List<FieldInfo> p_one_to_one_fields = new List<FieldInfo>();
			List<FieldInfo> p_one_to_many_fields = new List<FieldInfo>();
			List<FieldInfo> p_many_to_many_fields = new List<FieldInfo>();

			Dictionary<EFieldType, List<FieldInfo>> p_dict = new Dictionary<EFieldType,List<FieldInfo>>()
			{
				{ EFieldType.primitive, p_primitive_fields },
				{ EFieldType.one_to_one, p_one_to_one_fields },
				{ EFieldType.one_to_many, p_one_to_many_fields },
				{ EFieldType.many_to_many, p_many_to_many_fields },
			};
			for (Int32 i = 0; i < p_available_fields.Count; ++i)
			{
				FieldInfo p_field_info = p_available_fields[i];

				// TODO: Think about error reporting
				List<StoreAttributes.CPrimitiveAttribute> p_primitive_attributes =
					(from p_custom_attribute in p_field_info.GetCustomAttributes()
					 where p_custom_attribute is StoreAttributes.CPrimitiveAttribute
					 select (StoreAttributes.CPrimitiveAttribute)p_custom_attribute
					 ).ToList();
				List<StoreAttributes.COneToOneAttribute> p_one_to_one_attributes =
					(from p_custom_attribute in p_field_info.GetCustomAttributes()
					 where p_custom_attribute is StoreAttributes.COneToOneAttribute
					 select (StoreAttributes.COneToOneAttribute)p_custom_attribute
					 ).ToList();
				List<StoreAttributes.COneToManyAttribute> p_one_to_many_attributes =
					(from p_custom_attribute in p_field_info.GetCustomAttributes()
					 where p_custom_attribute is StoreAttributes.COneToManyAttribute
					 select (StoreAttributes.COneToManyAttribute)p_custom_attribute
					 ).ToList();
				List<StoreAttributes.CManyToManyAttribute> p_many_to_many_attributes =
					(from p_custom_attribute in p_field_info.GetCustomAttributes()
					 where p_custom_attribute is StoreAttributes.CManyToManyAttribute
					 select (StoreAttributes.CManyToManyAttribute)p_custom_attribute
					 ).ToList();
				Int32 n_store_attributes = (p_primitive_attributes.Count + p_one_to_many_attributes.Count + p_one_to_one_attributes.Count + p_many_to_many_attributes.Count);
				if (n_store_attributes > 1)
				{
					throw new Exception("One field can only be assigned to one primitive or one relation.");
				}

				List<StoreAttributes.CUniqueKeyAttribute> p_primary_key_attributes =
					(from p_custom_attribute in p_field_info.GetCustomAttributes()
					 where p_custom_attribute is StoreAttributes.CUniqueKeyAttribute
					 select (StoreAttributes.CUniqueKeyAttribute)p_custom_attribute
					 ).ToList();
				Boolean is_primary_key = p_primitive_attributes.Count == 1;
				if (p_primary_key_attributes.Count > 1)
				{
					throw new Exception("A Primary key attribute can only be specified once per field.");
				}
				else if (n_store_attributes == 0 && p_primary_key_attributes.Count > 0)
				{
					throw new Exception("A Primary key attribute cannot be applied to a field without store attributes.");
				}

				if (p_primitive_attributes.Count > 0)
				{
					p_primitive_fields.Add(p_field_info);
				}
				if (p_one_to_one_attributes.Count > 0)
				{
					p_one_to_one_fields.Add(p_field_info);
				}
				if (p_one_to_many_attributes.Count > 0)
				{
					p_one_to_many_fields.Add(p_field_info);
				}
				if (p_many_to_many_attributes.Count > 0)
				{
					p_many_to_many_fields.Add(p_field_info);
				}
			}
			return p_dict;
		}

		private void GenerateMappingDelegates()
		{
			// void Deserialize(<Object-type> p_obj, CDataBaseRow p_row);
			m_p_map_to_method = GenerateMapToMethod();
			m_p_map_from_method = GenerateMapFromMethod();
			m_p_assign_auto_increment_method = GenerateAssignAutoIncrementMethod();
		}
		private DynamicMethod GenerateMapFromMethod()
		{
			BindingFlags binding_flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

			MethodInfo p_set_primitive_throw_method = typeof(CDataBaseObject).GetMethod("SetPrimitiveThrow", binding_flags);
			MethodInfo p_set_object_method = typeof(CDataBaseObject).GetMethod("SetObject", binding_flags);
			MethodInfo p_set_objects_method = typeof(CDataBaseObject).GetMethod("SetObjects", binding_flags);
			MethodInfo p_set_db_objects_method = typeof(CDataBaseObject).GetMethod("SetDBObjects", binding_flags);
			MethodInfo p_get_method = typeof(CDataBaseObject).GetMethod("get_Item", binding_flags);

			DynamicMethod p_serialize = new DynamicMethod("__DBWizardAutoSerialize", typeof(void), new Type[] { typeof(CDataBaseObject), m_p_object_type, typeof(Dictionary<Object, CDataBaseObject>) }, m_p_object_type, true);
			ILGenerator p_gen = p_serialize.GetILGenerator();
			p_gen.DeclareLocal(typeof(Exception));

			foreach (KeyValuePair<String, SStorePrimitiveOptions> primitive_entry in m_p_primitives_map)
			{
				SStorePrimitiveOptions store_options = primitive_entry.Value;
				FieldInfo p_field_info = store_options.m_p_field;
				// primitives that do not map to a field can be ignored here, since this is only dealing with setting internal state.
				

				// in case of user-callback: p_db_obj.SetPrimitive("<column-name>", p_obj.<user-callback>(p_obj.<field-name>));
				// more simply:
				// var temp = p_obj.<user-callback>(p_obj.<field-name>);
				// p_db_obj.SetPrimitive("<column-name>", p_obj.<field-name>);
				if (store_options.m_store_options == EStoreOptions.direct_assignment)
				{
					if (p_field_info == null)
					{
						continue;
					}
					p_gen.Emit(OpCodes.Ldarg_0);
					p_gen.Emit(OpCodes.Ldstr, store_options.m_p_column_name);
					p_gen.Emit(OpCodes.Ldarg_1);
					p_gen.Emit(OpCodes.Ldfld, p_field_info);
					p_gen.Emit(OpCodes.Box, p_field_info.FieldType);
					p_gen.Emit(OpCodes.Callvirt, p_set_primitive_throw_method);
				}
				else if(store_options.m_store_options == EStoreOptions.user_callback)
				{
					MethodInfo p_method_info;
					if (!m_p_user_save_callbacks.TryGetValue(store_options.m_p_column_name, out p_method_info))
					{
						throw new Exception("You did not define a save callback for column " + store_options.m_p_column_name + ". In Type " + m_p_object_type.FullName);
					}
					ParameterInfo[] p_params = p_method_info.GetParameters();
					if (p_params.Length != 0 ||
						(
						 p_method_info.ReturnType != store_options.m_primitive_type.ToType() && 
						 p_method_info.ReturnType != typeof(Nullable<>).MakeGenericType(store_options.m_primitive_type.ToType())
						)
					)
					{
						throw new Exception(
							"A user-defined callback to save primitive-data must have a return value matching the primitive type and take no arguments. " +
							"Method " + p_method_info.Name + " in " + m_p_object_type.FullName + " must return a value of type " + store_options.m_primitive_type.ToType().Name + " and take no arguments " + store_options.m_primitive_type.ToType().ToString() + "."
						);
					}

					// verify that the user-save callback has the signature:
					// <Required-Primitive-Type> MethodName(<field-type> param0)

					// p_db_obj.SetPrimitive("<column-name>", p_obj.<user-callback>(p_obj.<field-name>));
					p_gen.BeginExceptionBlock();
					p_gen.Emit(OpCodes.Ldarg_0);
					p_gen.Emit(OpCodes.Ldstr, store_options.m_p_column_name);

					p_gen.Emit(OpCodes.Ldarg_1);
					p_gen.Emit(OpCodes.Callvirt, m_p_user_save_callbacks[store_options.m_p_column_name]);
					

					p_gen.Emit(OpCodes.Box, store_options.m_primitive_type.ToType());
					p_gen.Emit(OpCodes.Callvirt, p_set_primitive_throw_method);

					p_gen.BeginCatchBlock(typeof(Exception));
					p_gen.Emit(OpCodes.Stloc_0);
					p_gen.Emit(OpCodes.Ldstr, "Exception occured in User Save Callback " + m_p_user_save_callbacks[store_options.m_p_column_name].Name + ".");
					p_gen.Emit(OpCodes.Ldloc_0);
					p_gen.Emit(OpCodes.Newobj, typeof(CObjectInitializationException).GetConstructor(new Type[] { typeof(String), typeof(Exception) }));
					p_gen.Emit(OpCodes.Throw);
					p_gen.EndExceptionBlock();
				}
			}
			for (Int32 i = 0; i < m_p_object_links.Count; ++i)
			{
				String p_value_name = m_p_linked_values_names[i];
				FieldInfo p_field_info = m_p_object_links[i].m_p_field;


				MethodInfo p_save_callback;
				if (m_p_user_save_callbacks.TryGetValue(p_value_name, out p_save_callback))
				{
					ParameterInfo[] p_params = p_save_callback.GetParameters();
					if (p_params.Length != 0 || p_save_callback.ReturnType != typeof(List<>).MakeGenericType(m_p_object_links[i].m_p_target_type))
					{
						throw new Exception("The user-defined save callback for relation \"" + p_value_name + "\" must take no parameters and return a value of type List<" + m_p_object_links[i].m_p_target_type.FullName + ">.");
					}
					p_gen.BeginExceptionBlock();
					p_gen.Emit(OpCodes.Ldarg_0); // Stack = {CDataBaseObject}
					p_gen.Emit(OpCodes.Ldarg_1); // Stack = {CDataBaseObject,<user-object>}
					p_gen.Emit(OpCodes.Callvirt, p_save_callback);		// Stack = {CDataBaseObject,List<CDataBaseObject>}
					p_gen.Emit(OpCodes.Ldstr, p_value_name); // Stack = {CDataBaseObject,List<CDataBaseObject>,String}
					p_gen.Emit(OpCodes.Ldarg_2); // Stack = {CDataBaseObject,List<CDataBaseObject>,String,Dictionary<Object,CDataBaseObject>}
					MethodInfo p_generic_set_objects = p_set_objects_method.MakeGenericMethod(m_p_object_links[i].m_p_target_type);
					p_gen.Emit(OpCodes.Call, p_generic_set_objects);	// Stack = {}
					p_gen.BeginCatchBlock(typeof(Exception));
					p_gen.Emit(OpCodes.Stloc_0);
					p_gen.Emit(OpCodes.Ldstr, "Exception occured in User Save Callback " + p_save_callback.Name + ".");
					p_gen.Emit(OpCodes.Ldloc_0);
					p_gen.Emit(OpCodes.Newobj, typeof(CObjectInitializationException).GetConstructor(new Type[] { typeof(String), typeof(Exception) }));
					p_gen.Emit(OpCodes.Throw);
					p_gen.EndExceptionBlock();
				}
				else
				{
					p_gen.Emit(OpCodes.Ldarg_0); // Stack = {CDataBaseObject}
					p_gen.Emit(OpCodes.Ldarg_1); // Stack = {CDataBaseObject,<user-object>}
					p_gen.Emit(OpCodes.Ldfld, p_field_info);			// Stack = {CDataBaseObject,<user-field>}
					p_gen.Emit(OpCodes.Box, p_field_info.FieldType);	// Stack = {CDataBaseObject,Object(<user-field>)}
					p_gen.Emit(OpCodes.Ldstr, p_value_name);			// Stack = {CDataBaseObject,Object(<user-field>),"<value-name>"}
					p_gen.Emit(OpCodes.Ldarg_2); // Stack = {CDataBaseObject,List<CDataBaseObject>,"<value-name>",Dictionary<Object,CDataBaseObject>}
					if (p_field_info.FieldType == m_p_object_links[i].m_p_target_map.m_p_object_type) // type is stored directly
					{
						// p_db_obj.SetObject(p_obj.<field-name>, "<value-name>");
						p_gen.Emit(OpCodes.Call, p_set_object_method);
					}
					else // type is stored in enumerable
					{
						MethodInfo p_generic_set_objects_method = p_set_objects_method.MakeGenericMethod(m_p_object_links[i].m_p_target_map.m_p_object_type);
						// p_db_obj.SetObjects(p_obj.<field-name>, "<value-name>");
						p_gen.Emit(OpCodes.Call, p_generic_set_objects_method);
					}
				}
			}

			p_gen.Emit(OpCodes.Ret);

			return p_serialize;
		}
		private DynamicMethod GenerateMapToMethod()
		{
			MethodInfo p_get_method = typeof(CDataBaseObject).GetMethod("get_Item");

			Type p_ref_type = m_p_object_type;
			if (!p_ref_type.IsClass)
			{
				p_ref_type = p_ref_type.MakeByRefType();
			}
			DynamicMethod p_map_to_method = new DynamicMethod("__DBWizardAutoDeserialize", null, new Type[] { p_ref_type, typeof(CDataBaseObject) }, m_p_object_type, true);
			
			ILGenerator p_gen = p_map_to_method.GetILGenerator();
			p_gen.DeclareLocal(typeof(Exception));

			// generates MSIL code that assigns all primitives on the Object.
			foreach (KeyValuePair<String, SStorePrimitiveOptions> primitive_entry in m_p_primitives_map)
			{
				SStorePrimitiveOptions store_options = primitive_entry.Value;
				FieldInfo p_field_info = store_options.m_p_field;
				if (store_options.m_store_options == EStoreOptions.none)
				{
					continue;
				}
				// primitives that do not map to a field can be ignored here, since this is only dealing with setting internal state.

				// <field-type> thing = (<field-type>)p_obj["<column-name>"];

				Type p_target_type;
				switch (store_options.m_store_options)
				{
					case EStoreOptions.direct_assignment:
						if (p_field_info == null) continue;
						p_target_type = p_field_info.FieldType;
						break;
					case EStoreOptions.user_callback:
						p_target_type = store_options.m_primitive_type.ToType();

						MethodInfo p_method_info;
						if (!m_p_user_load_callbacks.TryGetValue(store_options.m_p_column_name, out p_method_info))
						{
							throw new Exception("You did not define a load callback for column " + store_options.m_p_column_name + ". In Type " + m_p_object_type.FullName);
						}
						ParameterInfo[] p_params = p_method_info.GetParameters();
						Boolean has_parameter = p_params.Length == 1;
						if (!has_parameter)
						{
							throw new Exception(
								"A user-defined callback to load primitive-data must not have a return value and take a single argument of the type matching the primitive type. " +
								"Method " + p_method_info.Name + " in " + m_p_object_type.FullName + " must not return a value and take a single parameter of type " + store_options.m_primitive_type.ToType().ToString()
							);
						}
						Boolean is_primitive_type = p_params[0].ParameterType == store_options.m_primitive_type.ToType();
						if (!is_primitive_type)
						{
							Boolean is_primitive_nullable_type = p_params[0].ParameterType == typeof(Nullable<>).MakeGenericType(store_options.m_primitive_type.ToType());
							if(!is_primitive_nullable_type)
							{
								throw new Exception(
									"A user-defined callback to load primitive-data must not have a return value and take a single argument of the type matching the primitive type. " +
									"Method " + p_method_info.Name + " in " + m_p_object_type.FullName + " must not return a value and take a single parameter of type " + store_options.m_primitive_type.ToType().ToString()
								);
							}
							else
							{
								p_target_type = typeof(Nullable<>).MakeGenericType(store_options.m_primitive_type.ToType());
							}
						}
						if(p_method_info.ReturnType != typeof(void))
						{
							throw new Exception(
								"A user-defined callback to load primitive-data must not have a return value and take a single argument of the type matching the primitive type. " +
								"Method " + p_method_info.Name + " in " + m_p_object_type.FullName + " must not return a value and take a single parameter of type " + store_options.m_primitive_type.ToType().ToString()
							);
						}

						break;
					default:
						continue;
				}

				if (store_options.m_store_options == EStoreOptions.user_callback)
				{
					p_gen.BeginExceptionBlock();
				}
				p_gen.Emit(OpCodes.Ldarg_0);
				p_gen.Emit(OpCodes.Ldarg_1);
				p_gen.Emit(OpCodes.Ldstr, primitive_entry.Value.m_p_column_name);
				p_gen.Emit(OpCodes.Call, p_get_method);
				if (p_target_type.IsValueType)
				{
					p_gen.Emit(OpCodes.Unbox_Any, p_target_type);
				}
				else
				{
					p_gen.Emit(OpCodes.Castclass, p_target_type);
				}

				if (store_options.m_store_options == EStoreOptions.direct_assignment)
				{
					if (store_options.m_primitive_type.ToType() != p_field_info.FieldType)
					{
						if (typeof(Nullable<>).MakeGenericType(store_options.m_primitive_type.ToType()) != p_field_info.FieldType)
						{
							throw new Exception("The field-type must match the primitive-type that should assign to it. Field " + p_field_info.Name + " must be of type " + store_options.m_primitive_type.ToType().ToString());
						}
					}

					// p_a.<field-name> = thing;
					p_gen.Emit(OpCodes.Stfld, p_field_info); // field has to match type of the primitive
				}
				else if (store_options.m_store_options == EStoreOptions.user_callback)
				{

					// p_a.<user-callback>(thing);

					p_gen.Emit(OpCodes.Callvirt, m_p_user_load_callbacks[store_options.m_p_column_name]); // method parameter has to match type of the primitive
					p_gen.BeginCatchBlock(typeof(Exception));
					p_gen.Emit(OpCodes.Stloc_0);
					p_gen.Emit(OpCodes.Ldstr, "Exception occured in User Load Callback " + m_p_user_load_callbacks[store_options.m_p_column_name].Name + ".");
					p_gen.Emit(OpCodes.Ldloc_0);
					p_gen.Emit(OpCodes.Newobj, typeof(CObjectInitializationException).GetConstructor(new Type[] { typeof(String), typeof(Exception) }));
					p_gen.Emit(OpCodes.Throw);
					p_gen.EndExceptionBlock();
				}
			}
			for (Int32 i = 0; i < m_p_object_links.Count; ++i)
			{
				String p_value_name = m_p_linked_values_names[i];
				ParameterInfo[] p_params = m_p_user_load_callbacks[p_value_name].GetParameters();
				if (p_params.Length != 1 ||
					p_params[0].ParameterType != typeof(List<CDataBaseObject>) ||
					m_p_user_load_callbacks[p_value_name].ReturnType != typeof(void)
				)
				{
					throw new Exception("A user-defined callback to load Object-data must not have a return value and take a single argument of type List<CDataBaseObject>.");
				}

				p_gen.BeginExceptionBlock();
				p_gen.Emit(OpCodes.Ldarg_0);
				p_gen.Emit(OpCodes.Ldarg_1);
				p_gen.Emit(OpCodes.Ldstr, p_value_name);
				p_gen.Emit(OpCodes.Call, p_get_method);
				p_gen.Emit(OpCodes.Castclass, typeof(List<CDataBaseObject>));

				p_gen.Emit(OpCodes.Call, m_p_user_load_callbacks[p_value_name]); 
				p_gen.BeginCatchBlock(typeof(Exception));
				p_gen.Emit(OpCodes.Stloc_0);
				p_gen.Emit(OpCodes.Ldstr, "Exception occured in User Load Callback " + m_p_user_load_callbacks[p_value_name].Name + ".");
				p_gen.Emit(OpCodes.Ldloc_0);
				p_gen.Emit(OpCodes.Newobj, typeof(CObjectInitializationException).GetConstructor(new Type[] { typeof(String), typeof(Exception) }));
				p_gen.Emit(OpCodes.Throw);
				p_gen.EndExceptionBlock();
			}

			p_gen.Emit(OpCodes.Ret);
			return p_map_to_method;
		}
		private DynamicMethod GenerateAssignAutoIncrementMethod()
		{
			IEnumerable<SStorePrimitiveOptions> p_auto_inc_enum = (from primary_key in m_p_unique_keys where primary_key.m_p_field.GetCustomAttribute<StoreAttributes.CUniqueKeyAttribute>().m_is_identity select primary_key);
			
			DynamicMethod p_assign_auto_increment_method = new DynamicMethod("__DBWizardUpdateAutoIncrement", null, new Type[] { m_p_object_type, typeof(Int64) }, m_p_object_type, true);
			ILGenerator p_gen = p_assign_auto_increment_method.GetILGenerator();
			if (p_auto_inc_enum.Count() == 0)
			{
				p_gen.Emit(OpCodes.Ret);
				return p_assign_auto_increment_method;
			}
			SStorePrimitiveOptions auto_increment_store_options = p_auto_inc_enum.First();

			// generates MSIL code that assigns the auto incrementing primitive on the Object.
			
			SStorePrimitiveOptions store_options = auto_increment_store_options;
			FieldInfo p_field_info = store_options.m_p_field;
			if (p_field_info == null)
			{
				throw new Exception("Auto-increment primitives must be stored in fields. Auto-increment primitive named \"" + store_options.m_p_column_name + "\" in type " + m_p_object_type.FullName + " is not associated with a field.");
			}

			if (store_options.m_store_options == EStoreOptions.direct_assignment)
			{
				if (store_options.m_primitive_type.ToType() != p_field_info.FieldType)
				{
					throw new Exception("The field-type must match the primitive-type that should assign to it. Field " + p_field_info.Name + " must be of type " + store_options.m_primitive_type.ToType().ToString());
				}

				// p_a.<field-name> = last_inserted_id;
				p_gen.Emit(OpCodes.Ldarg_0);
				p_gen.Emit(OpCodes.Ldarg_1);
				if (p_field_info.FieldType == typeof(Byte))
				{
					p_gen.Emit(OpCodes.Conv_U1);
				}
				else if (p_field_info.FieldType == typeof(SByte))
				{
					p_gen.Emit(OpCodes.Conv_I1);
				}
				else if (p_field_info.FieldType == typeof(UInt16))
				{
					p_gen.Emit(OpCodes.Conv_U2);
				}
				else if (p_field_info.FieldType == typeof(Int16))
				{
					p_gen.Emit(OpCodes.Conv_I2);
				}
				else if (p_field_info.FieldType == typeof(UInt32))
				{
					p_gen.Emit(OpCodes.Conv_U4);
				}
				else if (p_field_info.FieldType == typeof(Int32))
				{
					p_gen.Emit(OpCodes.Conv_I4);
				}
				else if (p_field_info.FieldType == typeof(UInt64))
				{
					p_gen.Emit(OpCodes.Conv_U8);
				}
				else if (p_field_info.FieldType == typeof(Int64))
				{
					p_gen.Emit(OpCodes.Conv_I8);
				}
				p_gen.Emit(OpCodes.Stfld, p_field_info); // field has to match type of the primitive
			}
			else if (store_options.m_store_options == EStoreOptions.user_callback)
			{
				MethodInfo p_method_info = m_p_user_load_callbacks[store_options.m_p_column_name];
				ParameterInfo[] p_params = p_method_info.GetParameters();
				if (p_params.Length != 1 ||
					p_params[0].ParameterType != store_options.m_primitive_type.ToType() ||
					p_method_info.ReturnType != typeof(void)
				)
				{
					throw new Exception(
						"A user-defined callback to load auto-increment values must not have a return value and take a single argument of type System.Int64 (aka long). " +
						"Method " + p_method_info.Name + " must not return a value and take a single parameter of type System.Int64 (long)."
					);
				}

				// p_a.<user-callback>(thing);
				p_gen.Emit(OpCodes.Ldarg_0);
				p_gen.Emit(OpCodes.Ldarg_1);
				p_gen.Emit(OpCodes.Call, m_p_user_load_callbacks[store_options.m_p_column_name]); // method parameter has to match type of the primitive
			}

			p_gen.Emit(OpCodes.Ret);
			return p_assign_auto_increment_method;
		}

		internal CDBWizardStatus LoadObject(CDataBase p_data_base, SQL.CWhereCondition p_condition, CDataBaseObject p_db_obj)
		{
			List<CDataBaseObject> p_objs = new List<CDataBaseObject>();
			CDBWizardStatus p_status = LoadObject(p_data_base, p_condition, new Dictionary<CDataBaseObject, CDataBaseObject>(), p_objs);
			if (p_status.IsError)
			{
				p_db_obj = null;
				return p_status;
			}
			if (p_objs.Count == 0)
			{
				p_db_obj = null;
				return new CDBWizardStatus(EDBWizardStatusCode.err_no_object_found);
			}
			else if (p_objs.Count > 1)
			{
				p_db_obj = null;
				return new CDBWizardStatus(EDBWizardStatusCode.err_multiple_objects_found);
			}
			p_db_obj.CopyFrom(p_objs[0]);
			return new CDBWizardStatus(EDBWizardStatusCode.success);
		}
		private CDBWizardStatus LoadObject(CDataBase p_data_base, SQL.CWhereCondition p_condition, Dictionary<CDataBaseObject, CDataBaseObject> p_known_objects, List<CDataBaseObject> p_objs)
		{
			Queries.CDataBaseQueryResult p_primitives_query_result = SelectPrimitives(p_data_base, p_condition);

			if (p_primitives_query_result.m_p_exception != null)
			{
				return new CDBWizardStatus(EDBWizardStatusCode.err_exception_thrown, p_primitives_query_result.m_p_exception);
			}

			CDataBaseResultSet p_result_set = p_primitives_query_result.m_p_result_set;
			if (p_result_set.IsEmpty)
			{
				return new CDBWizardStatus(EDBWizardStatusCode.err_no_object_found);
			}

			for (Int32 i = 0; i < p_result_set.Count; ++i)
			{
				CDataBaseObject p_obj = new CDataBaseObject(this);
				CDBWizardStatus p_status = AssignObject(p_data_base, p_result_set[i], p_obj, p_known_objects);
                if (p_status.m_status_code == EDBWizardStatusCode.err_multiple_objects_found)
                {
                    p_obj = p_known_objects[p_obj];
                }
				else if (p_status.IsError)
				{
					return p_status;
				}

				p_objs.Add(p_obj);
			}

			return new CDBWizardStatus(EDBWizardStatusCode.success);
		}
		private CDBWizardStatus AssignObject(CDataBase p_data_base, CDataBaseRow p_row, CDataBaseObject p_db_obj, Dictionary<CDataBaseObject, CDataBaseObject> p_known_objects)
		{
			List<CObjectMap> p_sub_classes = CObjectMap.GetExistingSubClasses(m_p_object_type);
			if (p_sub_classes.Count == 0)
			{
				p_sub_classes.Add(this);
			}
			Dictionary<String, SStorePrimitiveOptions> p_full_primitives = new Dictionary<String, SStorePrimitiveOptions>();
			for (Int32 i = 0; i < p_sub_classes.Count; ++i)
			{
				CObjectMap p_sub_class_map = p_sub_classes[i];
				// since subclasses always contain the base-class primitives, just going through the subclasses is fine
				// should there be no subclasses initially, only the base-class is considered
				foreach (KeyValuePair<String, SStorePrimitiveOptions> primitive_entry in p_sub_class_map.m_p_primitives_map)
				{
					if (p_full_primitives.ContainsKey(primitive_entry.Key)) continue;

					p_full_primitives.Add(primitive_entry.Key, primitive_entry.Value);
				}
			}

			foreach (KeyValuePair<String, SStorePrimitiveOptions> primitive_entry in p_full_primitives)
			{
				Object p_row_value;
				if (!p_row.TryGetValue(primitive_entry.Value.m_p_column_name, out p_row_value))
				{
					throw new Exception("Could not find selected value for column \"" + primitive_entry.Value.m_p_column_name + "\". Perhaps your table does not contain such a column?");
				}

				switch (p_db_obj.SetPrimitive(primitive_entry.Value.m_p_column_name, p_row_value))
				{
					case CDataBaseObject.ESetPrimitiveStatus.err_type_mismatch:
						throw new Exception("The column type specified in your mysql table for column \"" + primitive_entry.Value.m_p_column_name + "\" does not match the specified primitive type associated with the column in type " + m_p_object_type.FullName);
					case CDataBaseObject.ESetPrimitiveStatus.err_unknown_column:
						throw new Exception("The DBWizard completely fucked up, basically. The column \"" + primitive_entry.Value.m_p_column_name + "\" is not known to be in type " + m_p_object_type.FullName + " despite being selected as being eligible for that same type.");
					case CDataBaseObject.ESetPrimitiveStatus.err_unknown_primitive_type:
						throw new Exception("Your primitive for column type \"" + primitive_entry.Value.m_p_column_name + "\" in type " + m_p_object_type.FullName + " has an unknown type.");
				}
			}

			CDataBaseObject p_known_object;
			if (p_known_objects.TryGetValue(p_db_obj, out p_known_object))
			{
				return new CDBWizardStatus(EDBWizardStatusCode.err_multiple_objects_found); // Already loaded, thus error!
			}
			p_known_objects.Add(p_db_obj, p_db_obj);

			return SelectLinks(p_db_obj, p_data_base, p_row, p_known_objects);
		}
		private Queries.CDataBaseQueryResult SelectPrimitives(CDataBase p_data_base, SQL.CWhereCondition p_condition)
		{
			Queries.CDataBaseQueryResult p_primitives_query_result = new Queries.CSelectQuery(
				p_data_base,
				this,
				m_p_object_table,
				new String[] { "*" },
				p_condition
			).Run();

			return p_primitives_query_result;
		}
		private CDBWizardStatus SelectLinks(CDataBaseObject p_object, CDataBase p_data_base, CDataBaseRow p_row, Dictionary<CDataBaseObject, CDataBaseObject> p_known_objects)
		{
			Queries.CSelectQuery[] p_queries = new Queries.CSelectQuery[m_p_object_links.Count];

            List<SObjectLink> p_relevant_links = new List<SObjectLink>(m_p_object_links);
            List<String> p_linked_values_names = new List<String>(m_p_linked_values_names);
            HashSet<String> p_known_links = new HashSet<String>(m_p_linked_values_names);
            foreach(CObjectMap p_sub_class_map in CObjectMap.GetExistingSubClasses(m_p_object_type))
            {
                for (Int32 i = 0; i < p_sub_class_map.m_p_linked_values_names.Count; ++i)
                {
                    if (p_known_links.Add(p_sub_class_map.m_p_linked_values_names[i]))
                    {
                        p_relevant_links.Add(p_sub_class_map.m_p_object_links[i]);
                        p_linked_values_names.Add(p_sub_class_map.m_p_linked_values_names[i]);
                    }
                }
            }
            for (Int32 i = 0; i < p_relevant_links.Count; ++i)
			{
                SObjectLink table_link = p_relevant_links[i];
				CForeignKey p_foreign_key_link = table_link.m_p_foreign_key;
				CObjectMap p_target_map = table_link.m_p_target_map;

				ReadOnlyCollection<String> p_source_columns = p_foreign_key_link.m_p_source_columns;
				ReadOnlyCollection<String> p_target_columns = p_foreign_key_link.m_p_target_columns;

                Boolean has_linked_objects = true;
				SQL.CWhereCondition[] p_link_conditions = new SQL.CWhereCondition[p_target_columns.Count];
				for (Int32 k = 0; k < p_target_columns.Count; ++k)
				{
                    if (p_row[p_source_columns[k]] is DBNull)
                    {
                        p_object.SetDBObjects(new List<CDataBaseObject>() { null }, p_linked_values_names[i]);
                        has_linked_objects = false;
                        break;
                    }

					p_link_conditions[k] = new SQL.CWhereCondition(
						p_target_columns[k],
						"=",
						p_row.Get<Object>(p_source_columns[k]).ToString(),
						null,
						SQL.EBooleanOperator.and
					);
				}
                if (!has_linked_objects)
                    continue;

				Queries.CSelectQuery p_explorer_query = new Queries.CSelectQuery(
					p_data_base,
					table_link.m_p_target_map,
					table_link.m_p_target_map.m_p_object_table,
					new String[] { "*" },
					new SQL.CWhereCondition(p_link_conditions)
				);
				if (p_target_map.m_p_object_type == null)
				{
					// just continue exploration
					Queries.CDataBaseQueryResult p_linked_primitives_result = p_explorer_query.Run();
					if (p_linked_primitives_result.m_p_result_set.IsEmpty)
					{
                        p_object.SetDBObject(null, p_linked_values_names[i]); // make sure there is at least an empty list for the given value name!
					}
					else
					{
						for (Int32 j = 0; j < p_linked_primitives_result.m_p_result_set.Count; ++j)
						{
							CDBWizardStatus p_status = p_target_map.SelectLinks(p_object, p_data_base, p_linked_primitives_result.m_p_result_set[j], p_known_objects);
							if (p_status.IsError)
							{
								return p_status;
							}
						}
					}
				}
				else
				{
					// load additional db objects
					List<CDataBaseObject> p_linked_objs = new List<CDataBaseObject>();
					CDBWizardStatus p_status = p_target_map.LoadObject(p_data_base, new SQL.CWhereCondition(p_link_conditions), p_known_objects, p_linked_objs);
					if (p_status.m_status_code == EDBWizardStatusCode.err_no_object_found)
					{
						// do nothing, do not treat as error, specifically
						p_linked_objs = new List<CDataBaseObject>() { null };
					}
					else if (p_status.IsError)
					{
						return p_status;
					}
                    p_object.SetDBObjects(p_linked_objs, p_linked_values_names[i]);
				}
			}
			return new CDBWizardStatus(EDBWizardStatusCode.success);
		}

		internal async Task<CDBWizardStatus> LoadObjectAsync(CDataBase p_data_base, SQL.CWhereCondition p_condition, CDataBaseObject p_db_obj)
		{
			List<CDataBaseObject> p_objs = new List<CDataBaseObject>();
			CDBWizardStatus p_status = await LoadObjectAsync(p_data_base, p_condition, new Dictionary<CDataBaseObject,CDataBaseObject>(), p_objs);
			if (p_status.IsError)
			{
				p_db_obj = null;
				return p_status;
			}
			if (p_objs.Count == 0)
			{
				p_db_obj = null;
				return new CDBWizardStatus(EDBWizardStatusCode.err_no_object_found);
			}
			else if (p_objs.Count > 1)
			{
				p_db_obj = null;
				return new CDBWizardStatus(EDBWizardStatusCode.err_multiple_objects_found);
			}
			p_db_obj.CopyFrom(p_objs[0]);
			return new CDBWizardStatus(EDBWizardStatusCode.success);
		}
		private async Task<CDBWizardStatus> LoadObjectAsync(CDataBase p_data_base, SQL.CWhereCondition p_condition, Dictionary<CDataBaseObject, CDataBaseObject> p_known_objects, List<CDataBaseObject> p_objs)
		{
			Queries.CDataBaseQueryResult p_primitives_query_result = await SelectPrimitivesAsync(p_data_base, p_condition);

			if (p_primitives_query_result.m_p_exception != null)
			{
				return new CDBWizardStatus(EDBWizardStatusCode.err_exception_thrown, p_primitives_query_result.m_p_exception);
			}

			CDataBaseResultSet p_result_set = p_primitives_query_result.m_p_result_set;
			if (p_result_set.IsEmpty)
			{
				return new CDBWizardStatus(EDBWizardStatusCode.err_no_object_found);
			}

			for (Int32 i = 0; i < p_result_set.Count; ++i)
			{
				CDataBaseObject p_obj = new CDataBaseObject(this);
				CDBWizardStatus p_status = await AssignObjectAsync(p_data_base, p_result_set[i], p_obj, p_known_objects);
                if (p_status.m_status_code == EDBWizardStatusCode.err_multiple_objects_found)
                {
                    p_obj = p_known_objects[p_obj];
                }
				else if (p_status.IsError)
				{
					return p_status;
				}

				p_objs.Add(p_obj);
			}

			return new CDBWizardStatus(EDBWizardStatusCode.success);
		}
		private async Task<CDBWizardStatus> AssignObjectAsync(CDataBase p_data_base, CDataBaseRow p_row, CDataBaseObject p_db_obj, Dictionary<CDataBaseObject, CDataBaseObject> p_known_objects)
		{
			List<CObjectMap> p_sub_classes = CObjectMap.GetExistingSubClasses(m_p_object_type);
			if (p_sub_classes.Count == 0)
			{
				p_sub_classes.Add(this);
			}
			Dictionary<String, SStorePrimitiveOptions> p_full_primitives = new Dictionary<String, SStorePrimitiveOptions>();
			for (Int32 i = 0; i < p_sub_classes.Count; ++i)
			{
				CObjectMap p_sub_class_map = p_sub_classes[i];
				// since subclasses always contain the base-class primitives, just going through the subclasses is fine
				// should there be no subclasses initially, only the base-class is considered
				foreach (KeyValuePair<String, SStorePrimitiveOptions> primitive_entry in p_sub_class_map.m_p_primitives_map)
				{
					if (p_full_primitives.ContainsKey(primitive_entry.Key)) continue;

					p_full_primitives.Add(primitive_entry.Key, primitive_entry.Value);
				}
			}

			foreach (KeyValuePair<String, SStorePrimitiveOptions> primitive_entry in p_full_primitives)
			{
				Object p_row_value;
				if (!p_row.TryGetValue(primitive_entry.Value.m_p_column_name, out p_row_value))
				{
					throw new Exception("Could not find selected value for column \"" + primitive_entry.Value.m_p_column_name + "\". Perhaps your table does not contain such a column?");
				}

				switch (p_db_obj.SetPrimitive(primitive_entry.Value.m_p_column_name, p_row_value))
				{
					case CDataBaseObject.ESetPrimitiveStatus.err_type_mismatch:
						throw new Exception("The column type specified in your mysql table for column \"" + primitive_entry.Value.m_p_column_name + "\" does not match the specified primitive type associated with the column in type " + m_p_object_type.FullName);
					case CDataBaseObject.ESetPrimitiveStatus.err_unknown_column:
						throw new Exception("The DBWizard completely fucked up, basically. The column \"" + primitive_entry.Value.m_p_column_name + "\" is not known to be in type " + m_p_object_type.FullName + " despite being selected as being eligible for that same type.");
					case CDataBaseObject.ESetPrimitiveStatus.err_unknown_primitive_type:
						throw new Exception("Your primitive for column type \"" + primitive_entry.Value.m_p_column_name + "\" in type " + m_p_object_type.FullName + " has an unknown type.");
				}
			}

			CDataBaseObject p_known_object;
			if (p_known_objects.TryGetValue(p_db_obj, out p_known_object))
			{
				return new CDBWizardStatus(EDBWizardStatusCode.err_multiple_objects_found); // Object already loaded, thus error!
			}
			p_known_objects.Add(p_db_obj, p_db_obj);

			return await SelectLinksAsync(p_db_obj, p_data_base, p_row, p_known_objects);
		}
		private async Task<Queries.CDataBaseQueryResult> SelectPrimitivesAsync(CDataBase p_data_base, SQL.CWhereCondition p_condition)
		{
			Queries.CDataBaseQueryResult p_primitives_query_result = await new Queries.CSelectQuery(
				p_data_base,
				this,
				m_p_object_table,
				new String[] { "*" },
				p_condition
			).RunAsync();

			return p_primitives_query_result;
		}
		private async Task<CDBWizardStatus> SelectLinksAsync(CDataBaseObject p_object, CDataBase p_data_base, CDataBaseRow p_row, Dictionary<CDataBaseObject, CDataBaseObject> p_known_objects)
		{
			Queries.CSelectQuery[] p_queries = new Queries.CSelectQuery[m_p_object_links.Count];
			for (Int32 i = 0; i < m_p_object_links.Count; ++i)
			{
				SObjectLink table_link = m_p_object_links[i];
				CForeignKey p_foreign_key_link = table_link.m_p_foreign_key;
				CObjectMap p_target_map = table_link.m_p_target_map;

				ReadOnlyCollection<String> p_source_columns = p_foreign_key_link.m_p_source_columns;
				ReadOnlyCollection<String> p_target_columns = p_foreign_key_link.m_p_target_columns;

                Boolean has_linked_objects = true;
				SQL.CWhereCondition[] p_link_conditions = new SQL.CWhereCondition[p_target_columns.Count];
				for (Int32 k = 0; k < p_target_columns.Count; ++k)
                {
                    if (p_row[p_source_columns[k]] is DBNull)
                    {
                        p_object.SetDBObjects(new List<CDataBaseObject>() { null }, m_p_linked_values_names[i]);
                        has_linked_objects = false;
                        break;
                    }

					p_link_conditions[k] = new SQL.CWhereCondition(
						p_target_columns[k],
						"=",
						p_row.Get<Object>(p_source_columns[k]).ToString(),
						null,
						SQL.EBooleanOperator.and
					);
				}
                if (!has_linked_objects)
                    continue;

				Queries.CSelectQuery p_explorer_query = new Queries.CSelectQuery(
					p_data_base,
					table_link.m_p_target_map,
					table_link.m_p_target_map.m_p_object_table,
					new String[] { "*" },
					new SQL.CWhereCondition(p_link_conditions)
				);
				if (p_target_map.m_p_object_type == null)
				{
					// just continue exploration
					Queries.CDataBaseQueryResult p_linked_primitives_result = await p_explorer_query.RunAsync();
					if (p_linked_primitives_result.m_p_result_set.IsEmpty)
					{
						p_object.SetDBObject(null, m_p_linked_values_names[i]); // make sure there is at least an empty list for the given value name!
					}
					else
					{
						for (Int32 j = 0; j < p_linked_primitives_result.m_p_result_set.Count; ++j)
						{
							CDBWizardStatus p_status = await p_target_map.SelectLinksAsync(p_object, p_data_base, p_linked_primitives_result.m_p_result_set[j], p_known_objects);
							if (p_status.IsError)
							{
								return p_status;
							}
						}
					}
				}
				else
				{
					// load additional db objects
					List<CDataBaseObject> p_linked_objs = new List<CDataBaseObject>();
					CDBWizardStatus p_status = await p_target_map.LoadObjectAsync(p_data_base, new SQL.CWhereCondition(p_link_conditions), p_known_objects, p_linked_objs);
					if (p_status.m_status_code == EDBWizardStatusCode.err_no_object_found)
					{
						// do nothing, do not treat as error, specifically
						p_linked_objs = new List<CDataBaseObject>() { null };
					}
					else if (p_status.IsError)
					{
						return p_status;
					}
					p_object.SetDBObjects(p_linked_objs, m_p_linked_values_names[i]);
				}
			}
			return new CDBWizardStatus(EDBWizardStatusCode.success);
		}

		internal CDBWizardStatus SaveObject<T>(CDataBase p_data_base, T p_obj)
		{
			CDataBaseObject p_db_obj = new CDataBaseObject(CObjectMap.Get(p_obj.GetType()));
			p_db_obj.MapFrom(p_obj, new Dictionary<Object,CDataBaseObject>());

			Dictionary<Queries.CDataBaseQuery, CDataBaseObject> p_query_map = SaveObject(p_data_base, p_db_obj, new Dictionary<CDataBaseObject, CDataBaseObject>());
			Queries.CDataBaseQuery[] p_queries = p_query_map.Keys.ToArray();

			List<Queries.CInsertQuery> p_insert_queries = (from p_query in p_queries where p_query is Queries.CInsertQuery select (Queries.CInsertQuery)p_query).ToList();
			List<Queries.CDeleteQuery> p_delete_queries = (from p_query in p_queries where p_query is Queries.CDeleteQuery select (Queries.CDeleteQuery)p_query).ToList();

			Dictionary<CDataBaseObject, Int32> p_other_indices = new Dictionary<CDataBaseObject, Int32>();
			for (Int32 i = 0; i < p_insert_queries.Count; ++i)
			{
				Int32 other_index;
				if (p_other_indices.TryGetValue(p_query_map[p_insert_queries[i]], out other_index))
				{
					p_insert_queries.RemoveAt(other_index);
					--i;
				}
				p_other_indices[p_query_map[p_insert_queries[i]]] = i;
			}

			Dictionary<String, List<Queries.CInsertQuery>> p_insert_queries_by_table = new Dictionary<String, List<Queries.CInsertQuery>>();
			for (Int32 i = 0; i < p_insert_queries.Count; ++i)
			{
				List<Queries.CInsertQuery> p_sub_list;
				if (!p_insert_queries_by_table.TryGetValue(p_insert_queries[i].m_p_table_name, out p_sub_list))
				{
					p_sub_list = new List<Queries.CInsertQuery>();
					p_insert_queries_by_table[p_insert_queries[i].m_p_table_name] = p_sub_list;
				}
				p_sub_list.Add(p_insert_queries[i]);
			}

			/*p_insert_queries.Clear();
			foreach (KeyValuePair<String, List<Queries.CInsertQuery>> queries_by_table_entry in p_insert_queries_by_table)
			{
				List<Queries.CInsertQuery> p_mergable_queries = queries_by_table_entry.Value;
				for (Int32 i = 0; i < p_mergable_queries.Count; ++i)
				{

				}
				p_insert_queries.Add(MergeInsertQueries(p_data_base, queries_by_table_entry.Value));
			}*/

			DbConnection p_connection = p_data_base.GetConnection();
			DbTransaction p_trans_action = p_connection.BeginTransaction();
			try
			{
				for (Int32 i = 0; i < p_delete_queries.Count; ++i)
				{
					p_delete_queries[i].m_p_connection = p_connection;
					p_delete_queries[i].m_p_trans_action = p_trans_action;
				}
				for (Int32 i = 0; i < p_insert_queries.Count; ++i)
				{
					p_insert_queries[i].m_p_connection = p_connection;
					p_insert_queries[i].m_p_trans_action = p_trans_action;
				}

				for (Int32 i = 0; i < p_delete_queries.Count; ++i)
				{
					p_delete_queries[i].Run();
				}
				List<Queries.CDataBaseQueryResult> p_results = new List<Queries.CDataBaseQueryResult>();
				for (Int32 i = 0; i < p_insert_queries.Count; ++i)
				{
					Queries.CDataBaseQueryResult p_result = p_insert_queries[i].Run();

					if (p_result.m_last_inserted_id.HasValue)
					{
						CDataBaseObject p_inserted_obj;
						if (p_query_map.TryGetValue(p_insert_queries[i], out p_inserted_obj))
						{
							if (p_inserted_obj != null && p_inserted_obj.m_p_source != null)
							{
								CObjectMap p_mapper = CObjectMap.Get(p_inserted_obj.m_p_source.GetType());
								p_mapper.m_p_assign_auto_increment_method.Invoke(p_inserted_obj.m_p_source, new Object[] { p_inserted_obj.m_p_source, p_result.m_last_inserted_id.Value });
							}
						}
					}
				}

				p_trans_action.Commit();
			}
			catch (Exception p_except)
			{
				p_trans_action.Rollback();
				return new CDBWizardStatus(EDBWizardStatusCode.err_exception_thrown, p_except);
			}
			finally
			{
				p_trans_action.Dispose();
				p_connection.Dispose();
			}
			return new CDBWizardStatus(EDBWizardStatusCode.success);
			// TODO: Research EnlistTransaction function and stuff so you actually know what youre doing :V
		}
		
		internal async Task<CDBWizardStatus> SaveObjectAsync<T>(CDataBase p_data_base, T p_obj)
		{
			CDataBaseObject p_db_obj = new CDataBaseObject(CObjectMap.Get(p_obj.GetType()));
			p_db_obj.MapFrom(p_obj, new Dictionary<Object,CDataBaseObject>());

			Dictionary<Queries.CDataBaseQuery, CDataBaseObject> p_query_map = SaveObject(p_data_base, p_db_obj, new Dictionary<CDataBaseObject, CDataBaseObject>());
			Queries.CDataBaseQuery[] p_queries = p_query_map.Keys.ToArray();

			List<Queries.CInsertQuery> p_insert_queries = (from p_query in p_queries where p_query is Queries.CInsertQuery select (Queries.CInsertQuery)p_query).ToList();
			List<Queries.CDeleteQuery> p_delete_queries = (from p_query in p_queries where p_query is Queries.CDeleteQuery select (Queries.CDeleteQuery)p_query).ToList();

			Dictionary<String, List<Queries.CInsertQuery>> p_insert_queries_by_table = new Dictionary<String, List<Queries.CInsertQuery>>();
			for(Int32 i = 0; i < p_insert_queries.Count; ++i)
			{
				List<Queries.CInsertQuery> p_sub_list;
				if (!p_insert_queries_by_table.TryGetValue(p_insert_queries[i].m_p_table_name, out p_sub_list))
				{
					p_sub_list = new List<Queries.CInsertQuery>();
					p_insert_queries_by_table[p_insert_queries[i].m_p_table_name] = p_sub_list;
				}
				p_sub_list.Add(p_insert_queries[i]);
			}

			/*p_insert_queries.Clear();
			foreach (KeyValuePair<String, List<Queries.CInsertQuery>> queries_by_table_entry in p_insert_queries_by_table)
			{
				List<Queries.CInsertQuery> p_mergable_queries = queries_by_table_entry.Value;
				for (Int32 i = 0; i < p_mergable_queries.Count; ++i)
				{

				}
				p_insert_queries.Add(MergeInsertQueries(p_data_base, queries_by_table_entry.Value));
			}*/

			DbConnection p_connection = await p_data_base.GetConnectionAsync();
			DbTransaction p_trans_action = p_connection.BeginTransaction();
			try
			{
				for (Int32 i = 0; i < p_delete_queries.Count; ++i)
				{
					p_delete_queries[i].m_p_connection = p_connection;
					p_delete_queries[i].m_p_trans_action = p_trans_action;
				}
				for (Int32 i = 0; i < p_insert_queries.Count; ++i)
				{
					p_insert_queries[i].m_p_connection = p_connection;
					p_insert_queries[i].m_p_trans_action = p_trans_action;
				}

				for (Int32 i = 0; i < p_delete_queries.Count; ++i)
				{
					await p_delete_queries[i].RunAsync();
				}
				List<Queries.CDataBaseQueryResult> p_results = new List<Queries.CDataBaseQueryResult>();
				for (Int32 i = 0; i < p_insert_queries.Count; ++i)
				{
					Queries.CDataBaseQueryResult p_result = await p_insert_queries[i].RunAsync();
					
					if (p_result.m_last_inserted_id.HasValue)
					{
						CDataBaseObject p_inserted_obj;
						if (p_query_map.TryGetValue(p_insert_queries[i], out p_inserted_obj))
						{
							if (p_inserted_obj != null && p_inserted_obj.m_p_source != null)
							{
								CObjectMap p_mapper = CObjectMap.Get(p_inserted_obj.m_p_source.GetType());
								p_mapper.m_p_assign_auto_increment_method.Invoke(p_inserted_obj.m_p_source, new Object[] { p_inserted_obj.m_p_source, p_result.m_last_inserted_id.Value });
							}
						}
					}
				}

				p_trans_action.Commit();
			}
			catch(Exception p_except)
			{
				p_trans_action.Rollback();
				return new CDBWizardStatus(EDBWizardStatusCode.err_exception_thrown, p_except);
			}
			finally
			{
				p_trans_action.Dispose();
				p_connection.Dispose();
			}
			return new CDBWizardStatus(EDBWizardStatusCode.success);
			// TODO: Research EnlistTransaction function and stuff so you actually know what youre doing :V
		}
		
		private Dictionary<Queries.CDataBaseQuery, CDataBaseObject> SaveObject(CDataBase p_data_base, CDataBaseObject p_db_obj, Dictionary<CDataBaseObject, CDataBaseObject> p_known_objects)
		{
			Dictionary<Queries.CDataBaseQuery, CDataBaseObject> p_queries = new Dictionary<Queries.CDataBaseQuery, CDataBaseObject>();
			p_queries.Add(InsertPrimitives(p_db_obj, p_data_base), p_db_obj);

			CDataBaseObject p_known_object;
			if (p_known_objects.TryGetValue(p_db_obj, out p_known_object))
			{
				return p_queries;
			}
			p_known_objects.Add(p_db_obj, p_db_obj);

			foreach (KeyValuePair<Queries.CDataBaseQuery, CDataBaseObject> query_entry in InsertLinks(p_db_obj, p_data_base, p_known_objects))
			{
				p_queries[query_entry.Key] = query_entry.Value;
			}

			return p_queries;
		}
		private Queries.CInsertQuery InsertPrimitives(CDataBaseObject p_object, CDataBase p_data_base)
		{
			String[] p_primitive_names = p_object.GetPrimitiveNames();
			Object[] p_primitive_values = p_object.GetPrimitiveValues();

			return new Queries.CInsertQuery(
				p_data_base,
				true,
				p_object.m_p_map,
				p_primitive_names,
				p_primitive_values
			);
		}
		private Dictionary<Queries.CDataBaseQuery, CDataBaseObject> InsertLinks(CDataBaseObject p_object, CDataBase p_data_base, Dictionary<CDataBaseObject, CDataBaseObject> p_known_objects)
		{
			Dictionary<Queries.CDataBaseQuery, CDataBaseObject> p_queries = new Dictionary<Queries.CDataBaseQuery, CDataBaseObject>();
			for (Int32 i = 0; i < m_p_object_links.Count; ++i)
			{
				SObjectLink table_link = m_p_object_links[i];
				CForeignKey p_foreign_key_link = table_link.m_p_foreign_key;

				ReadOnlyCollection<String> p_source_columns = p_foreign_key_link.m_p_source_columns;
				ReadOnlyCollection<String> p_target_columns = p_foreign_key_link.m_p_target_columns;

				List<CDataBaseObject> p_objects;
				if (!p_object.TryGetValueAs(m_p_linked_values_names[i], out p_objects)) continue;

				for (Int32 j = 0; j < p_objects.Count; ++j)
				{
					CObjectMap p_target_map = p_objects[j].m_p_map;

					if (table_link.m_type != EObjectLinkType.one_to_one) // no need for clean-up in a one-to-one. Upsert will do the trick :-)
					{
						SQL.CWhereCondition[] p_link_conditions = new SQL.CWhereCondition[p_target_columns.Count];
						for (Int32 k = 0; k < p_target_columns.Count; ++k)
						{
							/*if (p_target_map.m_p_object_type != null)
							{
								p_link_conditions[k] = new SQL.CWhereCondition(
									p_target_columns[k],
									"=",
									p_objects[j][p_target_columns[k]].ToString(),
									null,
									SQL.EBooleanOperator.and
								);
							}
							else
							{*/
							p_link_conditions[k] = new SQL.CWhereCondition(
								p_target_columns[k],
								"=",
								p_object[p_source_columns[k]].ToString(),
								null,
								SQL.EBooleanOperator.and
							);
							//}
						}

						// current Object-list indirectly linked
						// if its indirectly linked, we want to update the intermediate table and the destination table

						p_queries.Add(
							new Queries.CDeleteQuery(
								p_data_base,
								p_target_map,
								p_target_map.m_p_object_table,
								new SQL.CWhereCondition(p_link_conditions),
								0
							),
							null
						); // clean up old linked objects so we can safely insert new ones
					}

					if (p_target_map.m_p_object_type == null)
					{
						// if it's an indirect link, update the indirectly linked table!
						CObjectMap p_destination_map =
							(from p_targ_map in p_target_map.m_p_object_links.Select(x => x.m_p_target_map)
								where p_targ_map.m_p_object_type == p_objects[j].m_p_map.m_p_object_type ||
										p_objects[j].m_p_map.m_p_object_type.IsSubclassOf(p_targ_map.m_p_object_type)
								select p_targ_map
							).FirstOrDefault();
						if(p_destination_map == null)
						{
							throw new Exception("Indirectly linked Object-map does not contain a link pointing to the map of the linked destination objects.");
						}
						SObjectLink destination_link = p_target_map.m_p_object_links.Where(x => x.m_p_target_map == p_destination_map).First();

						String[] p_column_names = p_target_map.m_p_primitives_map.Keys.ToArray();
						String[] p_values = new String[p_column_names.Length];
						for(Int32 k = 0; k < p_source_columns.Count; ++k)
						{
							Int32 index = Array.IndexOf(p_column_names, p_target_columns[k]);
							p_values[index] = p_object[p_source_columns[k]].ToString();
						}
						String[] p_link_to_destination_columns = destination_link.m_p_foreign_key.m_p_source_columns.ToArray();
						String[] p_destination_to_link_columns = destination_link.m_p_foreign_key.m_p_target_columns.ToArray();
						for (Int32 k = 0; k < p_destination_to_link_columns.Length; ++k)
						{
							Int32 index = Array.IndexOf(p_column_names, p_link_to_destination_columns[k]);
							p_values[index] = p_objects[j][p_destination_to_link_columns[k]].ToString();
						}

						p_queries.Add(
							new Queries.CInsertQuery(
								p_data_base,
								true,
								p_target_map,
								p_column_names,
								p_values
							),
							null
						);
					}
					foreach (KeyValuePair<Queries.CDataBaseQuery, CDataBaseObject> query_entry in p_target_map.SaveObject(p_data_base, p_objects[j], p_known_objects))
					{
						p_queries.Add(query_entry.Key, query_entry.Value);
					}
				}
			}
			return p_queries;
		}

		internal CDBWizardStatus DeleteObject<T>(CDataBase p_data_base, T obj)
		{
			CDataBaseObject p_db_obj = new CDataBaseObject(obj);

			SQL.CWhereCondition[] p_conditions = new SQL.CWhereCondition[m_p_unique_keys.Count];
			for (Int32 i = 0; i < m_p_unique_keys.Count; ++i)
			{
				p_conditions[i] = new SQL.CWhereCondition(
					m_p_unique_keys[i].m_p_column_name,
					"=",
					p_db_obj[m_p_unique_keys[i].m_p_column_name].ToString()
				);
			}

			SQL.CWhereCondition p_delete_condition = new SQL.CWhereCondition(p_conditions);

			Queries.CDeleteQuery p_delete_query = new Queries.CDeleteQuery(p_data_base, this, m_p_object_table, p_delete_condition, 1);
			Queries.CDataBaseQueryResult p_result = p_delete_query.Run();
			if (p_result.m_p_exception != null)
			{
				return new CDBWizardStatus(EDBWizardStatusCode.err_exception_thrown, p_result.m_p_exception);
			}
			else if (p_result.m_rows_affected == 0)
			{
				return new CDBWizardStatus(EDBWizardStatusCode.err_no_object_found);
			}
			return new CDBWizardStatus(EDBWizardStatusCode.success);
		}

		internal async Task<CDBWizardStatus> DeleteObjectAsync<T>(CDataBase p_data_base, T obj)
		{
			CDataBaseObject p_db_obj = new CDataBaseObject(obj);

			SQL.CWhereCondition[] p_conditions = new SQL.CWhereCondition[m_p_unique_keys.Count];
			for (Int32 i = 0; i < m_p_unique_keys.Count; ++i)
			{
				p_conditions[i] = new SQL.CWhereCondition(
					m_p_unique_keys[i].m_p_column_name, 
					"=", 
					p_db_obj[m_p_unique_keys[i].m_p_column_name].ToString()
				);
			}

			SQL.CWhereCondition p_delete_condition = new SQL.CWhereCondition(p_conditions);

			Queries.CDeleteQuery p_delete_query = new Queries.CDeleteQuery(p_data_base, this, m_p_object_table, p_delete_condition, 1);
			Queries.CDataBaseQueryResult p_result = await p_delete_query.RunAsync();
			if (p_result.m_p_exception != null)
			{
				return new CDBWizardStatus(EDBWizardStatusCode.err_exception_thrown, p_result.m_p_exception);
			}
			else if (p_result.m_rows_affected == 0)
			{
				return new CDBWizardStatus(EDBWizardStatusCode.err_no_object_found);
			}
			return new CDBWizardStatus(EDBWizardStatusCode.success);
		}

		internal Boolean IsIdentity(String p_column_name)
		{
			for (Int32 i = 0; i < m_p_unique_keys.Count; ++i)
			{
				if (m_p_unique_keys[i].m_p_column_name == p_column_name && m_p_unique_keys[i].m_is_identity)
					return true;
			}
			return false;
		}
		internal Boolean AllowsPrimitive(String p_column_name, out SStorePrimitiveOptions store_options)
		{
			if (m_p_primitives_map.TryGetValue(p_column_name, out store_options))
			{
				return true;
			}
			else
			{
				List<CObjectMap> p_maps = CObjectMap.GetExistingSubClasses(m_p_object_type);
				for (Int32 i = 0; i < p_maps.Count; ++i)
				{
					if (p_maps[i].m_p_primitives_map.TryGetValue(p_column_name, out store_options))
					{
						return true;
					}
				}
				return false;
			}
		}

		public override String ToString()
		{
			return "ObjectMap for " + ((m_p_object_type == null) ? "no type" : m_p_object_type.ToString());
		}
	}
}
