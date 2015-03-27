using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard
{
	public enum EDBPrimitive
	{
		none = 0,

		int8,
		uint8,

		int16,
		uint16,

		int24,
		uint24,

		int32,
		uint32,

		int64,
		uint64,

		@decimal,
		@float,
		@double,

		bit,
		boolean,

		@char,
		varchar,
		text,

		binary,
		varbinary,

		date,
		datetime,
		timestamp,
		time,
		year,

		/// <summary>
		/// Infers the actual primitive type from the field type.
		/// </summary>
		infer
	}
}
