using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Data;

namespace DBWizard
{
    public static class CByteArrayOperations
    {
        private static Char[] _s_p_upper_char_map = new Char[16]
		{
			'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
		};
        private static Char[] _s_p_lower_char_map = new Char[16]
		{
			'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'
		};

        public static String ToUpperHex(this Byte[] p_bytes)
        {
            StringBuilder p_str = new StringBuilder(p_bytes.Length << 1);
            for (Int32 i = 0; i < p_bytes.Length; ++i)
            {
                p_str.Append(_s_p_upper_char_map[(p_bytes[i] >> 4)]);
                p_str.Append(_s_p_upper_char_map[(p_bytes[i] & 0x0F)]);
            }
            return p_str.ToString();
        }
        public static String ToLowerHex(this Byte[] p_bytes)
        {
            StringBuilder p_str = new StringBuilder(p_bytes.Length << 1);
            for (Int32 i = 0; i < p_bytes.Length; ++i)
            {
                p_str.Append(_s_p_lower_char_map[(p_bytes[i] >> 4)]);
                p_str.Append(_s_p_lower_char_map[(p_bytes[i] & 0x0F)]);
            }
            return p_str.ToString();
        }
        public static Byte[] FromHexToBytes(this String p_hex_str)
        {
            if ((p_hex_str.Length & 1) != 0)
                throw new FormatException("The string must have a length that is divisible by two.");

            Byte[] p_bytes = new Byte[p_hex_str.Length >> 1];
            for (Int32 i = 0; i < p_hex_str.Length; ++i)
            {
                Byte cur_nibble = (Byte)p_hex_str[i];
                if (cur_nibble > ('0' - 1) && cur_nibble < ('9' + 1))
                    cur_nibble -= (Byte)'0';
                else if (cur_nibble > ('A' - 1) && cur_nibble < ('F' + 1))
                    cur_nibble -= 'A' - 10;
                else if (cur_nibble > ('a' - 1) && cur_nibble < ('f' + 1))
                    cur_nibble -= 'a' - 10;

                if ((i & 1) == 0)
                {
                    p_bytes[(i >> 1)] = (Byte)(cur_nibble << 4);
                }
                else
                {
                    p_bytes[(i >> 1)] |= cur_nibble;
                }
            }
            return p_bytes;
        }
    }
    public static class CEnumOperations
    {
        public static Type ToType(this EDBPrimitive primtive_type)
        {
            switch (primtive_type)
            {
                case EDBPrimitive.binary: // Byte[]
                case EDBPrimitive.varbinary: // Byte[]
                    return typeof(Byte[]);

                case EDBPrimitive.boolean: // boolean
                    return typeof(Boolean);

                case EDBPrimitive.@decimal: // decimal
                    return typeof(Decimal);

                case EDBPrimitive.@double: // double
                    return typeof(Double);

                case EDBPrimitive.@float: // single
                    return typeof(Single);

                case EDBPrimitive.int8: // sbyte
                    return typeof(SByte);

                case EDBPrimitive.int16: // int16
                    return typeof(Int16);

                case EDBPrimitive.int24: // int32
                case EDBPrimitive.int32: // int32
                    return typeof(Int32);

                case EDBPrimitive.int64: // int64
                    return typeof(Int64);

                case EDBPrimitive.uint8: // Byte
                    return typeof(Byte);

                case EDBPrimitive.uint16: // uint16
                    return typeof(UInt16);

                case EDBPrimitive.uint24: // uint32
                case EDBPrimitive.uint32: // uint32
                    return typeof(UInt32);

                case EDBPrimitive.uint64: // uint64
                case EDBPrimitive.bit: // uint64
                    return typeof(UInt64);

                case EDBPrimitive.@char: // String
                case EDBPrimitive.varchar: // String
                case EDBPrimitive.text:
                    return typeof(String);

                case EDBPrimitive.time:
                case EDBPrimitive.timestamp:
                case EDBPrimitive.date:
                case EDBPrimitive.datetime:
                    return typeof(DateTime);

                default:
                    return null;
            }
        }
        public static Boolean RequiresLength(this EDBPrimitive primitive_type)
        {
            switch (primitive_type)
            {
                default:
                    return true;
                /*default:
                    return false;*/
            }
        }
        public static Boolean RequiresPrecisionAndScale(this EDBPrimitive primtive_type)
        {
            switch (primtive_type)
            {
                case EDBPrimitive.@decimal:
                    return true;
                default:
                    return false;
            }
        }
        public static DbType ToDbType(this EDBPrimitive primitive_type)
        {
            switch (primitive_type)
            {
                case EDBPrimitive.binary:
                case EDBPrimitive.varbinary:
                    return DbType.Binary;
                case EDBPrimitive.bit:
                    return DbType.UInt64;
                case EDBPrimitive.boolean:
                    return DbType.Boolean;
                case EDBPrimitive.@char:
                    return DbType.StringFixedLength;
                case EDBPrimitive.date:
                    return DbType.Date;
                case EDBPrimitive.datetime:
                    return DbType.DateTime;
                case EDBPrimitive.@decimal:
                    return DbType.Decimal;
                case EDBPrimitive.@double:
                    return DbType.Double;
                case EDBPrimitive.@float:
                    return DbType.Single;
                case EDBPrimitive.int16:
                    return DbType.Int16;
                case EDBPrimitive.int32:
                    return DbType.Int32;
                case EDBPrimitive.int64:
                    return DbType.Int64;
                case EDBPrimitive.int8:
                    return DbType.SByte;
                case EDBPrimitive.time:
                    return DbType.Time;
                case EDBPrimitive.timestamp:
                    return DbType.DateTime;
                case EDBPrimitive.uint16:
                    return DbType.UInt16;
                case EDBPrimitive.uint24:
                    return DbType.UInt32;
                case EDBPrimitive.uint32:
                    return DbType.UInt32;
                case EDBPrimitive.uint64:
                    return DbType.UInt64;
                case EDBPrimitive.uint8:
                    return DbType.Byte;
                case EDBPrimitive.varchar:
                case EDBPrimitive.text:
                    return DbType.String;
                case EDBPrimitive.year:
                    return DbType.Int32;
                default:
                    throw new InvalidCastException("Cannot convert DBWizard's \"" + primitive_type.ToString() + "\" to a " + typeof(DbType).FullName + ".");
            }
        }
    }
    public static class CArrayExtensions
    {
        public static Int32 IndexOf<T>(T[] p_arr, T p_elem)
        {
            for (Int32 i = 0; i < p_arr.Length; ++i)
            {
                if (p_arr[i].Equals(p_elem)) return i;
            }
            return -1;
        }
        public static IEnumerable<T> UnionAll<T>(this IEnumerable<IEnumerable<T>> p_enumerable_enum)
        {
            IEnumerable<T> p_result = p_enumerable_enum.FirstOrDefault();
            foreach (IEnumerable<T> p_enum in p_enumerable_enum)
            {
                p_result = p_result.Union(p_enum);
            }
            return p_result;
        }
    }
    public static class CReflectionExtensions
    {
        public static Boolean ToDBPrimitive(this Type p_type, out EDBPrimitive result)
        {
            Type p_nullable;
            if (p_type.GetNullableType(out p_nullable))
                p_type = p_nullable;

            if (p_type == typeof(Byte[]))
            {
                result = EDBPrimitive.varbinary;
                return true;
            }
            else if (p_type == typeof(Boolean))
            {
                result = EDBPrimitive.boolean;
                return true;
            }
            else if (p_type == typeof(Decimal))
            {
                result = EDBPrimitive.@decimal;
                return true;
            }
            else if (p_type == typeof(Double))
            {
                result = EDBPrimitive.@double;
                return true;
            }
            else if (p_type == typeof(Single))
            {
                result = EDBPrimitive.@float;
                return true;
            }
            else if (p_type == typeof(SByte))
            {
                result = EDBPrimitive.int8;
                return true;
            }
            else if (p_type == typeof(Int16))
            {
                result = EDBPrimitive.int16;
                return true;
            }
            else if (p_type == typeof(Int32))
            {
                result = EDBPrimitive.int32;
                return true;
            }
            else if (p_type == typeof(Int64))
            {
                result = EDBPrimitive.int64;
                return true;
            }
            else if (p_type == typeof(Byte))
            {
                result = EDBPrimitive.uint8;
                return true;
            }
            else if (p_type == typeof(UInt16))
            {
                result = EDBPrimitive.uint16;
                return true;
            }
            else if (p_type == typeof(UInt32))
            {
                result = EDBPrimitive.uint32;
                return true;
            }
            else if (p_type == typeof(UInt64))
            {
                result = EDBPrimitive.uint64;
                return true;
            }
            else if (p_type == typeof(String))
            {
                result = EDBPrimitive.varchar;
                return true;
            }
            else if (p_type == typeof(DateTime))
            {
                result = EDBPrimitive.time;
                return true;
            }
            result = EDBPrimitive.none;
            return false;
        }

