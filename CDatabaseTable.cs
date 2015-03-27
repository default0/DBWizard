using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace DBWizard
{
	public class CDatabaseTable
	{
		public string m_p_name;

		public CDataBaseRow this[Int32 index]
		{
			get { return m_p_rows[index]; }
		}

		public List<CDataBaseRow> m_p_rows;
		public CDatabaseTable m_p_schema_table;
		public CDataBaseRow m_p_metadata;
		public CDatabaseTable m_p_index_table;
		public CDatabaseTable m_p_index_column_table;
		public CDataBaseRow m_p_database_info;


		public CDatabaseTable()
		{
			m_p_rows = new List<CDataBaseRow>();
		}
		public CDatabaseTable(DataTable p_table) : this()
		{
			foreach (DataRow p_row in p_table.Rows)
			{
				CDataBaseRow p_object = new CDataBaseRow();
				for (Int32 i = 0; i < p_table.Columns.Count; ++i)
				{
					p_object.Set<Object>(p_table.Columns[i].ColumnName, p_row.Field<object>(i));
				}
				m_p_rows.Add(p_object);
			}
		}

		public List<TResult> FindAll<TSearch, TResult>(string p_search_column, TSearch comparison_value, string p_result_column)
		{
			List<TResult> p_list = new List<TResult>();
			foreach (CDataBaseRow p_row in m_p_rows)
			{
				if (p_row.Get<TSearch>(p_search_column).Equals(comparison_value))
				{
					p_list.Add(p_row.Get<TResult>(p_result_column));
				}
			}
			return p_list;
		}
	}
}
