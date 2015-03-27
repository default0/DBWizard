using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard.StoreAttributes
{
    /// <summary>
    /// Represents an attribute that marks a class or a struct to be stored in a table.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class CTableAttribute : Attribute
    {
        /// <summary>
        /// The name of the table to store data in.
        /// </summary>
        public String m_p_table_name { get; private set; }

        /// <summary>
        /// The name of the database the table is in, or null.
        /// </summary>
        public String m_p_data_base_name { get; private set; }

        /// <summary>
        /// Constructs a new store table attribute referring to the given table.
        /// </summary>
        /// <param name="p_table_name">The table to refer to.</param>
        public CTableAttribute(String p_table_name)
            : this(p_table_name, null)
        {
        }
        /// <summary>
        /// Constructs a new store table attribute referring to the given table in the given database.
        /// </summary>
        /// <param name="p_table_name">The table to refer to.</param>
        /// <param name="p_data_base_name">The database to refer to.</param>
        public CTableAttribute(String p_table_name, String p_data_base_name)
        {
            m_p_table_name = p_table_name;
            m_p_data_base_name = p_data_base_name;
        }
    }
}
