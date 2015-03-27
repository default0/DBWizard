using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard
{
    internal delegate Task<Queries.CDataBaseQueryResult[]> QueryFinishedHandler(CDataBaseOperation p_data_base_operation, CDataBaseOperationResult p_operation_result, Queries.CDataBaseQueryResult p_query_result);

    /// <summary>
    /// Represents a database operation, which combines multiple queries into one operation that can be executed at once.
    /// </summary>
    internal class CDataBaseOperation
    {
        private struct SOperationQuery
        {
            public Queries.CDataBaseQuery m_p_query;
            public Boolean m_include_in_result;
            public QueryFinishedHandler m_p_finished_handler;
        }

        private List<SOperationQuery> _m_p_operation_queries;

        /// <summary>
        /// Constructs a new empty database ooperation.
        /// </summary>
        internal CDataBaseOperation()
        {
            _m_p_operation_queries = new List<SOperationQuery>();
        }

        /// <summary>
        /// Adds the given database query to the operation.
        /// </summary>
        /// <param name="p_query">The query to add to the operation.</param>
        /// <param name="reflect_in_result">Whether to add the result of the query as part of the result of the operation.</param>
        internal void AddQuery(Queries.CDataBaseQuery p_query, Boolean reflect_in_result, QueryFinishedHandler p_finished_handler)
        {
            _m_p_operation_queries.Add(
                new SOperationQuery()
                {
                    m_p_query = p_query,
                    m_include_in_result = reflect_in_result,
                    m_p_finished_handler = p_finished_handler
                }
            );
        }

        /// <summary>
        /// Executes this database operation and returns the database operation result.
        /// </summary>
        /// <returns>The database operation result.</returns>
        internal async Task<CDataBaseOperationResult> Run()
        {
            CDataBaseOperationResult p_result = new CDataBaseOperationResult();
            for (Int32 i = 0; i < _m_p_operation_queries.Count; ++i)
            {
                SOperationQuery operation_query = _m_p_operation_queries[i];
                Queries.CDataBaseQueryResult p_query_result = await operation_query.m_p_query.RunAsync();
                if (operation_query.m_include_in_result)
                {
                    p_result.AddQueryResult(p_query_result);
                }

                if (operation_query.m_p_finished_handler != null)
                {
                    // problem: the finished handler is recursive: => include_in_result applies to all nested queries as well
                    Queries.CDataBaseQueryResult[] p_additional_results = await operation_query.m_p_finished_handler(this, p_result, p_query_result);
                    if (operation_query.m_include_in_result)
                    {
                        p_result.AddQueryResults(p_additional_results);
                    }
                }
            }
            return p_result;
        }
    }
}
