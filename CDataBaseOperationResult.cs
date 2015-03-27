using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard
{
    /// <summary>
    /// Represents the result of a database operation.
    /// </summary>
    internal class CDataBaseOperationResult
    {
        private List<Queries.CDataBaseQueryResult> _m_p_results;
        /// <summary>
        /// The query-results this operation result holds.
        /// </summary>
        internal ReadOnlyCollection<Queries.CDataBaseQueryResult> m_p_results { get; private set; }

        /// <summary>
        /// Constructs a new empty database operation result.
        /// </summary>
        internal CDataBaseOperationResult()
        {
            _m_p_results = new List<Queries.CDataBaseQueryResult>();
            m_p_results = new ReadOnlyCollection<Queries.CDataBaseQueryResult>(_m_p_results);
        }

        /// <summary>
        /// Adds a new database query result to the database operation result.
        /// </summary>
        /// <param name="p_result">The query result to add to the database operation result.</param>
        internal void AddQueryResult(Queries.CDataBaseQueryResult p_result)
        {
            _m_p_results.Add(p_result);
        }
        /// <summary>
        /// Adds new database query results to the database operation result.
        /// </summary>
        /// <param name="p_results">The query results to add to the database operation result.</param>
        internal void AddQueryResults(Queries.CDataBaseQueryResult[] p_results)
        {
            _m_p_results.AddRange(p_results);
        }
    }
}
