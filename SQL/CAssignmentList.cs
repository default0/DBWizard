using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard.SQL
{
	/// <summary>
	/// Represents a list of assignments in an update query.
	/// </summary>
	internal class CAssignmentList
	{
		/// <summary>
		/// The assignments that are to be made.
		/// </summary>
		internal ReadOnlyDictionary<String, String> m_p_assignments { get; private set; }

		/// <summary>
		/// Constructs a new assignment list from the given argument list in the form: key0, value0, key1, value1, etc.
		/// </summary>
		/// <param name="p_assignments">The list of key-value pairs that form the assignments in the form: key0, value0, key1, value1, etc.</param>
		internal CAssignmentList(params String[] p_assignments)
		{
			if ((p_assignments.Length & 1) == 1) throw new ArgumentException("The given assignments array has an odd length and thus cannot be mapped as key-value pairs.");

			Dictionary<String, String> p_assignments_dict = new Dictionary<String, String>();
			for (Int32 i = 0; i < p_assignments.Length; ++i)
			{
				p_assignments_dict.Add(p_assignments[i], p_assignments[i + 1]);
			}
			m_p_assignments = new ReadOnlyDictionary<String, String>(p_assignments_dict);
		}

		/// <summary>
		/// Returns an array of database parameters for this assignment.
		/// </summary>
		/// <param name="p_cmd">The command to create the parameters with.</param>
		/// <returns>An array containing the created database parameters.</returns>
		internal DbParameter[] GetAssignmentParameters(DbCommand p_cmd, CObjectMap p_map)
		{
			DbParameter[] p_params = new DbParameter[m_p_assignments.Count];
			Int32 index = 0;
			foreach (KeyValuePair<String, String> assignment in m_p_assignments)
			{
				DbParameter p_param = p_cmd.CreateParameter();
				p_param.ParameterName = "@assignparam" + index.ToString();
				p_param.Value = CHelper.MakePrimitiveType(assignment.Value, p_map.m_p_primitives_map[assignment.Key].m_primitive_type);
				p_param.DbType = p_map.m_p_primitives_map[assignment.Key].m_primitive_type.ToDbType();
				if (p_map.m_p_primitives_map[assignment.Key].m_primitive_type.RequiresLength())
				{
					p_param.Size = assignment.Value.Length;
				}
				p_params[index] = p_param;
				++index;
			}

			return p_params;
		}
		/// <summary>
		/// Returns a list of assignments to insert into a query, fe: column1=12,column2=test.
		/// </summary>
		/// <returns>A list of assignments to insert into a query, fe: column1=12,column2=test.</returns>
		internal String GetAssignmentText()
		{
			StringBuilder p_assignment_text = new StringBuilder();

			Int32 index = 0;
			foreach (KeyValuePair<String, String> assignment in m_p_assignments)
			{
				++index;
				p_assignment_text.Append(assignment.Key);
				p_assignment_text.Append('=');
				p_assignment_text.Append("@assignparam");
				p_assignment_text.Append((index - 1).ToString());
				if (index < m_p_assignments.Count)
					p_assignment_text.Append(',');
			}

			return p_assignment_text.ToString();
		}
	}
}
