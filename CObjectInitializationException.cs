using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard
{
    /// <summary>
    /// Represents an exception that occured during initialization of an object.
    /// </summary>
    public class CObjectInitializationException : Exception
    {
        public CObjectInitializationException(String p_message, Exception p_inner_exception)
            : base(p_message, p_inner_exception)
        {

        }

        public override String ToString()
        {
            return base.ToString() + InnerException.ToString();
        }
    }
}
