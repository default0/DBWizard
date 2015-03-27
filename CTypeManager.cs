using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard
{
	/// <summary>
	/// Provides static methods for working with types that need to be serialized into or deserialized from the database.
	/// </summary>
	internal class CTypeManager
	{
		private static Dictionary<Int32, Type> _s_p_types;
		private static Boolean _s_is_initialized = false;

		private static Dictionary<Type, CObjectMap> _s_p_object_maps_by_type;
		private static Dictionary<String, CObjectMap> _s_p_object_maps_by_table_name;

		/// <summary>
		/// Initializes the type manager, loading and processing all types that are currently available to the appdomain.
		/// </summary>
		internal static void Initialize()
		{
			_s_p_types = new Dictionary<Int32, Type>();
			_s_p_object_maps_by_type = new Dictionary<Type, CObjectMap>();
			_s_p_object_maps_by_table_name = new Dictionary<String, CObjectMap>();

			// Gets all types currently loaded into the AppDomain. This should be sufficient as by the time this code is called
			// we can assume that user-code has loaded all its important dependencies.
			IEnumerable<Type[]> p_all_types = (from x in AppDomain.CurrentDomain.GetAssemblies() where !x.GlobalAssemblyCache select x.GetTypes());
			Type[] p_types = new Type[p_all_types.Sum(x => x.Length)];
			Int32 i = 0;
			foreach (Type[] p_assembly_types in p_all_types)
			{
				for (Int32 j = 0; j < p_assembly_types.Length; ++j)
				{
					p_types[i] = p_assembly_types[j];
					++i;
				}
			}

			foreach (Type p_type in p_types)
			{
				ScanForTypeCode(p_type);
				ProcessStoreAttributes(p_type);
			}
			_s_is_initialized = true;
		}

		/// <summary>
		/// Performs a type-switch. The type switch ignores inheritance relations and returns the index of the first type that exactly matches the type of the given Object or -1 if none of the given types matches the type of the Object.
		/// </summary>
		/// <param name="p_object">The Object whose type to determine.</param>
		/// <param name="p_types">The possible types for the Object.</param>
		/// <returns>The index of the first type that exactly matches the type of the given Object, or -1 if none of the given types matches the type of the Object.</returns>
		internal static Int32 TypeSwitch(Object p_object, params Type[] p_types)
		{
			Type p_obj_type = p_object.GetType();
			for (Int32 i = 0; i < p_types.Length; ++i)
			{
				if (p_obj_type == p_types[i]) return i;
			}
			return -1;
		}

		private static void ScanForTypeCode(Type p_type)
		{
			if (p_type.IsInterface || p_type.IsAbstract) return;

			ConstructorInfo p_ctor_info = p_type.GetConstructor(new Type[0]);
			if (p_ctor_info == null || !p_ctor_info.IsPublic) return;

			MethodInfo p_tc_method = p_type.GetMethod("GetTypeCode");
			if (p_tc_method == null) return;
			if (p_tc_method.ReturnType != typeof(Int32)) return;

			Int32 type_code = (Int32)p_tc_method.Invoke(FormatterServices.GetUninitializedObject(p_type), new Object[0]);
			if (_s_p_types.ContainsKey(type_code))
			{
				throw new Exception("Duplicate type code definition in " + p_type.FullName + " and " + _s_p_types[type_code].FullName);
			}
			_s_p_types.Add(type_code, p_type);
		}
		private static void ProcessStoreAttributes(Type p_type)
		{
			// TODO: Think about inheritance more thoroughly...
			// Also GetCustomAttribute<T> may throw an ambiguity exception (nice library design, microsoft, vexing exceptions \o/)
			StoreAttributes.CTableAttribute p_store_table_attribute = p_type.GetCustomAttribute<StoreAttributes.CTableAttribute>();
			if (p_store_table_attribute == null) return; // table store attribute is the only attribute that matters when applied directly to a type.

			CObjectMap p_object_map = CObjectMap.Get(p_type); // <- magic is in the ctor
		}

		/// <summary>
		/// Creates an instance from a given type-code using the assigned types public, parameterless constructor and casts it to the given type.
		/// </summary>
		/// <typeparam name="T">The type to return the created Object as.</typeparam>
		/// <param name="type_code">The type code that should be used to create the Object.</param>
		/// <returns>The created Object cast to the given type.</returns>
		internal static T CreateInstance<T>(Int32 type_code)
		{
			if (!_s_is_initialized) throw new InvalidOperationException("You need to initialize the type manager before you can use it by calling CTypeManager.Initialize.");
			
			return (T)Activator.CreateInstance(_s_p_types[type_code], new Object[0]);
		}

		/// <summary>
		/// Returns an array containing all types that are available for use with CreateInstance&lt;T&gt; and have an associated type code.
		/// </summary>
		/// <returns></returns>
		internal static Type[] GetTypesWithTypeCode()
		{
			if (!_s_is_initialized) throw new InvalidOperationException("You need to initialize the type manager before you can use it by calling CTypeManager.Initialize.");
			
			return (from x in _s_p_types select x.Value).ToArray();
		}
	}
}
