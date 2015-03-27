using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard.SQL
{
	/// <summary>
	/// Represents a variable list.
	/// </summary>
	internal class CVariableList
	{
		/// <summary>
		/// The variables in this variable list.
		/// </summary>
		internal ReadOnlyCollection<String> m_p_variables { get; private set; }

		/// <summary>
		/// Returns the number of variables in this variable list.
		/// </summary>
		internal Int32 Count { get { return m_p_variables.Count; } }

		/// <summary>
		/// Constructs a new variable list with the given variable names.
		/// </summary>
		/// <param name="p_variable_names">The variable names the list should contain.</param>
		internal CVariableList(params String[] p_variable_names)
		{
			if (p_variable_names == null) throw new ArgumentNullException("The given variable names may not be null.");
			if (p_variable_names.Length == 0) throw new ArgumentException("You must provide at least one variable name in a variable list.");

			p_variable_names = (String[])p_variable_names.Clone(); // prevent modification of given array.
			for (Int32 i = 0; i < p_variable_names.Length; ++i)
			{
				if (p_variable_names[i] == null) throw new ArgumentNullException("The variable names list may not contain a null reference.");
				if (String.IsNullOrWhiteSpace(p_variable_names[i])) throw new ArgumentException("The variable names list may not contain strings that are completely whitespace.");

				if (p_variable_names[i][0] != '@')
				{
					p_variable_names[i] = '@' + p_variable_names[i];
				}
			}
			m_p_variables = new ReadOnlyCollection<String>(p_variable_names);
		}

		/// <summary>
		/// Returns a String in the form: @variable0,@variable1,@variable2,...
		/// </summary>
		/// <returns>A String in the form: @variable0,@variable1,@variable2,...</returns>
		internal String GetVariableText()
		{
			StringBuilder p_variable_text = new StringBuilder();
			for (Int32 i = 0; i < m_p_variables.Count; ++i)
			{
				p_variable_text.Append(m_p_variables[i]);
				if((i + 1) < m_p_variables.Count) p_variable_text.Append(',');
			}
			return p_variable_text.ToString();
		}
	}
}
