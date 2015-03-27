using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard.StoreAttributes
{
	/// <summary>
	/// Represents an attribute that marks a field to be referring from many objects to many objects stored externally, related via a special relation table.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class CManyToManyAttribute : Attribute
	{
		/// <summary>
		/// The value name to identify the callback that handles the loaded data.
		/// </summary>
		public String m_p_value_name { get; private set; }
		/// <summary>
		/// The foreign key linking the many source objects to the relation table.
		/// </summary>
		public CForeignKey m_p_source_to_relation_link { get; private set; }
		/// <summary>
		/// The foreign key linking the relation table to the many destination objects.
		/// </summary>
		public CForeignKey m_p_relation_to_target_link { get; private set; }

		/// <summary>
		/// Constructs a new store many to many attribute linking many source objects to many destination objects using the primary keys and tables defined through attributes on the relevant types as well as the given relation table.
		/// </summary>
		/// <param name="p_value_name">The value name to identify the callback that handles the loaded data.</param>
		/// <param name="p_relation_table">The relation table to use for linking the objects.</param>
		public CManyToManyAttribute(String p_value_name, String p_relation_table)
		{
			if (p_value_name == null) throw new ArgumentNullException("The value name may not be null.");
			m_p_value_name = p_value_name;

			if (p_relation_table == null) throw new ArgumentNullException("The relation table may not be null");
			if (p_relation_table.Length == 0) throw new ArgumentException("Please specify at least one character for the name of the relation table.");

			// null data will be filled in by attribute processors
			m_p_source_to_relation_link = new CForeignKey(null, (String)null, p_relation_table, (String)null);
			m_p_relation_to_target_link = new CForeignKey(p_relation_table, (String)null, null, (String)null);
		}

		/// <summary>
		/// Constructs a new store many to many attribute linking many source objects to many destination objects using the given columns.
		/// </summary>
		/// <param name="p_value_name">The value name to identify the callback that handles the loaded data.</param>
		/// <param name="p_relation_table">The relation table to use for linking the objects.</param>
		public CManyToManyAttribute(String p_value_name, String p_source_column, String p_target_column, String p_relation_table)
		{
			if (p_value_name == null) throw new ArgumentNullException("The value name may not be null.");
			m_p_value_name = p_value_name;

			if (p_source_column == null || p_target_column == null) throw new ArgumentNullException("Neither the source column nor the target column may be null. Consider constructing only with a relation table given instead.");
			if (p_source_column == p_target_column) throw new ArgumentException("The names of the source and target column may not be the same.");
			if (p_source_column.Length == 0 || p_target_column.Length == 0) throw new ArgumentException("Neither the name of the source column nor the name of the target column may be empty strings.");
			if (p_relation_table == null) throw new ArgumentNullException("The relation table may not be null");
			if (p_relation_table.Length == 0) throw new ArgumentException("Please specify at least one character for the name of the relation table.");


			m_p_source_to_relation_link = new CForeignKey(null, p_source_column, p_relation_table, p_source_column);
			m_p_relation_to_target_link = new CForeignKey(p_relation_table, p_target_column, null, p_target_column);
		}
	}
}
