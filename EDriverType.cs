using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBWizard
{
    /// <summary>
    /// Represents the type of the database driver that should be used.
    /// </summary>
    public enum EDriverType
    {
        /// <summary>
        /// Mysql driver type, uses MySQL Server Dialect.
        /// </summary>
        mysql,
        /// <summary>
        /// Odbc driver type, uses the Microsoft SQL Server Dialect.
        /// </summary>
        mssql
    }
}
