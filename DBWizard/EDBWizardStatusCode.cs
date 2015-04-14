using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard
{
    /// <summary>
    /// Represents a status code that is used to indicate the status of an operation.
    /// </summary>
    public enum EDBWizardStatusCode
    {
        success = 0,

        err_no_object_found = 10,
        err_multiple_objects_found = 11,

        err_exception_thrown = 20,
    }
}
