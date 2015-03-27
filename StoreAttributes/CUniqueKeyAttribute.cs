using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard.StoreAttributes
{
	/// <summary>
	/// Represents an attribute that marks the fields that are part of the primary key and uniquely identify the Object. Every field that is marked with
	/// this attribute must also be marked with a CPrimitiveAttribute.
	/// </summary>
	public class CUniqueKeyAttribute : Attribute
	{
		/// <summary>
		/// Whether the primary key uses an auto incrementing value.
		/// </summary>
		public Boolean m_is_identity { get; private set; }

		/// <summary>
		/// Constructs a new primary key attribute that can be marked as using an auto incrementing value.
		/// </summary>
		/// <param name="is_identity">Whether this key is an identity key.</param>
		public CUniqueKeyAttribute(Boolean is_identity)
		{
			m_is_identity = is_identity;
		}
	}
}
