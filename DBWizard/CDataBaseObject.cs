using MySql.Data.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard
{
    /// <summary>
    /// Represents a database Object that corresponds to a certain Object map.
    /// </summary>
    public sealed class CDataBaseObject
    {
        internal enum ESetPrimitiveStatus
        {
            success = 0,

            err_unknown_column = 10,
            err_type_mismatch = 11,
            err_unknown_primitive_type = 12,
        }
        internal enum ESetObjectStatus
        {
            success = 0,

            err_table_not_linked = 10,
            err_table_link_ambiguous = 11,
        }
        delegate void MapToStructDelegate<T>(ref T p_struct, CDataBaseObject p_db_obj);

        /// <summary>
        /// Returns the value for the given column of this database object.
        /// </summary>
        /// <param name="p_column_name">The name of the column to return the value for.</param>
        /// <returns>The value of the column with the given name.</returns>
        public Object this[String p_column_name]
        {
            get
            {
                if (!_m_p_values.ContainsKey(p_column_name))
                    throw new Exception("The key \"" + p_column_name + "\" was not found.");

                return _m_p_values[p_column_name];
            }
        }
        /// <summary>
        /// Returns the value of the primary key of this database object, null if the object does not have a primary key, or an array of primary keys if there are multiple.
        /// </summary>
        public Object PrimaryKey
        {
            get
            {
                List<SStorePrimitiveOptions> p_unique_keys = m_p_map.m_p_unique_keys;
                if (p_unique_keys.Count == 0)
                    return null;
                else if (p_unique_keys.Count == 1)
                    return this[p_unique_keys[0].m_p_column_name];

                Object[] p_primary_keys = new Object[p_unique_keys.Count];
                for (Int32 i = 0; i < p_unique_keys.Count; ++i)
                {
                    p_primary_keys[i] = this[p_unique_keys[i].m_p_column_name];
                }
                return p_primary_keys;
            }
        }


        internal Boolean IsEmpty { get { return _m_p_values.Count == 0; } }

        private Dictionary<String, Object> _m_p_values;
        internal CObjectMap m_p_map { get; private set; }

        /// <summary>
        /// The source that this database Object was mapped from, or null.
        /// </summary>
        internal Object m_p_source { get; private set; }

        internal CDataBaseObject(CObjectMap p_map)
        {
            if (p_map == null) throw new ArgumentNullException("You must provide an Object map.");
            m_p_map = p_map;
            _m_p_values = new Dictionary<String, Object>();
        }
        /// <summary>
        /// Constructs a new database object and sets its internal values according to the given object.
        /// </summary>
        /// <param name="p_obj">The object that is used as base for this database object.</param>
        public CDataBaseObject(Object p_obj)
        {
            if (p_obj == null) throw new ArgumentNullException("The object you provide must not be null.");
            m_p_map = CObjectMap.Get(p_obj.GetType());
            _m_p_values = new Dictionary<String, Object>();
            MapFrom(p_obj, new Dictionary<Object, CDataBaseObject>());
        }

        public void MapToClass<T>(T p_obj) where T : class
        {
            if (p_obj == null) throw new ArgumentNullException("You must provide an Object.");

            Action<CDataBaseObject> p_delegate = (Action<CDataBaseObject>)CObjectMap.Get(p_obj.GetType()).m_p_map_to_method.CreateDelegate(typeof(Action<CDataBaseObject>), p_obj);
            p_delegate(this);
        }
        public void MapToStruct<T>(ref T p_obj) where T : struct
        {
            MapToStructDelegate<T> p_delegate = (MapToStructDelegate<T>)CObjectMap.Get(p_obj.GetType()).m_p_map_to_method.CreateDelegate(typeof(MapToStructDelegate<T>));
            p_delegate(ref p_obj, this);
        }
        internal void MapFrom(Object p_obj, Dictionary<Object, CDataBaseObject> p_mapped_objects)
        {
            if (p_obj == null)
                return; // lel

            if (p_mapped_objects == null)
                p_mapped_objects = new Dictionary<Object, CDataBaseObject>();
            p_mapped_objects.Add(p_obj, this);

            // used to be a create-delegate + invoke created delegate using call operator
            // but changed due to potential sub-classing issues
            m_p_source = p_obj;
            m_p_map.m_p_map_from_method.Invoke(this, new Object[] { this, p_obj, p_mapped_objects });
        }

        internal ESetPrimitiveStatus SetPrimitive(String p_column_name, Object p_value)
        {
            SStorePrimitiveOptions store_primitive_options;
            if (!m_p_map.AllowsPrimitive(p_column_name, out store_primitive_options))
            {
                return ESetPrimitiveStatus.err_unknown_column;
            }

            if (store_primitive_options.m_auto_convert)
            {
                if (!TryConvert(p_value, store_primitive_options.m_primitive_type, out p_value))
                {
                    return ESetPrimitiveStatus.err_type_mismatch;
                }
            }

            if (!(p_value is System.DBNull) && p_value != null)
            {
                switch (store_primitive_options.m_primitive_type)
                {
                    case EDBPrimitive.binary: // Byte[]
                    case EDBPrimitive.varbinary: // Byte[]
                        if (!(p_value is Byte[])) return ESetPrimitiveStatus.err_type_mismatch;
                        break;

                    case EDBPrimitive.boolean: // boolean
                        if (!(p_value is Boolean)) return ESetPrimitiveStatus.err_type_mismatch;
                        break;

                    case EDBPrimitive.@decimal: // decimal
                        if (!(p_value is Decimal)) return ESetPrimitiveStatus.err_type_mismatch;
                        break;

                    case EDBPrimitive.@double: // double
                        if (!(p_value is Double)) return ESetPrimitiveStatus.err_type_mismatch;
                        break;

                    case EDBPrimitive.@float: // single
                        if (!(p_value is Single)) return ESetPrimitiveStatus.err_type_mismatch;
                        break;

                    case EDBPrimitive.int8: // sbyte
                        if (!(p_value is SByte)) return ESetPrimitiveStatus.err_type_mismatch;
                        break;

                    case EDBPrimitive.int16: // int16
                        if (!(p_value is Int16)) return ESetPrimitiveStatus.err_type_mismatch;
                        break;

                    case EDBPrimitive.int24: // int32
                    case EDBPrimitive.int32: // int32
                    case EDBPrimitive.year:
                        if (!(p_value is Int32)) return ESetPrimitiveStatus.err_type_mismatch;
                        break;

                    case EDBPrimitive.int64: // int64
                        if (!(p_value is Int64)) return ESetPrimitiveStatus.err_type_mismatch;
                        break;

                    case EDBPrimitive.uint8: // Byte
                        if (!(p_value is Byte)) return ESetPrimitiveStatus.err_type_mismatch;
                        break;

                    case EDBPrimitive.uint16: // uint16
                        if (!(p_value is UInt16)) return ESetPrimitiveStatus.err_type_mismatch;
                        break;

                    case EDBPrimitive.uint24: // uint32
                    case EDBPrimitive.uint32: // uint32
                        if (!(p_value is UInt32)) return ESetPrimitiveStatus.err_type_mismatch;
                        break;

                    case EDBPrimitive.uint64: // uint64
                    case EDBPrimitive.bit: // uint64
                        if (!(p_value is UInt64)) return ESetPrimitiveStatus.err_type_mismatch;
                        break;

                    case EDBPrimitive.@char: // String
                    case EDBPrimitive.varchar: // String
                    case EDBPrimitive.text:
                        if (!(p_value is String)) return ESetPrimitiveStatus.err_type_mismatch;
                        break;

                    case EDBPrimitive.date:
                    case EDBPrimitive.datetime:
                    case EDBPrimitive.timestamp:
                        Nullable<DateTime> time;
                        if (p_value is MySqlDateTime)
                        {
                            time = ((MySqlDateTime)p_value).IsValidDateTime ? ((MySqlDateTime)p_value).GetDateTime() : (Nullable<DateTime>)null;
                        }
                        else if (p_value is DateTime)
                        {
                            time = (DateTime)p_value;
                        }
                        else if (p_value is TimeSpan)
                        {
                            time = new DateTime(((TimeSpan)p_value).Ticks);
                        }
                        else if (p_value == null)
                        {
                            time = null;
                        }
                        else
                        {
                            return ESetPrimitiveStatus.err_type_mismatch;
                        }

                        p_value = time;
                        break;

                    case EDBPrimitive.time:
                        if (!(p_value is TimeSpan))
                            return ESetPrimitiveStatus.err_type_mismatch;
                        break;

                    default:
                        return ESetPrimitiveStatus.err_unknown_primitive_type;
                }
            }
            else
            {
                p_value = null;
            }
            _m_p_values[p_column_name] = p_value;

            return ESetPrimitiveStatus.success;
        }
        internal void SetPrimitiveThrow(String p_column_name, Object p_value)
        {
            switch (SetPrimitive(p_column_name, p_value))
            {
                case ESetPrimitiveStatus.err_type_mismatch:
                    throw new Exception("A type mismatch occured when trying to set column \"" + p_column_name + "\" to value \"" + (p_value == null ? "null" : p_value.ToString()) + "\" in type " + m_p_map.m_p_object_type.FullName + ".");
                case ESetPrimitiveStatus.err_unknown_column:
                    throw new Exception("The column \"" + p_column_name + "\" does not exist in type " + m_p_map.m_p_object_type.FullName + ".");
                case ESetPrimitiveStatus.err_unknown_primitive_type:
                    throw new Exception("The primitive type for value \"" + (p_value == null ? "null" : p_value.ToString()) + "\" inserted to column \"" + p_column_name + "\" by type " + m_p_map.m_p_object_type.FullName + " is not known.");
            }
        }
        internal ESetObjectStatus SetDBObject(CDataBaseObject p_object, String p_value_name)
        {
            //if (!m_p_map.m_p_linked_values_names.Contains(p_value_name)) throw new ArgumentException("The given value name was not registered in the Object map.");

            Object p_objs;
            if (!_m_p_values.TryGetValue(p_value_name, out p_objs))
            {
                if (p_object != null)
                {
                    p_objs = new List<CDataBaseObject>() { p_object };
                }
                else
                {
                    p_objs = new List<CDataBaseObject>();
                }
                _m_p_values[p_value_name] = p_objs;
            }
            else
            {
                if (p_object != null)
                {
                    ((List<CDataBaseObject>)p_objs).Add(p_object);
                }
            }
            return ESetObjectStatus.success;
        }
        internal void SetDBObjects(List<CDataBaseObject> p_objects, String p_value_name)
        {
            for (Int32 i = 0; i < p_objects.Count; ++i)
            {
                SetDBObject(p_objects[i], p_value_name);
            }
        }
        internal void SetObject(Object p_obj, String p_value_name, Dictionary<Object, CDataBaseObject> p_mapped_objects)
        {
            CDataBaseObject p_db_obj = null;
            if (p_obj != null)
            {
                if (!p_mapped_objects.TryGetValue(p_obj, out p_db_obj))
                {
                    p_db_obj = new CDataBaseObject(CObjectMap.Get(p_obj.GetType()));
                    p_db_obj.MapFrom(p_obj, p_mapped_objects);
                    if (p_db_obj.IsEmpty)
                    {
                        p_db_obj = null;
                    }
                }
            }
            ESetObjectStatus status = SetDBObject(p_db_obj, p_value_name);
            switch (status)
            {
                case ESetObjectStatus.err_table_link_ambiguous:
                case ESetObjectStatus.err_table_not_linked:
                    throw new Exception("Error while trying to set database Object from given value: " + status.ToString());
                case ESetObjectStatus.success:
                    break;
            }
        }
        internal void SetObjects<T>(IEnumerable<T> p_enumberable, String p_value_name, Dictionary<Object, CDataBaseObject> p_mapped_objects)
        {
            foreach (T obj in p_enumberable)
            {
                SetObject(obj, p_value_name, p_mapped_objects);
            }
        }

        private Boolean TryConvert(Object p_value, EDBPrimitive target_type, out Object p_converted)
        {
            if (p_value == null || p_value is DBNull)
            {
                p_converted = null;
                return true;
            }

            try
            {
                p_converted = Convert.ChangeType(p_value, target_type.ToType());
                return true;
            }
            catch (MySqlConversionException)
            {
                if (p_value is MySqlDateTime && !((MySqlDateTime)p_value).IsValidDateTime)
                {
                    Nullable<DateTime> value = null;
                    p_converted = value;
                    return true;
                }
                p_converted = false;
                return false;
            }
            catch
            {
                p_converted = null;
                return false;
            }
        }

        internal Boolean TryGetValueAs<T>(String p_key, out T p_result) where T : class
        {
            Object p_obj;
            if (!_m_p_values.TryGetValue(p_key, out p_obj))
            {
                p_result = default(T);
                return false;
            }

            p_result = p_obj as T;
            if (p_result == null) return false;

            return true;
        }

        internal Boolean HasValue(String p_key)
        {
            return _m_p_values.ContainsKey(p_key);
        }

        internal String[] GetPrimitiveNames(CDataBaseObject p_parent)
        {
            List<String> p_primitive_names = new List<String>();
            foreach (KeyValuePair<String, Object> entry in _m_p_values)
            {
                if (!(entry.Value is List<CDataBaseObject>))
                {
                    p_primitive_names.Add(entry.Key);
                }
                else
                {
                    Int32 link_index = m_p_map.m_p_linked_values_names.FindIndex(x => x == entry.Key);
                    if (link_index == -1)
                        continue;

                    List<CDataBaseObject> p_linked_objects = (List<CDataBaseObject>)entry.Value;
                    if (p_linked_objects.Count == 0)
                        continue;

                    if (m_p_map.m_p_object_links[link_index].m_type == EObjectLinkType.one_to_one)
                    {
                        p_primitive_names.AddRange(m_p_map.m_p_object_links[link_index].m_p_foreign_key.m_p_source_columns);
                    }
                }
            }
            if (p_parent != null)
            {
                List<SObjectLink> p_parent_links = p_parent.m_p_map.m_p_object_links;
                for (Int32 i = 0; i < p_parent_links.Count; ++i)
                {
                    if (p_parent_links[i].m_p_target_map == m_p_map)
                    {
                        p_primitive_names.AddRange(
                            p_parent_links[i].m_p_foreign_key.m_p_target_columns.Where(p_name => !p_primitive_names.Contains(p_name))
                        );
                    }
                }
            }
            return p_primitive_names.ToArray();
        }
        internal Object[] GetPrimitiveValues(CDataBaseObject p_parent)
        {
            List<Object> p_primitive_values = new List<Object>();
            foreach (KeyValuePair<String, Object> entry in _m_p_values)
            {
                if (!(entry.Value is List<CDataBaseObject>))
                {
                    p_primitive_values.Add(entry.Value);
                }
                else
                {
                    Int32 link_index = m_p_map.m_p_linked_values_names.FindIndex(x => x == entry.Key);
                    if (link_index == -1)
                        continue;

                    List<CDataBaseObject> p_linked_objects = (List<CDataBaseObject>)entry.Value;
                    if (p_linked_objects.Count == 0)
                        continue;

                    if (m_p_map.m_p_object_links[link_index].m_type == EObjectLinkType.one_to_one)
                    {
                        foreach (String p_target_column in m_p_map.m_p_object_links[link_index].m_p_foreign_key.m_p_target_columns)
                        {
                            p_primitive_values.Add(p_linked_objects[0][p_target_column]);
                        }
                    }
                }
            }
            if (p_parent != null)
            {
                List<SObjectLink> p_parent_links = p_parent.m_p_map.m_p_object_links;
                for (Int32 i = 0; i < p_parent_links.Count; ++i)
                {
                    if (p_parent_links[i].m_p_target_map == m_p_map)
                    {
                        ReadOnlyCollection<String> p_source_columns = p_parent_links[i].m_p_foreign_key.m_p_source_columns;
                        for (Int32 j = 0; j < p_source_columns.Count; ++j)
                        {
                            Object p_value;
                            if (p_parent.HasValue(p_source_columns[j]))
                                p_value = p_parent[p_source_columns[j]];
                            else if (HasValue(p_parent_links[i].m_p_foreign_key.m_p_target_columns[j]))
                                p_value = this[p_parent_links[i].m_p_foreign_key.m_p_target_columns[j]];
                            else
                                throw new Exception("Could not track foreign key \"" + p_parent.m_p_map.m_p_linked_values_names[i] + "\" from \"" + p_parent.m_p_map.m_p_object_type.ToString() + "\" to \"" + m_p_map.m_p_object_type.ToString() + "\".");

                            if (p_primitive_values.Contains(p_value))
                                continue;
                            else
                                p_primitive_values.Add(p_value);
                        }
                    }
                }
            }
            return p_primitive_values.ToArray();
        }

        public override Int32 GetHashCode()
        {
            if (m_p_map.m_p_unique_keys.Count == 0)
                return base.GetHashCode();

            // hash primary key
            List<Byte> p_bytes = new List<Byte>();
            foreach (KeyValuePair<String, Object> entry in _m_p_values)
            {
                if (!m_p_map.IsIdentity(entry.Key))
                    continue;

                Object p_obj = entry.Value;
                if (p_obj is Boolean)
                {
                    p_bytes.Add((Boolean)p_obj ? (Byte)1 : (Byte)0);
                }
                if (p_obj is SByte)
                {
                    p_bytes.Add((Byte)(SByte)p_obj);
                }
                else if (p_obj is Byte)
                {
                    p_bytes.Add((Byte)p_obj);
                }
                else if (p_obj is Int16)
                {
                    p_bytes.AddRange(BitConverter.GetBytes((Int16)p_obj));
                }
                else if (p_obj is UInt16)
                {
                    p_bytes.AddRange(BitConverter.GetBytes((UInt16)p_obj));
                }
                else if (p_obj is Int32)
                {
                    p_bytes.AddRange(BitConverter.GetBytes((Int32)p_obj));
                }
                else if (p_obj is UInt32)
                {
                    p_bytes.AddRange(BitConverter.GetBytes((UInt32)p_obj));
                }
                else if (p_obj is Int64)
                {
                    p_bytes.AddRange(BitConverter.GetBytes((Int64)p_obj));
                }
                else if (p_obj is UInt64)
                {
                    p_bytes.AddRange(BitConverter.GetBytes((UInt64)p_obj));
                }
                else if (p_obj is Single)
                {
                    p_bytes.AddRange(BitConverter.GetBytes((Single)p_obj));
                }
                else if (p_obj is Double)
                {
                    p_bytes.AddRange(BitConverter.GetBytes((Double)p_obj));
                }
                else if (p_obj is Decimal)
                {
                    Int32[] p_decimal_bits = Decimal.GetBits((Decimal)p_obj);
                    for (Int32 j = 0; j < p_decimal_bits.Length; ++j)
                    {
                        p_bytes.AddRange(BitConverter.GetBytes(p_decimal_bits[j]));
                    }
                }
                else if (p_obj is Byte[])
                {
                    p_bytes.AddRange((Byte[])p_obj);
                }
                else if (p_obj is String)
                {
                    p_bytes.AddRange(Encoding.UTF8.GetBytes((String)p_obj));
                }
            }

            Int32 hash_code = CCrc32.GetCrc32(p_bytes);
            return hash_code;
        }
        public override Boolean Equals(Object p_obj)
        {
            CDataBaseObject p_db_obj = p_obj as CDataBaseObject;
            if (p_db_obj == null) return false;

            return GetHashCode() == p_db_obj.GetHashCode();
        }

        /// <summary>
        /// Copies all internal values from the given database object into this database object.
        /// </summary>
        /// <param name="p_db_obj">The database object to copy from.</param>
        internal void CopyFrom(CDataBaseObject p_db_obj)
        {
            _m_p_values.Clear();
            foreach (KeyValuePair<String, Object> entry in p_db_obj._m_p_values)
            {
                _m_p_values.Add(entry.Key, entry.Value);
            }
        }
    }
}
