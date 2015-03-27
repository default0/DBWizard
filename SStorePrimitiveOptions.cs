using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard
{
    /// <summary>
    /// Represents options for storing a primitive value in the database.
    /// </summary>
    internal struct SStorePrimitiveOptions
    {
        /// <summary>
        /// Whether to auto-convert the value of the field to the primitive type.
        /// </summary>
        internal Boolean m_auto_convert { get; private set; }
        /// <summary>
        /// The primitive type to store in the database.
        /// </summary>
        internal EDBPrimitive m_primitive_type { get; private set; }

        internal Boolean m_is_identity { get; set; }

        /// <summary>
        /// The name of the column in the table that holds the primitive.
        /// </summary>
        internal String m_p_column_name { get; private set; }
        /// <summary>
        /// The field holding the value that maps to the primitive.
        /// </summary>
        internal FieldInfo m_p_field { get; private set; }
        /// <summary>
        /// The method that is called to determine the value of the primitive.
        /// </summary>
        internal MethodInfo m_p_method { get; private set; }
        /// <summary>
        /// Specifies the way the data in the column should be loaded.
        /// </summary>
        internal EStoreOptions m_store_options { get; private set; }

        /// <summary>
        /// Constructs new primitive store options with the given settings.
        /// </summary>
        /// <param name="p_column_name">The name of the column in the table.</param>
        /// <param name="primitive_type">The type of the database primitive.</param>
        /// <param name="p_field">The field holding the value that maps to the primitive.</param>
        /// <param name="auto_convert">Whether to auto-convert the value of the field to the primitive type.</param>
        /// <param name="load_options">Specified the way the primitive data should be loaded.</param>
        internal SStorePrimitiveOptions(String p_column_name, EDBPrimitive primitive_type, FieldInfo p_field, MethodInfo p_method, Boolean auto_convert, EStoreOptions load_options)
            : this()
        {
            m_p_column_name = p_column_name;
            m_auto_convert = auto_convert;
            m_primitive_type = primitive_type;
            m_p_field = p_field;
            m_p_method = p_method;
            m_store_options = load_options;
            m_is_identity = false;
        }
    }
}
