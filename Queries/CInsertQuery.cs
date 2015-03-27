using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard.Queries
{
	internal class CInsertQuery : CDataBaseQuery
	{
		internal String m_p_table_name { get; private set; }

		internal ReadOnlyCollection<String> m_p_keys { get; private set; }

		internal ReadOnlyCollection<String> m_p_column_names { get; private set; }
		internal ReadOnlyCollection<ReadOnlyCollection<Object>> m_p_values { get; private set; }

		internal Boolean m_on_duplicate_key_update { get; private set; }

		private CObjectMap _m_p_map;

		private Boolean _m_has_key;

		internal CInsertQuery(CDataBase p_data_base, Boolean update_on_duplicate_key, CObjectMap p_object_map, String[] p_column_names, params Object[][] p_values)
			: base(p_data_base)
		{
			if (p_object_map == null) throw new ArgumentNullException("You must specify an object map to use.");
			if (p_column_names == null) throw new ArgumentNullException("You must specify columns names to use.");
			if (p_values == null) throw new ArgumentNullException("You must specify values to use.");
			for (Int32 i = 0; i < p_values.Length; ++i)
			{
				if (p_values[i].Length != p_column_names.Length) throw new ArgumentException("One set of values did not have exactly the same length as the column names provided.");
			}

			m_p_keys = new ReadOnlyCollection<String>(p_object_map.m_p_unique_keys.Select(x => x.m_p_column_name).ToArray());
			m_on_duplicate_key_update = update_on_duplicate_key;
			m_p_table_name = p_object_map.m_p_object_table;
			m_p_column_names = new ReadOnlyCollection<String>((IList<String>)p_column_names.Clone());
			m_p_values = new ReadOnlyCollection<ReadOnlyCollection<Object>>((from p_value in p_values select new ReadOnlyCollection<Object>(p_value)).ToArray());

			_m_p_map = p_object_map;

			_m_has_key = false;
			for (Int32 i = 0; i < m_p_keys.Count; ++i)
			{
				if (m_p_column_names.Contains(m_p_keys[i]))
				{
					_m_has_key = true;
					break;
				}
			}
		}

		protected override void PrepareCommand(DbCommand p_cmd)
		{
			StringBuilder p_cmd_text = new StringBuilder();
			if (m_on_duplicate_key_update && m_p_data_base.m_driver_type == EDriverType.mssql)
			{
				// ms sql specific
				p_cmd_text.Append("MERGE INTO ");
				p_cmd_text.Append(m_p_table_name);
				p_cmd_text.Append(" As __DB_WIZARD_MERGE_TARGET__ USING (SELECT ");
				// get all values that correspond to keys
				for (Int32 i = 0; i < m_p_values.Count; ++i)
				{
					ReadOnlyCollection<Object> p_values = m_p_values[i];
					for (Int32 j = 0; j < p_values.Count; ++j)
					{
						p_cmd_text.Append("@insertval_");
						p_cmd_text.Append(i.ToString());
						p_cmd_text.Append('_');
						p_cmd_text.Append(j.ToString());

						if ((j + 1) < p_values.Count)
							p_cmd_text.Append(',');
					}
					if ((i + 1) < m_p_values.Count)
						p_cmd_text.Append(" UNION ALL SELECT ");
				}
				p_cmd_text.Append(") As __DB_WIZARD_MERGE_SOURCE__ (");
				for (Int32 i = 0; i < m_p_column_names.Count; ++i)
				{
					p_cmd_text.Append(m_p_column_names[i]);
					if ((i + 1) < m_p_column_names.Count)
						p_cmd_text.Append(',');
				}
				p_cmd_text.Append(")ON(");
				Boolean is_first = true;
				if (m_p_keys.Count > 0)
				{
					for (Int32 i = 0; i < m_p_keys.Count; ++i)
					{
						if (m_p_column_names.Contains(m_p_keys[i]))
						{
							if (!is_first)
							{
								p_cmd_text.Append(" AND ");
							}
							is_first = false;
							p_cmd_text.Append("__DB_WIZARD_MERGE_TARGET__.");
							p_cmd_text.Append(m_p_keys[i]);
							p_cmd_text.Append(" = __DB_WIZARD_MERGE_SOURCE__.");
							p_cmd_text.Append(m_p_keys[i]);
						}
					}
				}
				else
				{
					for (Int32 i = 0; i < m_p_column_names.Count; ++i)
					{
						if (!is_first)
						{
							p_cmd_text.Append(" AND ");
						}
						is_first = false;
						p_cmd_text.Append("__DB_WIZARD_MERGE_TARGET__.");
						p_cmd_text.Append(m_p_column_names[i]);
						p_cmd_text.Append(" = __DB_WIZARD_MERGE_SOURCE__.");
						p_cmd_text.Append(m_p_column_names[i]);
					}
				}
				p_cmd_text.Append(")WHEN MATCHED THEN UPDATE SET ");
				for (Int32 i = 0; i < m_p_column_names.Count; ++i)
				{
					if (m_p_keys.Contains(m_p_column_names[i]))
						continue;

					p_cmd_text.Append("__DB_WIZARD_MERGE_TARGET__.");
					p_cmd_text.Append(m_p_column_names[i]);
					p_cmd_text.Append('=');
					p_cmd_text.Append("__DB_WIZARD_MERGE_SOURCE__.");
					p_cmd_text.Append(m_p_column_names[i]);

					if ((i + 1) < m_p_column_names.Count)
						p_cmd_text.Append(',');
				}
				p_cmd_text.Append(" WHEN NOT MATCHED THEN INSERT (");
				for (Int32 i = 0; i < m_p_column_names.Count; ++i)
				{
					if (m_p_keys.Contains(m_p_column_names[i]))
						continue;

					p_cmd_text.Append(m_p_column_names[i]);
					if ((i + 1) < m_p_column_names.Count)
						p_cmd_text.Append(',');
				}
				p_cmd_text.Append(")VALUES(");
				for (Int32 i = 0; i < m_p_column_names.Count; ++i)
				{
					if (m_p_keys.Contains(m_p_column_names[i]))
						continue;

					p_cmd_text.Append("__DB_WIZARD_MERGE_SOURCE__.");
					p_cmd_text.Append(m_p_column_names[i]);
					if ((i + 1) < m_p_column_names.Count)
						p_cmd_text.Append(',');
				}
				p_cmd_text.Append(");");
			}
			else
			{
				p_cmd_text.Append("INSERT INTO ");
				p_cmd_text.Append(m_p_table_name);
				p_cmd_text.Append('(');
				for (Int32 i = 0; i < m_p_column_names.Count; ++i)
				{
					p_cmd_text.Append(m_p_column_names[i]);
					if ((i + 1) < m_p_column_names.Count) p_cmd_text.Append(',');
				}
				p_cmd_text.Append(") VALUES ");
				for (Int32 i = 0; i < m_p_values.Count; ++i)
				{
					ReadOnlyCollection<Object> p_values = m_p_values[i];
					p_cmd_text.Append('(');
					for (Int32 j = 0; j < p_values.Count; ++j)
					{
						p_cmd_text.Append("@insertval_");
						p_cmd_text.Append(i.ToString());
						p_cmd_text.Append('_');
						p_cmd_text.Append(j.ToString());
						if ((j + 1) < p_values.Count) p_cmd_text.Append(',');
					}
					p_cmd_text.Append(')');
					if ((i + 1) < m_p_values.Count) p_cmd_text.Append(',');
				}
				if (m_on_duplicate_key_update) // mysql specific
				{
					p_cmd_text.Append(" ON DUPLICATE KEY UPDATE ");
					for (Int32 i = 0; i < m_p_column_names.Count; ++i)
					{
						p_cmd_text.Append(m_p_column_names[i]);
						p_cmd_text.Append("=VALUES(");
						p_cmd_text.Append(m_p_column_names[i]);
						p_cmd_text.Append(')');
						if ((i + 1) < m_p_column_names.Count) p_cmd_text.Append(',');
					}
				}
			}

			p_cmd.CommandText = p_cmd_text.ToString();
			for(Int32 i = 0; i < m_p_values.Count; ++i)
			{
				ReadOnlyCollection<Object> p_values = m_p_values[i];
				for(Int32 j = 0; j < p_values.Count; ++j)
				{
					EDBPrimitive primitive_type = _m_p_map.m_p_primitives_map[m_p_column_names[j]].m_primitive_type;
					DbParameter p_param = p_cmd.CreateParameter();
					p_param.ParameterName = "@insertval_" + i.ToString() + '_' + j.ToString();
					p_param.Value = p_values[j];
					p_param.DbType = primitive_type.ToDbType();
					if (primitive_type.RequiresLength())
					{
						if (p_values[j] is Array)
						{
							p_param.Size = ((Array)p_values[j]).Length;
							if (p_param.Size == 0)
								p_param.Size = 1;
						}
						else
						{
							p_param.Size = p_values[j].ToString().Length;
						}
					}
					if (primitive_type.RequiresPrecisionAndScale())
					{
                        if (m_p_data_base.m_driver_type == EDriverType.mssql)
                        {
                            ((SqlParameter)p_param).Precision = 18;
                            ((SqlParameter)p_param).Scale = 0;
                        }
                        else if(m_p_data_base.m_driver_type == EDriverType.mysql)
                        {
                            ((MySqlParameter)p_param).Precision = 18;
                            ((MySqlParameter)p_param).Scale = 0;
                        }
					}
					p_cmd.Parameters.Add(p_param);
				}
			}
		}


		protected override CDataBaseQueryResult RunAsCommand(DbCommand p_command)
		{
			Int32 n_rows_affected = p_command.ExecuteNonQuery();

			if (m_p_data_base.m_driver_type == EDriverType.mysql)
			{
				MySqlCommand p_mysql_cmd = p_command as MySqlCommand;
				if (p_mysql_cmd == null)
					throw new Exception("Database with mysql driver type executed non-mysql insert command.");

				if (p_mysql_cmd.LastInsertedId != 0)
				{
					return new CDataBaseQueryResult(this, n_rows_affected, p_mysql_cmd.LastInsertedId);
				}
				else
				{
					return new CDataBaseQueryResult(this, n_rows_affected);
				}
			}
			else if (m_p_data_base.m_driver_type == EDriverType.mssql)
			{
				SqlCommand p_get_last_insert_id_cmd = new SqlCommand();
				p_get_last_insert_id_cmd.Transaction = p_command.Transaction as SqlTransaction;
				p_get_last_insert_id_cmd.Connection = p_command.Connection as SqlConnection;
				p_get_last_insert_id_cmd.CommandText = "SELECT @@IDENTITY;";

				Object p_scalar = p_get_last_insert_id_cmd.ExecuteScalar();
				if (p_scalar is DBNull)
				{
					return new CDataBaseQueryResult(this, n_rows_affected);
				}
				else
				{
					Int64 last_insert_id = CHelper.SafeCastObjectToInt64(p_scalar);
					if (last_insert_id != 0)
					{
						return new CDataBaseQueryResult(this, n_rows_affected, last_insert_id);
					}
					else
					{
						return new CDataBaseQueryResult(this, n_rows_affected);
					}
				}
			}
			else
			{
				return new CDataBaseQueryResult(this, n_rows_affected);
			}
		}
		protected override async Task<CDataBaseQueryResult> RunAsCommandAsync(DbCommand p_command)
		{
			Int32 n_rows_affected = await p_command.ExecuteNonQueryAsync();
			
			if(m_p_data_base.m_driver_type == EDriverType.mysql)
			{
				MySqlCommand p_mysql_cmd = p_command as MySqlCommand;
				if(p_mysql_cmd == null)
					throw new Exception("Database with mysql driver type executed non-mysql insert command.");

				if (p_mysql_cmd.LastInsertedId != 0)
				{
					return new CDataBaseQueryResult(this, n_rows_affected, p_mysql_cmd.LastInsertedId);
				}
				else
				{
					return new CDataBaseQueryResult(this, n_rows_affected);
				}
			}
			else if(m_p_data_base.m_driver_type == EDriverType.mssql)
			{
				SqlCommand p_get_last_insert_id_cmd = new SqlCommand();
				p_get_last_insert_id_cmd.Transaction = p_command.Transaction as SqlTransaction;
				p_get_last_insert_id_cmd.Connection = p_command.Connection as SqlConnection;
				p_get_last_insert_id_cmd.CommandText = "SELECT @@IDENTITY;";

				Object p_scalar = await p_get_last_insert_id_cmd.ExecuteScalarAsync();
				if (p_scalar is DBNull)
				{
					return new CDataBaseQueryResult(this, n_rows_affected);
				}
				else
				{
					Int64 last_insert_id = CHelper.SafeCastObjectToInt64(p_scalar);
					if (last_insert_id != 0)
					{
						return new CDataBaseQueryResult(this, n_rows_affected, last_insert_id);
					}
					else
					{
						return new CDataBaseQueryResult(this, n_rows_affected);
					}
				}
			}
			else
			{
				return new CDataBaseQueryResult(this, n_rows_affected);
			}
		}
	}
}
