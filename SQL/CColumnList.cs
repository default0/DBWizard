using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard.SQL
{
    /// <summary>
    /// Represents a list of columns in a sql statement.
    /// </summary>
    internal class CColumnList
    {
        /// <summary>
        /// The columns contained in this column list.
        /// </summary>
        internal ReadOnlyCollection<String> m_p_columns { get; private set; }

        /// <summary>
        /// Constructs a new column-list from the given String-array. Each String element represents one column name.
        /// </summary>
        /// <param name="p_columns">The columns this column list should contain.</param>
        internal CColumnList(String[] p_columns)
        {
            m_p_columns = new ReadOnlyCollection<String>((String[])p_columns.Clone());
        }
    }
}
