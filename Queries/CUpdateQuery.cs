using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard.Queries
{
	internal class CUpdateQuery : CDataBaseQuery
	{
		internal String m_p_table_name { get; private set; }
		internal SQL.CAssignmentList m_p_assignments { get; private set; }
		internal SQL.CWhereCondition m_p_update_condition { get; private set; }

		private CObjectMap _m_p_map;

		internal CUpdateQuery(CDataBase p_data_base, CObjectMap p_map, String p_table_name, SQL.CAssignmentList p_assignments, SQL.CWhereCondition p_update_condition)
			: base(p_data_base)
		{
			if (p_map == null) throw new ArgumentNullException("You must specify an object map to use.");
			if (p_table_name == null) throw new ArgumentNullException("You must specify a table name to use.");
			if (String.IsNullOrWhiteSpace(p_table_name)) throw new ArgumentException("The specified table name may not consist only of whitespace.");
			if (p_assignments == null) throw new ArgumentNullException("You must specify assignments to use for the query.");

			m_p_table_name = p_table_name;
			m_p_assignments = p_assignments;
			m_p_update_condition = p_update_condition;
			_m_p_map = p_map;
		}

		protected override void PrepareCommand(DbCommand p_cmd)
		{
			StringBuilder p_cmd_text = new StringBuilder();

			p_cmd_text.Append("UPDATE ");
			p_cmd_text.Append(m_p_table_name);
			p_cmd_text.Append(" SET ");
			p_cmd_text.Append(m_p_assignments.GetAssignmentText());
			if (m_p_update_condition != null)
			{
				p_cmd_text.Append(" WHERE ");
				p_cmd_text.Append(m_p_update_condition.GetWhereClause());
				p_cmd.Parameters.AddRange(m_p_update_condition.GetWhereParams(p_cmd, _m_p_map));
			}

			p_cmd.CommandText = p_cmd_text.ToString();
			p_cmd.Parameters.AddRange(m_p_assignments.GetAssignmentParameters(p_cmd, _m_p_map));
		}

		protected override CDataBaseQueryResult RunAsCommand(DbCommand p_command)
		{
			CDataBaseQueryResult p_result = new CDataBaseQueryResult(
				this,
				p_command.ExecuteNonQuery()
			);
			return p_result;
		}
		protected override async Task<CDataBaseQueryResult> RunAsCommandAsync(DbCommand p_command)
		{
			CDataBaseQueryResult p_result = new CDataBaseQueryResult(
				this,
				await p_command.ExecuteNonQueryAsync()
			);
			return p_result;
		}
	}
}
