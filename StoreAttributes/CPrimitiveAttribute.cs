using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard.StoreAttributes
{
	/// <summary>
	/// Represents an attribute that marks a field to be stored as a primitive type given by the specified type-code. Can additionally be set to automatically
	/// convert to the given type, if necessary.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
	public class CPrimitiveAttribute : Attribute
	{
		/// <summary>
		/// The primitive type that should be used to store data.
		/// </summary>
		public EDBPrimitive m_primitive_type { get; private set; }

		/// <summary>
		/// The name of the column to store the primitive value in.
		/// </summary>
		public String m_p_column_name { get; private set; }

		/// <summary>
		/// Whether the associated field will be automatically converted to the given primitive type.
		/// </summary>
		public Boolean m_auto_convert { get; private set; }

		/// <summary>
		/// The way the primitive data should be loaded into the Object.
		/// </summary>
		public EStoreOptions m_store_options { get; private set; }

		/// <summary>
		/// Constructs a new primitive store attribute with the given column name and auto conversion turned off.
		/// </summary>
		/// <param name="p_column_name">The name of the column to store the data in.</param>
		public CPrimitiveAttribute(String p_column_name)
			: this(EDBPrimitive.infer, p_column_name, false, EStoreOptions.direct_assignment)
		{

		}
		/// <summary>
		/// Constructs a new primitive store attribute with the given primitive type and column name and auto conversion turned off.
		/// </summary>
		/// <param name="primitive_type">The primitive type to store.</param>
		/// <param name="p_column_name">The name of the column to store the data in.</param>
		public CPrimitiveAttribute(EDBPrimitive primitive_type, String p_column_name)
			: this(primitive_type, p_column_name, false, EStoreOptions.direct_assignment)
		{
			
		}

		/// <summary>
		/// Constructs a new primitive store attribute with the given primitive type, column name and auto conversion option.
		/// </summary>
		/// <param name="primitive_type">The primitive type to store.</param>
		/// <param name="p_column_name">The name of the column to store the data in.</param>
		/// <param name="auto_convert">Whether the field this attribute is associated with should be automatically converted to the given primitive type.</param>
		/// <param name="store_options">Specifies how the data should be loaded.</param>
		public CPrimitiveAttribute(EDBPrimitive primitive_type, String p_column_name, Boolean auto_convert, EStoreOptions store_options)
		{
			switch (store_options)
			{
				case EStoreOptions.none:
					throw new ArgumentException("ELoadOptions.none is not a valid option.");
				case EStoreOptions.direct_assignment:
				case EStoreOptions.user_callback:
					break;
				default:
					throw new ArgumentException(store_options.ToString() + " is not a valid option.");
			}
			switch (primitive_type)
			{
				case EDBPrimitive.none:
					throw new ArgumentException("EDBPrimitive.none is not a valid primitive-type to store data with.");
				case EDBPrimitive.binary:
				case EDBPrimitive.bit:
				case EDBPrimitive.boolean:
				case EDBPrimitive.@char:
				case EDBPrimitive.@decimal:
				case EDBPrimitive.@double:
				case EDBPrimitive.@float:
				case EDBPrimitive.int16:
				case EDBPrimitive.int24:
				case EDBPrimitive.int32:
				case EDBPrimitive.int64:
				case EDBPrimitive.int8:
				case EDBPrimitive.uint16:
				case EDBPrimitive.uint24:
				case EDBPrimitive.uint32:
				case EDBPrimitive.uint64:
				case EDBPrimitive.uint8:
				case EDBPrimitive.varbinary:
				case EDBPrimitive.varchar:
				case EDBPrimitive.date:
				case EDBPrimitive.datetime:
				case EDBPrimitive.time:
				case EDBPrimitive.timestamp:
				case EDBPrimitive.year:
				case EDBPrimitive.text:
				case EDBPrimitive.infer:
					break;
				default:
					throw new ArgumentException(primitive_type.ToString() + " is not a valid primitive-type to store data with.");
			}

			m_primitive_type = primitive_type;
			m_p_column_name = p_column_name;
			m_auto_convert = auto_convert;
			m_store_options = store_options;
		}
	}
}
