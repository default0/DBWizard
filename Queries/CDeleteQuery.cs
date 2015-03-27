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
	/// Represents a delete query deleting all columns that match certain criteria.
	/// </summary>
	internal class CDeleteQuery : CDataBaseQuery
	{
		/// <summary>
		/// The table to delete rows from.
		/// </summary>
		internal String m_p_table_name { get; private set; }
		/// <summary>
		/// The conditions that must be met for a row to get deleted.
		/// </summary>
		internal SQL.CWhereCondition m_p_where_condition { get; private set; }

		// total number of rows to delete, or 0 if all matched rows should be deleted.
		private UInt64 m_limit;

		// the object map this query is created for.
		private CObjectMap _m_p_map;

		/// <summary>
		/// Constructs a new delete query to delete columns matching the given condition from the given table.
		/// </summary>
		/// <param name="p_data_base">The database the query should be run against.</param>
		/// <param name="p_map">The object map this query is being created for.</param>
		/// <param name="p_table_name">The name of the table to delete rows from.</param>
		/// <param name="p_where_condition">The condition a row must satisfy to be deleted.</param>
		/// <param name="limit">The total number of rows to delete, or 0 if every matched row should be deleted.</param>
		internal CDeleteQuery(CDataBase p_data_base, CObjectMap p_map, String p_table_name, SQL.CWhereCondition p_where_condition, UInt64 limit)
			: base(p_data_base)
		{
			if (p_map == null) throw new ArgumentNullException("The map may not be null.");
			if (p_table_name == null) throw new ArgumentNullException("You must specify a table name to use.");
			if (String.IsNullOrWhiteSpace(p_table_name)) throw new ArgumentException("The specified table name may not consist only of whitespace.");

			m_p_table_name = p_table_name;
			m_p_where_condition = p_where_condition;

			m_limit = limit;

			_m_p_map = p_map;
		}

		protected override void PrepareCommand(DbCommand p_cmd)
		{
			StringBuilder p_cmd_text = new StringBuilder();

			if (m_p_data_base.m_driver_type == EDriverType.mssql)
			{
				p_cmd_text.Append("DELETE ");
				if(m_limit != 0)
				{
					p_cmd_text.Append("TOP (");
					p_cmd_text.Append(m_limit.ToString());
					p_cmd_text.Append(") ");
				}
				p_cmd_text.Append("FROM ");
				p_cmd_text.Append(m_p_table_name);
				p_cmd_text.Append(" WHERE ");
				p_cmd_text.Append(m_p_where_condition.GetWhereClause());

				p_cmd.CommandText = p_cmd_text.ToString();
			}
			else if (m_p_data_base.m_driver_type == EDriverType.mysql)
			{
				p_cmd_text.Append("DELETE FROM ");
				p_cmd_text.Append(m_p_table_name);
				p_cmd_text.Append(" WHERE ");
				p_cmd_text.Append(m_p_where_condition.GetWhereClause());
				if (m_limit != 0)
				{
					p_cmd_text.Append(" LIMIT ");
					p_cmd_text.Append(m_limit.ToString());
				}

				p_cmd.CommandText = p_cmd_text.ToString();
			}
			p_cmd.Parameters.AddRange(m_p_where_condition.GetWhereParams(p_cmd, _m_p_map));
		}


		protected override CDataBaseQueryResult RunAsCommand(DbCommand p_command)
		{
			try
			{
				Int32 n_affected_rows = p_command.ExecuteNonQuery();

				CDataBaseQueryResult p_result = new CDataBaseQueryResult(this, n_affected_rows);
				return p_result;
			}
			catch (Exception p_except)
			{
				throw p_except;
			}
		}
		protected override async Task<CDataBaseQueryResult> RunAsCommandAsync(DbCommand p_command)
		{
			try
			{
				Int32 n_affected_rows = await p_command.ExecuteNonQueryAsync();
				
				CDataBaseQueryResult p_result = new CDataBaseQueryResult(this, n_affected_rows);
				return p_result;
			}
			catch (Exception p_except)
			{
				throw p_except;
			}
		}
	}
}
