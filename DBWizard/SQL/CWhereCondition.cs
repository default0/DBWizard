using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard.SQL
{
    /// <summary>
    /// Represents a condition that can be used in a where-clause.
    /// </summary>
    internal class CWhereCondition
    {
        /// <summary>
        /// The column this condition should check (fe "id").
        /// </summary>
        internal String m_p_column_name { get; private set; }
        /// <summary>
        /// The operation this condition should perform (fe "=" or "&lt;").
        /// </summary>
        internal String m_p_operation { get; private set; }
        /// <summary>
        /// The value this condition should check against (fe 5, "test", othercolumn).
        /// </summary>
        internal Object m_p_expected_value { get; private set; }

        /// <summary>
        /// The operator used to connect this condition a nested connection, if one exists.
        /// </summary>
        internal EBooleanOperator m_boolean_operator { get; private set; }
        /// <summary>
        /// The nested connection this condition is connected with, or null if there is no nested connection.
        /// </summary>
        internal CWhereCondition m_p_nested_condition { get; private set; }

        /// <summary>
        /// Constructs a new condition that can be used in a where-clause.
        /// </summary>
        /// <param name="p_column_name">The column the new condition should check (fe "id").</param>
        /// <param name="p_operation">The operation the new condition should perform (fe "=" or "&lt;").</param>
        /// <param name="p_expected_value">The value the new condition should check against (fe 5, "test", othercolumn).</param>
        internal CWhereCondition(String p_column_name, String p_operation, Object p_expected_value)
            : this(p_column_name, p_operation, p_expected_value, (CWhereCondition)null, EBooleanOperator.and)
        {
        }
        /// <summary>
        /// Constructs a new condition that can be used in a where-clause.
        /// </summary>
        /// <param name="p_column_name">The column the new condition should check (fe "id").</param>
        /// <param name="p_operation">The operation the new condition should perform (fe "=" or "&lt;").</param>
        /// <param name="p_expected_value">The value the new condition should check against (fe 5, "test", othercolumn).</param>
        /// <param name="p_nested_condition">The condition that this condition should be chained with, or null.</param>
        /// <param name="boolean_operator">The operator that this condition should use to chain with the given nested condition.</param>
        internal CWhereCondition(String p_column_name, String p_operation, Object p_expected_value, CWhereCondition p_nested_condition, EBooleanOperator boolean_operator)
        {
            m_p_column_name = p_column_name;
            m_p_operation = p_operation;
            m_p_expected_value = p_expected_value;

            m_p_nested_condition = p_nested_condition;
            m_boolean_operator = boolean_operator;
        }
        /// <summary>
        /// Constructs a new condition that can be used in a where-clause.
        /// </summary>
        /// <param name="p_conditions">The conditions that this condition should be composed of. Needs to have at least one element.</param>
        internal CWhereCondition(CWhereCondition[] p_conditions)
        {
            m_p_column_name = p_conditions[0].m_p_column_name;
            m_p_operation = p_conditions[0].m_p_operation;
            m_p_expected_value = p_conditions[0].m_p_expected_value;
            m_boolean_operator = p_conditions[0].m_boolean_operator;

            for (Int32 i = p_conditions.Length - 1; i > 0; --i)
            {
                m_p_nested_condition = new SQL.CWhereCondition(
                    p_conditions[i].m_p_column_name,
                    p_conditions[i].m_p_operation,
                    p_conditions[i].m_p_expected_value,
                    m_p_nested_condition,
                    p_conditions[i].m_boolean_operator
                );
            }
        }

        internal DbParameter[] GetWhereParams(DbCommand p_cmd, CObjectMap p_map)
        {
            List<DbParameter> p_params = new List<DbParameter>();
            GetWhereParams(p_cmd, p_map, p_params, 0);
            return p_params.ToArray();
        }
        private void GetWhereParams(DbCommand p_cmd, CObjectMap p_map, List<DbParameter> p_params, Int32 depth)
        {
            SStorePrimitiveOptions primitive_options;
            if (!p_map.m_p_primitives_map.TryGetValue(m_p_column_name, out primitive_options))
            {
                throw new Exception("No values for column \"" + m_p_column_name + "\" loaded when trying to load parameters for type \"" + p_map.m_p_object_type.FullName + "\".");
            }
            DbParameter p_param = p_cmd.CreateParameter();
            p_param.ParameterName = "@whereparam" + depth.ToString();
            p_param.Value = CHelper.MakePrimitiveType(CHelper.ToValueString(m_p_expected_value), primitive_options.m_primitive_type);
            p_param.DbType = primitive_options.m_primitive_type.ToDbType();
            if (primitive_options.m_primitive_type.RequiresLength())
            {
                if (m_p_expected_value is Array)
                {
                    p_param.Size = ((Array)m_p_expected_value).Length;
                }
                else if(m_p_expected_value is String)
                {
                    p_param.Size = ((String)m_p_expected_value).Length;
                }
                else
                {
                    throw new InvalidOperationException("A db primitive type that requires a length is not an array.");
                }
            }
            p_params.Add(p_param);
            if (m_p_nested_condition != null)
            {
                m_p_nested_condition.GetWhereParams(p_cmd, p_map, p_params, depth + 1);
            }
        }

        /// <summary>
        /// Returns the entire where clause represented by this where condition, fe: age&lt;20 AND name="Dennis"
        /// </summary>
        /// <returns>The entire where clause represented by this where condition, fe: age&lt;20 AND name="Dennis"</returns>
        internal String GetWhereClause()
        {
            return GetWhereClause(0);
        }
        private String GetWhereClause(Int32 depth)
        {
            StringBuilder p_clause = new StringBuilder();
            //p_clause.Append('`');
            p_clause.Append(m_p_column_name);
            //p_clause.Append('`');
            p_clause.Append(m_p_operation);
            p_clause.Append("@whereparam");
            p_clause.Append(depth.ToString());
            if (m_p_nested_condition != null)
            {
                switch (m_boolean_operator)
                {
                    case EBooleanOperator.and:
                        p_clause.Append(" AND ");
                        break;
                    case EBooleanOperator.or:
                        p_clause.Append(" OR ");
                        break;
                }
                p_clause.Append(m_p_nested_condition.GetWhereClause(depth + 1));
            }
            return p_clause.ToString();
        }
    }
}
