using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard.StoreAttributes
{
	/// <summary>
	/// Represents a user-defined load callback attribute that marks a method to be called when loading certain data.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class CUserLoadCallBackAttribute : Attribute
	{
		/// <summary>
		/// The name of the value that when loading should trigger execution of the marked method.
		/// </summary>
		public String m_p_value_name { get; private set; }

		/// <summary>
		/// Constructs a new user-defined load callback attribute to call the marked method when the value with the given name is being processed during loading.
		/// </summary>
		/// <param name="p_value_name">The name of the value that when loading should trigger execution of the marked method.</param>
		public CUserLoadCallBackAttribute(String p_value_name)
		{
			if (p_value_name == null) throw new ArgumentNullException("You must provide a value name.");

			m_p_value_name = p_value_name;
		}
	}
}
