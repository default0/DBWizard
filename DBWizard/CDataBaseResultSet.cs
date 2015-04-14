using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard
{
    /// <summary>
    /// Represents a result-set containing selected rows from a database query.
    /// </summary>
    public class CDataBaseResultSet
    {
        private List<CDataBaseRow> _m_p_rows;

        /// <summary>
        /// Whether the result set contains any values.
        /// </summary>
        public Boolean IsEmpty { get { return _m_p_rows.Count == 0; } }
        /// <summary>
        /// Whether the result set contains a single unique entry.
        /// </summary>
        public Boolean HasUniqueEntry { get { return _m_p_rows.Count == 1; } }
        /// <summary>
        /// The first entry in the result set.
        /// </summary>
        public CDataBaseRow First { get { return _m_p_rows[0]; } }
        /// <summary>
        /// The number of entries in the result set.
        /// </summary>
        public Int32 Count { get { return _m_p_rows.Count; } }

        public CDataBaseRow this[Int32 index] { get { return _m_p_rows[index]; } }

        /// <summary>
        /// Constructs a new empty result-set.
        /// </summary>
        internal CDataBaseResultSet()
        {
            _m_p_rows = new List<CDataBaseRow>();
        }

        /// <summary>
        /// Adds a new database row to this result set.
        /// </summary>
        /// <param name="p_row">The row to add to this result set.</param>
        internal void AddRow(CDataBaseRow p_row)
        {
            _m_p_rows.Add(p_row);
        }
    }
}
