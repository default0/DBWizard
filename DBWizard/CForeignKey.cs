using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard
{
    /// <summary>
    /// Represents a foreign key linking two tables by a collection of their columns.
    /// </summary>
    public class CForeignKey
    {
        /// <summary>
        /// The source table the foreign key links from.
        /// </summary>
        public String m_p_source_table { get; private set; }
        /// <summary>
        /// The source columns the foreign key links from.
        /// </summary>
        public ReadOnlyCollection<String> m_p_source_columns { get; private set; }

        /// <summary>
        /// The destination table the foreign key links to.
        /// </summary>
        public String m_p_destination_table { get; private set; }
        /// <summary>
        /// The destination columns the foreign key links to.
        /// </summary>
        public ReadOnlyCollection<String> m_p_target_columns { get; private set; }

        /// <summary>
        /// Constructs a new foreign key linking the given source table on the given source columns with the given destination table on the given destination columns.
        /// </summary>
        /// <param name="p_source_table">The source table the foreign key should link from.</param>
        /// <param name="p_source_column">The source column the foreign key should link from.</param>
        /// <param name="p_destination_table">The destination table the foreign key should link to.</param>
        /// <param name="p_destination_column">The destination column the foreign key should link to.</param>
        public CForeignKey(String p_source_table, String p_source_column, String p_destination_table, String p_destination_column)
            : this(p_source_table, new String[] { p_source_column }, p_destination_table, new String[] { p_destination_column })
        {
        }
        /// <summary>
        /// Constructs a new foreign key linking the given source table on the given source columns with the given destination table on the given destination columns.
        /// </summary>
        /// <param name="p_source_table">The source table the foreign key should link from.</param>
        /// <param name="p_source_columns">The source columns the foreign key should link from. Must have the same number of elements as the destination columns.</param>
        /// <param name="p_destination_table">The destination table the foreign key should link to.</param>
        /// <param name="p_destination_columns">The destination columns the foreign key should link to. Must have the same number of elements as the source columns.</param>
        public CForeignKey(String p_source_table, String[] p_source_columns, String p_destination_table, String[] p_destination_columns)
        {
            if (p_source_columns == null || p_destination_columns == null)
            {
                throw new ArgumentNullException("Neither source columns nor destination columns may be null.");
            }
            if (p_source_columns.Length == 0 || p_destination_columns.Length == 0)
            {
                throw new ArgumentException("You must specify at least one column to link on for both the source and destination.");
            }
            if (p_source_columns.Length != p_destination_columns.Length)
            {
                throw new ArgumentException("The amount of columns that are linked must be identical for the source and destination.");
            }

            m_p_source_table = p_source_table;
            m_p_source_columns = new ReadOnlyCollection<String>((String[])p_source_columns.Clone());

            m_p_destination_table = p_destination_table;
            m_p_target_columns = new ReadOnlyCollection<String>((String[])p_destination_columns.Clone());
        }

        /// <summary>
        /// Returns a newly created identical foreign key.
        /// </summary>
        /// <returns>A newly created identical foreign key.</returns>
        public CForeignKey Clone()
        {
            return new CForeignKey(m_p_source_table, m_p_source_columns.ToArray(), m_p_destination_table, m_p_target_columns.ToArray());
        }
    }
}