        /// <summary>
        /// True if the type this is called on is an IEnumerable&lt;T&gt; and returns the type of the elements of the enumeration if it is.
        /// </summary>
        /// <param name="p_type">The type that should be checked for enumerability.</param>
        /// <param name="p_enumerated_type">The type of the enumerated elements.</param>
        /// <returns>True if the type this is called on is an IEnumerable&lt;T&gt; and returns the type of the elements of the enumerations if it is.</returns>
        public static Boolean GetEnumeratedType(this Type p_type, out Type p_enumerated_type)
        {
            if (p_type.IsGenericType)
            {
                Type p_generic_base = p_type.GetGenericTypeDefinition();

                Type[] p_generic_interfaces = (from p_interface in p_generic_base.GetInterfaces() where p_interface.IsGenericType select p_interface.GetGenericTypeDefinition()).ToArray();
                Boolean is_enumerable = p_generic_interfaces.Contains(typeof(IEnumerable<>));
                if (is_enumerable)
                {
                    p_enumerated_type = p_type.GenericTypeArguments[0]; // do not use enumerable type directly
                    return true;
                }
            }
            p_enumerated_type = null;
            return false;
        }
        /// <summary>
        /// True if the type this is called on is a <see cref="System.Nullable"/> and returns the type of the contained value if it is.
        /// </summary>
        /// <param name="p_type">The type that should be checked for nullability.</param>
        /// <param name="p_nullable_type">The type of the nullable value.</param>
        /// <returns>True if the type this is called on is a <see cref="System.Nullable"/> and returns the type of the contained value if it is.</returns>
        public static Boolean GetNullableType(this Type p_type, out Type p_nullable_type)
        {
            if (p_type.IsGenericType)
            {
                Type p_generic_base = p_type.GetGenericTypeDefinition();

                if (p_generic_base == typeof(Nullable<>))
                {
                    p_nullable_type = p_type.GenericTypeArguments[0]; // do not use enumerable type directly
                    return true;
                }
            }
            p_nullable_type = null;
            return false;
        }

