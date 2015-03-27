using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard.StoreAttributes
{
    /// <summary>
    /// Represents an attribute that marks a field to be referring from a single Object to many objects stored externally.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class COneToManyAttribute : Attribute
    {
        /// <summary>
        /// The value name to identify the callback that handles the loaded data.
        /// </summary>
        public String m_p_value_name { get; private set; }

        /// <summary>
        /// The foreign key that maps the one Object to the many objects.
        /// </summary>
        public CForeignKey m_p_linked_columns { get; private set; }

        /// <summary>
        /// The type that should be mapped to.
        /// </summary>
        public Type m_p_target_type { get; private set; }

        /// <summary>
        /// Constructs a new store one to many attribute linking the one to the many on the primary key of the table containing the many.
        /// </summary>
        public COneToManyAttribute(String p_value_name)
        {
            if (p_value_name == null) throw new ArgumentNullException("The value name may not be null.");
            m_p_value_name = p_value_name;

            m_p_linked_columns = new CForeignKey(
                null, (String)null,
                null, (String)null
            );
        }
        /// <summary>
        /// Constructs a new store one to many attribute linking the one to the many on the given column present in both tables.
        /// </summary>
        /// <param name="p_linked_column_name">The column to link on that is present in both tables.</param>
        public COneToManyAttribute(String p_value_name, String p_linked_column_name)
        {
            if (p_value_name == null) throw new ArgumentNullException("The value name may not be null.");
            m_p_value_name = p_value_name;

            m_p_linked_columns = new CForeignKey(
                null, p_linked_column_name,
                null, p_linked_column_name
            );
        }
        /// <summary>
        /// Constructs a new store one to many attribute linking the one to the many on the given column present in both tables.
        /// </summary>
        /// <param name="p_source_column_name">The column to link on that is present in the source table.</param>
        /// <param name="p_target_column_name">The column to link on that is present in the target table.</param>
        /// <param name="p_target_type">The type that determines what tables are linked.</param>
        public COneToManyAttribute(String p_value_name, String p_source_column_name, String p_target_column_name, Type p_target_type)
        {
            if (p_value_name == null) throw new ArgumentNullException("The value name may not be null.");
            m_p_value_name = p_value_name;
            m_p_target_type = p_target_type;

            m_p_linked_columns = new CForeignKey(
                null, p_source_column_name,
                null, p_target_column_name
            );
        }
        /// <summary>
        /// Constructs a new store one to many attribute linking the one to the many on the given columns present in both tables.
        /// </summary>
        /// <param name="p_linked_column_names">The columns to link on that are present in both tables.</param>
        public COneToManyAttribute(String p_value_name, String[] p_linked_column_names)
        {
            if (p_value_name == null) throw new ArgumentNullException("The value name may not be null.");
            m_p_value_name = p_value_name;

            m_p_linked_columns = new CForeignKey(
                null, p_linked_column_names,
                null, p_linked_column_names
            );
        }
    }
}
