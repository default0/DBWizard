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

        /// <summary>
        /// Specifies that the column is a signed tinyint in the database. This is automatically inferred if the member is of type <see cref="System.SByte"/>.
        /// </summary>
        int8,
        /// <summary>
        /// Specifies that the column is an unsigned tinyint in the database. This is automatically inferred if the member is of type <see cref="System.Byte"/>.
        /// </summary>
        uint8,

        /// <summary>
        /// Specifies that the column is a signed smallint in the database. This is automatically inferred if the member is of type <see cref="System.Int16"/>.
        /// </summary>
        int16,
        /// <summary>
        /// Specifies that the column is an unsigned smallint in the database. This is automatically inferred if the member is of type <see cref="System.UInt16"/>.
        /// </summary>
        uint16,

        /// <summary>
        /// Specifies that the column is a signed mediumint in the database. No type is being inferred to this.
        /// </summary>
        int24,
        /// <summary>
        /// Specifies that the column is an unsigned mediumint in the database. No type is being inferred to this.
        /// </summary>
        uint24,

        /// <summary>
        /// Specifies that the column is a signed int in the database. This is automatically inferred if the member is of type <see cref="System.Int32"/>.
        /// </summary>
        int32,
        /// <summary>
        /// Specifies that the column is an unsigned int in the database. This is automatically inferred if the member is of type <see cref="System.UInt32"/>.
        /// </summary>
        uint32,

        /// <summary>
        /// Specifies that the column is a signed bigint in the database. This is automatically inferred if the member is of type <see cref="System.Int64"/>.
        /// </summary>
        int64,
        /// <summary>
        /// Specifies that the column is an unsigned bigint in the database. This is automatically inferred if the member is of type <see cref="System.UInt64"/>.
        /// </summary>
        uint64,

        /// <summary>
        /// Specifies that the column is a decimal in the database. This is automatically inferred if the member is of type <see cref="System.Decimal"/>
        /// </summary>
        @decimal,
        /// <summary>
        /// Specifies that the column is a float in the database. This is automatically inferred if the member is of type <see cref="System.Single"/>
        /// </summary>
        @float,
        /// <summary>
        /// Specifies that the column is a double in the database. This is automatically inferred if the member is of type <see cref="System.Double"/>
        /// </summary>
        @double,

        /// <summary>
        /// Specifies that the column is a bit in the database. No type is being inferred to this.
        /// </summary>
        bit,
        /// <summary>
        /// Specifies that the column is a boolean in the database. This is automatically inferred if the member is of type <see cref="System.Boolean"/>
        /// </summary>
        boolean,

        /// <summary>
        /// Specifies that the column is a char in the database. No type is being inferred to this.
        /// </summary>
        @char,
        /// <summary>
        /// Specifies that the column is a varchar in the database. This is automatically inferred if the member is of type <see cref="System.String"/>
        /// </summary>
        varchar,
        /// <summary>
        /// Specifies that the column is a text in the database. No type is being inferred to this.
        /// </summary>
        text,

        /// <summary>
        /// Specifies that the column is a binary in the database. No type is being inferred to this.
        /// </summary>
        binary,
        /// <summary>
        /// Specifies that the column is a varbinary in the database. This is automatically inferred if the member is of type <see cref="System.Byte[]"/>
        /// </summary>
        varbinary,

        /// <summary>
        /// Specifies that the column is a date in the database. No type is being inferred to this.
        /// </summary>
        date,
        /// <summary>
        /// Specifies that the column is a datetime in the database. This is automatically inferred if the member is of type <see cref="System.DateTime"/>
        /// </summary>
        datetime,
        /// <summary>
        /// Specifies that the column is a timestamp in the database. No type is being inferred to this.
        /// </summary>
        timestamp,
        /// <summary>
        /// Specifies that the column is a time in the database. This is automatically inferred if the member is of type <see cref="System.TimeStamp"/>
        /// </summary>
        time,
        /// <summary>
        /// Specifies that the column is a year in the database. No type is being inferred to this.
        /// </summary>
        year,

        /// <summary>
        /// Infers the actual primitive type from the field type.
        /// </summary>
        infer
    }
}
