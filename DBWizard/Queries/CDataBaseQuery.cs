using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard.Queries
{
    /// <summary>
    /// Represents an abstract database query.
    /// </summary>
    public abstract class CDataBaseQuery
    {
        public DbConnection m_p_connection { get; set; }
        public DbTransaction m_p_trans_action { get; set; }


        /// <summary>
        /// The connection String that should be used to run the query against a database.
        /// </summary>
        internal CDataBase m_p_data_base { get; private set; }

        /// <summary>
        /// Constructs a new abstract database query and sets the connection String to the one specified in the database.
        /// </summary>
        /// <param name="p_data_base">The database the connection String should be taken from.</param>
        internal CDataBaseQuery(CDataBase p_data_base)
        {
            m_p_data_base = p_data_base;
        }

        /// <summary>
        /// Runs the database query and returns the result retrieved from execution.
        /// </summary>
        /// <returns>The result after executing the query.</returns>
        internal async Task<CDataBaseQueryResult> RunAsync()
        {
            if (m_p_connection == null)
            {

                using (m_p_connection = await m_p_data_base.GetConnectionAsync())
                {
                    DbCommand p_command = CreateCommand();
                    m_p_data_base.RaiseCommandExecuted(this, p_command);
                    return await RunAsCommandAsync(p_command);
                }
            }
            else
            {
                DbCommand p_command = CreateCommand();
                m_p_data_base.RaiseCommandExecuted(this, p_command);
                return await RunAsCommandAsync(p_command);
            }
        }
        protected abstract Task<CDataBaseQueryResult> RunAsCommandAsync(DbCommand p_command);

        internal CDataBaseQueryResult Run()
        {
            if (m_p_connection == null)
            {
                using (m_p_connection = m_p_data_base.GetConnection())
                {
                    DbCommand p_command = CreateCommand();
                    m_p_data_base.RaiseCommandExecuted(this, p_command);
                    return RunAsCommand(p_command);
                }
            }
            else
            {
                DbCommand p_command = CreateCommand();
                m_p_data_base.RaiseCommandExecuted(this, p_command);
                return RunAsCommand(p_command);
            }
        }
        protected abstract CDataBaseQueryResult RunAsCommand(DbCommand p_command);

        private DbCommand CreateCommand()
        {
            DbCommand p_cmd = m_p_connection.CreateCommand();
            p_cmd.Transaction = m_p_trans_action;
            PrepareCommand(p_cmd);
            p_cmd.Prepare();
            return p_cmd;
        }
        protected abstract void PrepareCommand(DbCommand p_cmd);
    }
}
