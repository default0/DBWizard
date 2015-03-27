using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using System.Data;

using ETypeCode = System.TypeCode;

namespace DBWizard
{
	/// <summary>
	/// Represents a database row.
	/// </summary>
	public class CDataBaseRow
	{
		private Dictionary<String, Object> _m_p_values;

		/// <summary>
		/// Constructs a new empty database row.
		/// </summary>
		public CDataBaseRow()
		{
			_m_p_values = new Dictionary<String, Object>();
		}

		public Object this[String p_column_name]
		{
			get
			{
				return _m_p_values[p_column_name];
			}
		}

		public Boolean TryGetValue(String p_column_name, out Object p_value)
		{
			return _m_p_values.TryGetValue(p_column_name, out p_value);
		}

		/// <summary>
		/// Retrieves the value for the given column.
		/// </summary>
		/// <typeparam name="T">The type of the value.</typeparam>
		/// <param name="p_column_name">The name of the column.</param>
		/// <returns>The value for the given column.</returns>
		public T Get<T>(String p_column_name)
		{
			return (T)_m_p_values[p_column_name];
		}
		/// <summary>
		/// Sets the value for the given column.
		/// </summary>
		/// <typeparam name="T">The type of the value.</typeparam>
		/// <param name="p_column_name">The name of the column.</param>
		/// <param name="value">The value the column should be set to.</param>
		public void Set<T>(String p_column_name, T value)
		{
			_m_p_values[p_column_name] = value;
		}
	}





























	#region LEGACY DATABASE ROW
	/*/// <summary>
	/// Represents a row in a database. Other rows or lists of other rows can be specified in entries of this row.
	/// </summary>
	public class CDataBaseRow
	{
		public static CDataBaseRow CreateFromReader(MySqlDataReader p_reader)
		{
			if(p_reader.Read())
			{
				CDataBaseRow p_db_obj = new CDataBaseRow();
				for(Int32 i = 0; i < p_reader.FieldCount; i++)
				{
					p_db_obj._m_p_dict.Add(p_reader.GetName(i), p_reader.GetValue(i));
				}
				return p_db_obj;
			}
			return null;
		}
		public static CDataBaseRow CreateFromRow(DataRow p_row, DataColumnCollection p_columns)
		{
			Int32 len = p_row.ItemArray.Length;
			CDataBaseRow p_db_obj = new CDataBaseRow();
			for(Int32 i = 0; i < len; i++)
			{
				p_db_obj._m_p_dict.Add(p_columns[i].ColumnName, p_row.ItemArray[i]);
			}
			return p_db_obj;
		}

		private Dictionary<String, Object> _m_p_dict;

		private const String p_version_field_name = "__version__";
		private const String p_type_code_field_name = "__type_code__";
		private static HashSet<String> _s_p_reserved_names = new HashSet<String>()
		{
			CDataBaseRow.p_version_field_name, CDataBaseRow.p_type_code_field_name
		};

		internal Byte Version
		{
			get
			{
				return GetPrivate<Byte>(CDataBaseRow.p_version_field_name);
			}
			set
			{
				SetPrivate<Byte>(CDataBaseRow.p_version_field_name, value);
			}
		}
		internal UInt16 TypeCode
		{
			get
			{
				return GetPrivate<UInt16>(CDataBaseRow.p_type_code_field_name);
			}
			set
			{
				SetPrivate<UInt16>(CDataBaseRow.p_type_code_field_name, value);
			}
		}

		internal CDataBaseRow()
		{
			_m_p_dict = new Dictionary<String, Object>();
		}
		internal CDataBaseRow(Dictionary<String, Object> p_data)
			: this()
		{
			foreach (KeyValuePair<String, Object> data_entry in p_data)
			{
				Set<Object>(data_entry.Key, data_entry.Value);
			}
		}
		public CDataBaseRow(IDataBaseSerializable p_serializable_object) : this()
		{
			Version = p_serializable_object.GetVersion();
			TypeCode = p_serializable_object.GetTypeCode();
		}

		internal void GetInsertCommand()
		{
			// strategy:
			// Object A owns exactly 1 Object B. Object B does not own Object A.
			// Object A owns exactly 1 Object B. Object B owns exactly 1 Object A.
			// Object A owns n Object B. Object B does not own Object A.

			foreach (Object p_object in _m_p_dict.Keys)
			{
				switch (Type.GetTypeCode(p_object.GetType()))
				{
					case ETypeCode.Boolean:
						break;
					case ETypeCode.Char:
						break;

					case ETypeCode.DateTime:
						break;

					case ETypeCode.DBNull:
						break;
					case ETypeCode.Empty:
						break;

					case ETypeCode.SByte:
						break;
					case ETypeCode.Int16:
						break;
					case ETypeCode.Int32:
						break;
					case ETypeCode.Int64:
						break;

					case ETypeCode.Byte:
						break;
					case ETypeCode.UInt16:
						break;
					case ETypeCode.UInt32:
						break;
					case ETypeCode.UInt64:
						break;

					case ETypeCode.String:
						break;

					case ETypeCode.Single:
						break;
					case ETypeCode.Double:
						break;
					case ETypeCode.Decimal:
						break;

					case ETypeCode.Object:
						IDataBaseSerializable p_serializable = p_object as IDataBaseSerializable;
						if (p_serializable == null)
						{
							IEnumerable<IDataBaseSerializable> p_serializables = p_object as IEnumerable<IDataBaseSerializable>;
							if(p_serializables == null) throw new Exception("wroighodjf"); // this may be a dictionary of some sorts...

							// 1:n relationship
						}

						// 1:1 relationship

						break;
				}
			}
		}

		public CDataBaseRow Clone()
		{
			CDataBaseRow p_clone = new CDataBaseRow();
			foreach (KeyValuePair<String, Object> vals in _m_p_dict)
			{
				p_clone._m_p_dict.Add(vals.Key, vals.Value);
			}
			return p_clone;
		}
		
		public T Get<T>(String p_val_name)
		{
			if (_s_p_reserved_names.Contains(p_val_name))
			{
				throw new ArgumentException("The given value name cannot be used because it is reserved.", "p_val_name");
			}

			return GetPrivate<T>(p_val_name);
		}
		private T GetPrivate<T>(String p_val_name)
		{
			return (T)_m_p_dict[p_val_name];
		}

		public void Set<T>(String p_val_name, T value)
		{
			if (_s_p_reserved_names.Contains(p_val_name))
			{
				throw new ArgumentException("The given value name cannot be used because it is reserved.", "p_val_name");
			}

			SetPrivate<T>(p_val_name, value);
		}
		private void SetPrivate<T>(String p_val_name, T value)
		{
			_m_p_dict[p_val_name] = value;
		}

		public Boolean IsSet(String p_val_name)
		{
			Object p_value;
			if (!_m_p_dict.TryGetValue(p_val_name, out p_value)) return false;
			if (p_value == System.DBNull.Value || p_value == null) return false;

			return true;
		}
	}*/
	#endregion
}
