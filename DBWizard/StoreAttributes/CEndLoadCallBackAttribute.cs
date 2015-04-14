using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard.StoreAttributes
{
    /// <summary>
    /// Represents an end load callback attribute that marks a method to be called after loading Object data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CEndLoadCallBackAttribute : Attribute
    {
    }
}
