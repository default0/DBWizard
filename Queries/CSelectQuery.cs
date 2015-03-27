using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard.Queries
{
    /// <summary>
    /// Represents a select query selecting all given columns from a set of rows specified by certain criteria.
    /// </summary>
    internal class CSelectQuery : CDataBaseQuery
    {

        /// <summary>
        /// The columns that should be extracted by this select query.
        /// </summary>
        internal ReadOnlyCollection<String> m_p_columns { get; private set; }
        /// <summary>
        /// The name of the table this select query should extract data from.
        /// </summary>
        internal String m_p_table_name { get; private set; }
        /// <summary>
        /// The conditions that rows must satisfy in order to be part of the result set produced by this select query, may be null if no conditions must be satisfied.
        /// </summary>
        internal SQL.CWhereCondition m_p_where_condition { get; private set; }

        private CObjectMap _m_p_map;

        /// <summary>
        /// Constructs a new select query with the given properties.
        /// </summary>
        /// <param name="p_data_base">The database the query is supposed to run against.</param>
        /// <param name="p_map">The object map the query is created for.</param>
        /// <param name="p_column_names">The columns that should be extracted by the query.</param>
        /// <param name="p_table_name">The name of the table this query should extract data from.</param>
        /// <param name="p_where_condition">The conditions that rows must satisfy to be matched in this query, or null if there should be no conditions.</param>
        internal CSelectQuery(CDataBase p_data_base, CObjectMap p_map, String p_table_name, String[] p_column_names, SQL.CWhereCondition p_where_condition)
            : base(p_data_base)
        {
            if (p_map == null) throw new ArgumentNullException("You must specify a map to use.");
            if (p_table_name == null) throw new ArgumentNullException("You must specify a table name to use.");
            if (String.IsNullOrWhiteSpace(p_table_name)) throw new ArgumentException("The specified table name may not consist only of whitespace.");
            if (p_column_names == null) throw new ArgumentNullException("You must provide column names to use.");

            m_p_columns = new ReadOnlyCollection<String>((IList<String>)p_column_names.Clone());
            m_p_table_name = p_table_name;
            m_p_where_condition = p_where_condition;

            _m_p_map = p_map;
        }

        protected override void PrepareCommand(DbCommand p_cmd)
        {
            StringBuilder p_cmd_text = new StringBuilder();
            p_cmd_text.Append("SELECT ");
            for (Int32 i = 0; i < m_p_columns.Count; ++i)
            {
                p_cmd_text.Append(m_p_columns[i]);
                if ((i + 1) < m_p_columns.Count) p_cmd_text.Append(',');
            }

            p_cmd_text.Append(" FROM ");
            p_cmd_text.Append(m_p_table_name);
            if (m_p_where_condition != null)
            {
                p_cmd_text.Append(" WHERE ");
                p_cmd_text.Append(m_p_where_condition.GetWhereClause());
                p_cmd.Parameters.AddRange(m_p_where_condition.GetWhereParams(p_cmd, _m_p_map));
            }

            p_cmd.CommandText = p_cmd_text.ToString();
        }

        protected override CDataBaseQueryResult RunAsCommand(DbCommand p_command)
        {
            DbDataReader p_reader = null;
            try
            {
                p_reader = p_command.ExecuteReader();

                CDataBaseQueryResult p_result = new CDataBaseQueryResult(this);
                p_result.RetrieveFromReader(p_reader);

                p_reader.Close();

                return p_result;
            }
            catch (MySqlException p_except)
            {
                return new CDataBaseQueryResult(this, p_except);
            }
            catch (Exception p_except)
            {
                throw p_except;
            }
            finally
            {
                if (p_reader != null) p_reader.Close();
            }
        }
        protected override async Task<CDataBaseQueryResult> RunAsCommandAsync(DbCommand p_command)
        {
            DbDataReader p_reader = null;
            try
            {
                p_reader = await p_command.ExecuteReaderAsync();

                CDataBaseQueryResult p_result = new CDataBaseQueryResult(this);
                await p_result.RetrieveFromReaderAsync(p_reader);

                p_reader.Close();

                return p_result;
            }
            catch (MySqlException p_except)
            {
                return new CDataBaseQueryResult(this, p_except);
            }
            catch (Exception p_except)
            {
                throw p_except;
            }
            finally
            {
                if (p_reader != null) p_reader.Close();
            }
        }
    }
}
