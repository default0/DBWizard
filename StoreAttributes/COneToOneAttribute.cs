using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard.StoreAttributes
{
	/// <summary>
	/// Represents an attribute that marks a field to be stored in a different table and referenced from this column, creating a 1:1 relationship.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class COneToOneAttribute : Attribute
	{
		/// <summary>
		/// The value name to identify the callback that handles the loaded data.
		/// </summary>
		public String m_p_value_name { get; private set; }
		/// <summary>
		/// The names of the columns to store the value of the foreign columns in.
		/// </summary>
		public ReadOnlyCollection<String> m_p_source_column_names { get; private set; }

		/// <summary>
		/// The foreign columns that the related Object can be identified with.
		/// </summary>
		public ReadOnlyCollection<String> m_p_foreign_column_names { get; private set; }

		/// <summary>
		/// Constructs a new store single attribute with the given column name and the primary key of the foreign Object.
		/// </summary>
		/// <param name="p_value_name">The value name to identify the callback that handles the loaded data.</param>
		/// <param name="p_column_name">The name of the column to store the value of the foreign primary key in.</param>
		public COneToOneAttribute(String p_value_name, String p_column_name)
		{
			m_p_value_name = p_value_name;
			m_p_source_column_names = new ReadOnlyCollection<String>(new String[] { p_column_name });
			m_p_foreign_column_names = new ReadOnlyCollection<String>(new String[0]);
		}
		/// <summary>
		/// Constructs a new store single attribute with the given column names and the primary key of the foreign Object.
		/// </summary>
		/// <param name="p_value_name">The value name to identify the callback that handles the loaded data.</param>
		/// <param name="p_column_name">The name of the columns to store the value of the foreign primary key in.</param>
		public COneToOneAttribute(String p_value_name, String[] p_column_names)
		{
			m_p_value_name = p_value_name;
			m_p_source_column_names = new ReadOnlyCollection<String>(p_column_names);
			m_p_foreign_column_names = new ReadOnlyCollection<String>(new String[0]);
		}
		/// <summary>
		/// Constructs a new store single attribute with the given column and foreign column names.
		/// </summary>
		/// <param name="p_value_name">The value name to identify the callback that handles the loaded data.</param>
		/// <param name="p_column_name">The name of the column to store the value of the foreign column in.</param>
		/// <param name="p_foreign_column_name">The name of the foreign column that can be used to identify the Object.</param>
		public COneToOneAttribute(String p_value_name, String p_column_name, String p_foreign_column_name)
		{
			m_p_value_name = p_value_name;
			m_p_source_column_names = new ReadOnlyCollection<String>(new String[] { p_column_name });
			m_p_foreign_column_names = new ReadOnlyCollection<String>(new String[] { p_foreign_column_name });
		}
		/// <summary>
		/// Constructs a new store single attribute with the given columns and foreign columns names.
		/// </summary>
		/// <param name="p_value_name">The value name to identify the callback that handles the loaded data.</param>
		/// <param name="p_column_name">The names of the columns to store the values of the foreign columns in.</param>
		/// <param name="p_foreign_column_name">The names of the foreign columns that can be used to identify the Object.</param>
		public COneToOneAttribute(String p_value_name, String[] p_column_names, String[] p_foreign_column_names)
		{
			m_p_value_name = p_value_name;
			m_p_source_column_names = new ReadOnlyCollection<String>(p_column_names);
			m_p_foreign_column_names = new ReadOnlyCollection<String>(p_foreign_column_names);
		}
	}
}
