using MySql.Data.MySqlClient;
using MySql.Data.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard
{
    public class CHelper
    {
        /// <summary>
        /// Casts the given object to a <see cref="System.Int64"/>. The object must be a numeric type and may not be a floating-point type.
        /// </summary>
        /// <param name="p_obj">The object that should be cast.</param>
        /// <returns>The value of the object, cast to <see cref="System.Int64"/>.</returns>
        internal static Int64 SafeCastObjectToInt64(Object p_obj)
        {
            if (p_obj is SByte)
                return (Int64)(SByte)p_obj;
            else if (p_obj is Byte)
                return (Int64)(Byte)p_obj;
            else if (p_obj is Int16)
                return (Int64)(Int16)p_obj;
            else if (p_obj is UInt16)
                return (Int64)(UInt16)p_obj;
            else if (p_obj is Int32)
                return (Int64)(Int32)p_obj;
            else if (p_obj is UInt32)
                return (Int64)(UInt32)p_obj;
            else if (p_obj is Int64)
                return (Int64)p_obj;
            else if (p_obj is UInt64)
                return (Int64)(UInt64)p_obj;
            else if (p_obj is Single)
                return (Int64)(Single)p_obj;
            else if (p_obj is Double)
                return (Int64)(Double)p_obj;
            else if (p_obj is Decimal)
                return (Int64)(Decimal)p_obj;
            else
                throw new InvalidCastException("The given object is not of a numeric type.");
        }

        internal static Object MakePrimitiveType(String p_value, EDBPrimitive db_primitive)
        {
            switch (db_primitive)
            {
                case EDBPrimitive.binary:
                case EDBPrimitive.varbinary:
                    return p_value.FromHexToBytes();
                case EDBPrimitive.@char:
                case EDBPrimitive.varchar:
                case EDBPrimitive.text:
                    return p_value;
                case EDBPrimitive.int8:
                    return SByte.Parse(p_value);
                case EDBPrimitive.int16:
                    return Int16.Parse(p_value);
                case EDBPrimitive.int24:
                case EDBPrimitive.int32:
                case EDBPrimitive.year:
                    return Int32.Parse(p_value);
                case EDBPrimitive.int64:
                    return Int64.Parse(p_value);
                case EDBPrimitive.uint8:
                    return Byte.Parse(p_value);
                case EDBPrimitive.uint16:
                    return UInt16.Parse(p_value);
                case EDBPrimitive.uint24:
                case EDBPrimitive.uint32:
                    return UInt32.Parse(p_value);
                case EDBPrimitive.uint64:
                    return UInt64.Parse(p_value);
                case EDBPrimitive.boolean:
                    return Boolean.Parse(p_value);
                case EDBPrimitive.date:
                case EDBPrimitive.datetime:
                case EDBPrimitive.time:
                case EDBPrimitive.timestamp:
                    return DateTime.Parse(p_value);
                default:
                    throw new Exception("Cannot create a primitive type from " + db_primitive.ToString());
            }
        }

        internal static String ToValueString(Object p_value)
        {
            if(p_value is System.Byte[])
            {
                return ((System.Byte[])p_value).ToLowerHex();
            }
            else
            {
                return p_value.ToString();
            }
        }
    }
}
