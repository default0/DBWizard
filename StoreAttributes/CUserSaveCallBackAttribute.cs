using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard.StoreAttributes
{
    /// <summary>
    /// Represents a user-defined save callback attribute that marks a method to be called when saving certain data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CUserSaveCallBackAttribute : Attribute
    {
        /// <summary>
        /// The name of the value that when saving should trigger execution of the marked method.
        /// </summary>
        public String m_p_value_name { get; private set; }

        /// <summary>
        /// Constructs a new user-defined save callback attribute to call the marked method when the value with the given name is being processed during saving.
        /// </summary>
        /// <param name="p_value_name">The name of the value that when saving should trigger execution of the marked method.</param>
        public CUserSaveCallBackAttribute(String p_value_name)
        {
            if (p_value_name == null) throw new ArgumentNullException("You must provide a value name.");

            m_p_value_name = p_value_name;
        }
    }
}
