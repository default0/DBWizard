using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard
{
    /// <summary>
    /// Represents a status of the db wizard.
    /// </summary>
    public class CDBWizardStatus
    {
        /// <summary>
        /// The status message.
        /// </summary>
        public String m_p_message { get; private set; }

        /// <summary>
        /// The status code.
        /// </summary>
        public EDBWizardStatusCode m_status_code { get; private set; }

        /// <summary>
        /// Additional data associated with the status.
        /// </summary>
        public Object m_p_data { get; private set; }

        /// <summary>
        /// Indicates whether the given status contains any errors.
        /// </summary>
        public Boolean IsError { get { return m_status_code != EDBWizardStatusCode.success; } }

        /// <summary>
        /// Construct a new dbwizard status from the given status code.
        /// </summary>
        /// <param name="status_code">The status code to construct the new status from.</param>
        public CDBWizardStatus(EDBWizardStatusCode status_code)
        {
            switch (status_code)
            {
                case EDBWizardStatusCode.err_no_object_found:
                    m_p_message = "No Object was found.";
                    break;
                case EDBWizardStatusCode.err_multiple_objects_found:
                    m_p_message = "Multiple objects were found.";
                    break;
                case EDBWizardStatusCode.success:
                    m_p_message = "Success";
                    break;
                case EDBWizardStatusCode.err_exception_thrown:
                    m_p_message = "An exception was thrown";
                    break;
                default:
                    throw new ArgumentException("No valid status code was given.");
            }
            m_status_code = status_code;
        }
        /// <summary>
        /// Construct a new dbwizard status from the given status code and data.
        /// </summary>
        /// <param name="status_code">The status code to construct the new status from.</param>
        /// <param name="p_data">The data the status should hold.</param>
        public CDBWizardStatus(EDBWizardStatusCode status_code, Object p_data)
            : this(status_code)
        {
            m_p_data = p_data;
        }


        public override String ToString()
        {
            if (m_p_data != null)
            {
                return "Database Operation Status: " + m_p_message + "\nCode: " + m_status_code.ToString() + "\nData: " + m_p_data.ToString();
            }
            else
            {
                return "Database Operation Status: " + m_p_message + "\nCode: " + m_status_code.ToString();
            }
        }
    }
}
