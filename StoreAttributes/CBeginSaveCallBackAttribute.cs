using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard.StoreAttributes
{
	/// <summary>
	/// Represents a begin save callback attribute that marks a method to be called before saving Object data.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class CBeginSaveCallBackAttribute : Attribute
	{
	}
}