        /// <summary>
        /// Returns a list of all fields available in the hierarchy of a given type matching the given binding flags.
        /// </summary>
        /// <param name="p_type">The type whose hierarchy should be searched.</param>
        /// <param name="flags">The binding flags that should be used for matching fields.</param>
        /// <returns>A list of all fields available in the hierarchy of a given type matching the given binding flags.</returns>
        public static List<FieldInfo> GetAllFields(this Type p_type, BindingFlags flags)
        {
            flags |= BindingFlags.DeclaredOnly;
            return GetFieldsRecursive(p_type, flags, new List<FieldInfo>());
        }
        private static List<FieldInfo> GetFieldsRecursive(Type p_type, BindingFlags flags, List<FieldInfo> p_known_fields)
        {
            p_known_fields.AddRange(p_type.GetFields(flags));

            if (p_type.BaseType == null) return p_known_fields;
            return GetFieldsRecursive(p_type.BaseType, flags, p_known_fields);
        }

        /// <summary>
        /// Lists all methods that can be found in the hierarchy of the given type matching the given binding flags. If there are virtual methods in the hierarchy, only the most derived method is included.
        /// </summary>
        /// <param name="p_type">The type whose hierarchy should be searched.</param>
        /// <param name="flags">The flags that should be used for matching methods in the search.</param>
        /// <returns>A list of all methods that can be found in the hierarchy of the given type that match the given binding flags, but only the most derived virtual methods.</returns>
        public static List<MethodInfo> GetAllMethods(this Type p_type, BindingFlags flags)
        {
            flags |= BindingFlags.DeclaredOnly;
            return GetMethodsRecursive(p_type, flags, new List<MethodInfo>());
        }
        /// <summary>
        /// Helper method for getting all methods in a given type hierarchy.
        /// </summary>
        /// <param name="p_type">The type hierarchy to analyze.</param>
        /// <param name="flags">The flags to use for the analysis.</param>
        /// <param name="p_known_methods">The methods that have already been found in previous calls.</param>
        /// <returns>A list of all found methods matching the given binding flags of the type hierarchy.</returns>
        private static List<MethodInfo> GetMethodsRecursive(Type p_type, BindingFlags flags, List<MethodInfo> p_known_methods)
        {
            MethodInfo[] p_methods = p_type.GetMethods(flags);
            List<MethodInfo> p_new_methods = new List<MethodInfo>();
            for (Int32 i = 0; i < p_methods.Length; ++i)
            {
                if (!p_methods[i].IsVirtual)
                {
                    p_new_methods.Add(p_methods[i]);
                    continue;
                }
                Type[] p_new_method_param_types = (from p_param_info in p_methods[i].GetParameters() select p_param_info.ParameterType).ToArray();
                Boolean is_known = false;
                for (Int32 j = 0; j < p_known_methods.Count; ++j)
                {
                    if (p_known_methods[j].Name != p_methods[i].Name)
                    {
                        continue;
                    }
                    if (!p_known_methods[j].IsVirtual)
                    {
                        continue;
                    }

                    Type[] p_known_method_param_types = (from p_param_info in p_known_methods[j].GetParameters() select p_param_info.ParameterType).ToArray();
                    if (p_known_method_param_types.Length != p_new_method_param_types.Length)
                    {
                        continue;
                    }

                    Boolean params_match = true;
                    for (Int32 k = 0; k < p_known_method_param_types.Length; ++k)
                    {
                        if (!p_known_method_param_types[k].Equals(p_new_method_param_types[k]))
                        {
                            params_match = false;
                        }
                    }
                    if (!params_match)
                    {
                        continue;
                    }

                    is_known = true;
                }
                if (!is_known) p_new_methods.Add(p_methods[i]);
            }
            p_known_methods.AddRange(p_new_methods);


            if (p_type.BaseType == null) return p_known_methods;
            return GetMethodsRecursive(p_type.BaseType, flags, p_known_methods);
        }

        public static List<T> GetAttributes<T>(this MemberInfo p_member_info) where T : Attribute
        {
            return (from p_custom_attribute in p_member_info.GetCustomAttributes()
                    where p_custom_attribute is T
                    select (T)p_custom_attribute
            ).ToList();
        }
    }
}