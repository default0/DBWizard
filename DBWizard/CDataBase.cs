using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard
{
    /// <summary>
    /// Represents a database that can be accessed in various ways. Think of this more like a running mysql instance than a mysql database.
    /// </summary>
    public class CDataBase
    {
        /// <summary>
        /// If true, you can construct databases even if some of your classes use primitives that are not supported by the driver type of the constructed database.<br/>
        /// This can be useful if your application needs to interface multiple databases. Set to false by default to catch potential errors early.
        /// </summary>
        public static Boolean s_allow_invalid_primitives = false;

        static CDataBase()
        {
            CTypeManager.Initialize();
        }

        /// <summary>
        /// The connection String the database uses to interact with a running mysql instance.
        /// </summary>
        public String m_p_connection_string { get; private set; }

        /// <summary>
        /// The type of the driver this data base uses.
        /// </summary>
        public EDriverType m_driver_type { get; private set; }

        internal HashSet<EDBPrimitive> m_p_supported_primitives;

        public event EventHandler<DbCommand> CommandExecuted;

        /// <summary>
        /// Constructs a new database connecting to a mysql server with the given String. This makes a blocking, synchronous connection attempt to test the connection String.
        /// </summary>
        /// <param name="p_connection_string">The connection String to use.</param>
        public CDataBase(String p_connection_string, EDriverType driver_type)
        {
            m_p_connection_string = p_connection_string;
            m_driver_type = driver_type;
            switch (driver_type)
            {
                case EDriverType.mysql:
                    m_p_supported_primitives = new HashSet<EDBPrimitive>()
                    {
                        EDBPrimitive.@decimal,
                        EDBPrimitive.int8,
                        EDBPrimitive.int16,
                        EDBPrimitive.int24,
                        EDBPrimitive.int32,
                        EDBPrimitive.int64,
                        EDBPrimitive.uint8,
                        EDBPrimitive.uint16,
                        EDBPrimitive.uint24,
                        EDBPrimitive.uint32,
                        EDBPrimitive.uint64,
                        EDBPrimitive.timestamp,
                        EDBPrimitive.date,
                        EDBPrimitive.time,
                        EDBPrimitive.datetime,
                        EDBPrimitive.year,
                        EDBPrimitive.varbinary,
                        EDBPrimitive.binary,
                        EDBPrimitive.varchar,
                        EDBPrimitive.@char,
                        EDBPrimitive.text,
                        EDBPrimitive.bit,
                        EDBPrimitive.@float,
                        EDBPrimitive.@double,
                        EDBPrimitive.boolean
                    };

                    using (MySqlConnection p_mysql_connection = new MySqlConnection())
                    {
                        p_mysql_connection.ConnectionString = p_connection_string;
                        try
                        {
                            p_mysql_connection.Open();
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                    break;
                case EDriverType.mssql:

                    m_p_supported_primitives = new HashSet<EDBPrimitive>()
                    {
                        EDBPrimitive.int64,
                        EDBPrimitive.binary,
                        EDBPrimitive.boolean,
                        EDBPrimitive.@char,
                        EDBPrimitive.datetime,
                        EDBPrimitive.@decimal,
                        EDBPrimitive.@double,
                        EDBPrimitive.varbinary,
                        EDBPrimitive.int32,
                        EDBPrimitive.text,
                        EDBPrimitive.varchar,
                        EDBPrimitive.@float,
                        EDBPrimitive.int16,
                        EDBPrimitive.uint8,
                        EDBPrimitive.varbinary,
                        EDBPrimitive.date,
                        EDBPrimitive.time,
                    };

                    using (SqlConnection p_mssql_connection = new SqlConnection())
                    {
                        p_mssql_connection.ConnectionString = p_connection_string;
                        try
                        {
                            p_mssql_connection.Open();
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                    break;
                default:
                    throw new Exception("Invalid driver type specified.");
            }
            if (!CDataBase.s_allow_invalid_primitives)
            {
                CObjectMap.ThrowOnIncompatibility(this);
            }
        }

        /// <summary>
        /// Executes the given query as scalar. The query is executed as is, so make sure your calls are XSS safe.
        /// </summary>
        /// <param name="p_query">The query that should be run.</param>
        /// <returns>The scalar the query returned.</returns>
        public Object ExecuteScalar(String p_query)
        {
            using (DbConnection p_connection = GetConnection())
            {
                using (DbCommand p_command = p_connection.CreateCommand())
                {
                    p_command.CommandText = p_query;

                    RaiseCommandExecuted(this, p_command);

                    return p_command.ExecuteScalar();
                }
            }
        }
        /// <summary>
        /// Executes the given query as scalar. The query is executed as is, so make sure your calls are XSS safe.
        /// </summary>
        /// <param name="p_query">The query that should be run.</param>
        /// <returns>The scalar the query returned.</returns>
        public async Task<Object> ExecuteScalarAsync(String p_query)
        {
            using(DbConnection p_connection = (await GetConnectionAsync()))
            {
                using (DbCommand p_command = p_connection.CreateCommand())
                {
                    p_command.CommandText = p_query;

                    RaiseCommandExecuted(this, p_command);

                    return await p_command.ExecuteScalarAsync();
                }
            }
        }

        public CDataBaseResultSet ExecuteReader(String p_query)
        {
            using (DbConnection p_connection = GetConnection())
            {
                using (DbCommand p_command = p_connection.CreateCommand())
                {
                    p_command.CommandText = p_query;

                    RaiseCommandExecuted(this, p_command);

                    Queries.CDataBaseQueryResult p_result = new Queries.CDataBaseQueryResult(null);
                    p_result.RetrieveFromReader(p_command.ExecuteReader());
                    return p_result.m_p_result_set;
                }
            }
        }
        public async Task<CDataBaseResultSet> ExecuteReaderAsync(String p_query)
        {
            using (DbConnection p_connection = (await GetConnectionAsync()))
            {
                using (DbCommand p_command = p_connection.CreateCommand())
                {
                    p_command.CommandText = p_query;

                    RaiseCommandExecuted(this, p_command);

                    Queries.CDataBaseQueryResult p_result = new Queries.CDataBaseQueryResult(null);
                    await p_result.RetrieveFromReaderAsync(await p_command.ExecuteReaderAsync());
                    return p_result.m_p_result_set;
                }
            }
        }

        /// <summary>
        /// Loads an Object of the given type with the given primary key. The criteria must uniquely identify an Object, otherwise an exception is thrown.
        /// </summary>
        /// <typeparam name="T">The type of the Object to load.</typeparam>
        /// <param name="p_primary_key_value">The criteria the loaded Object must match.</param>
        /// <returns>A status Object containing information about the success/failure of the operation.</returns>
        public CDBWizardStatus LoadClass<T>(T p_obj, Object p_primary_key_value) where T : class
        {
            CObjectMap p_object_map = CObjectMap.Get(p_obj.GetType());

            if (p_object_map.m_p_begin_load_call_back != null)
            {
                p_object_map.m_p_begin_load_call_back(p_obj);
            }

            CDataBaseObject p_db_obj = new CDataBaseObject(p_object_map);
            if (p_object_map.m_p_unique_keys.Count == 0)
            {
                throw new Exception("The class \"" + typeof(T).FullName + "\" does not define a primary key.");
            }
            CDBWizardStatus p_status = p_object_map.LoadObject(
                this,
                new SQL.CWhereCondition(p_object_map.m_p_unique_keys[0].m_p_column_name, "=", p_primary_key_value.ToString()),
                p_db_obj
            );
            if (!p_status.IsError)
            {
                try
                {
                    p_db_obj.MapToClass(p_obj);

                    if (p_object_map.m_p_end_load_call_back != null)
                    {
                        p_object_map.m_p_end_load_call_back(p_obj);
                    }
                }
                catch (Exception p_except)
                {
                    return new CDBWizardStatus(EDBWizardStatusCode.err_exception_thrown, p_except);
                }
            }
            return p_status;
        }
        /// <summary>
        /// Loads an Object of the given type with the given criteria. The criteria must uniquely identify an Object, otherwise an exception is thrown.
        /// </summary>
        /// <typeparam name="T">The type of the Object to load.</typeparam>
        /// <param name="p_object_conditions">The criteria the loaded Object must match.</param>
        /// <returns>The loaded Object that matched the criteria. If no Object matches the criteria, default(T) is returned, if multiple objects matched, an error is thrown.</returns>
        public CDBWizardStatus LoadClass<T>(T p_obj, params KeyValuePair<String, Object>[] p_object_conditions) where T : class
        {
            CObjectMap p_object_map = CObjectMap.Get(p_obj.GetType());

            if (p_object_map.m_p_begin_load_call_back != null)
            {
                p_object_map.m_p_begin_load_call_back(p_obj);
            }

            SQL.CWhereCondition[] p_conditions = new SQL.CWhereCondition[p_object_conditions.Length];
            for (Int32 i = 0; i < p_object_conditions.Length; ++i)
            {
                p_conditions[i] = new SQL.CWhereCondition(p_object_conditions[i].Key, "=", p_object_conditions[i].Value.ToString());
            }

            CDataBaseObject p_db_obj = new CDataBaseObject(p_object_map);
            CDBWizardStatus p_status = p_object_map.LoadObject(
                this,
                new SQL.CWhereCondition(p_conditions),
                p_db_obj
            );
            if (!p_status.IsError)
            {
                try
                {
                    p_db_obj.MapToClass(p_obj);

                    if (p_object_map.m_p_end_load_call_back != null)
                    {
                        p_object_map.m_p_end_load_call_back(p_obj);
                    }
                }
                catch (Exception p_except)
                {
                    return new CDBWizardStatus(EDBWizardStatusCode.err_exception_thrown, p_except);
                }
            }
            return p_status;
        }
        /// <summary>
        /// Loads the database object for the given type with the given primary key value. The criteria must uniquely identify an object, otherwise an exception is thrown.
        /// </summary>
        /// <typeparam name="T">The type of the object to load.</typeparam>
        /// <param name="p_primary_key_value">The primary key value that should be used to search the database.</param>
        /// <returns>A tuple containing status-object that can be used to retrieve information about the success/failure of the operation as well as the database object that was retrieved, or null if the status indicates an error.</returns>
        public Tuple<CDBWizardStatus, CDataBaseObject> LoadObject<T>(Object p_primary_key_value)
        {
            return LoadObject(typeof(T), p_primary_key_value);
        }
        public Tuple<CDBWizardStatus, CDataBaseObject> LoadObject(Type p_object_type, Object p_primary_key_value)
        {
            CObjectMap p_object_map = CObjectMap.Get(p_object_type);

            CDataBaseObject p_result = new CDataBaseObject(p_object_map);
            if (p_object_map.m_p_unique_keys.Count == 0)
            {
                throw new Exception("The class \"" + p_object_type.FullName + "\" does not define a primary key.");
            }
            CDBWizardStatus p_status = p_object_map.LoadObject(
                this,
                new SQL.CWhereCondition(p_object_map.m_p_unique_keys[0].m_p_column_name, "=", p_primary_key_value.ToString()),
                p_result
            );
            if (p_status.IsError)
                p_result = null;

            return new Tuple<CDBWizardStatus, CDataBaseObject>(p_status, p_result);
        }

        /// <summary>
        /// Loads an Object of the given type with the given primary key. The criteria must uniquely identify an Object, otherwise an exception is thrown.
        /// </summary>
        /// <typeparam name="T">The type of the Object to load.</typeparam>
        /// <param name="p_primary_key_value">The criteria the loaded Object must match.</param>
        /// <returns>A status Object containing information about the success/failure of the operation.</returns>
        public async Task<CDBWizardStatus> LoadClassAsync<T>(T p_obj, Object p_primary_key_value) where T : class
        {
            CObjectMap p_object_map = CObjectMap.Get(p_obj.GetType());

            if (p_object_map.m_p_begin_load_call_back != null)
            {
                p_object_map.m_p_begin_load_call_back(p_obj);
            }

            CDataBaseObject p_db_obj = new CDataBaseObject(p_object_map);
            if (p_object_map.m_p_unique_keys.Count == 0)
            {
                throw new Exception("The class \"" + typeof(T).FullName + "\" does not define a primary key.");
            }
            CDBWizardStatus p_status = await p_object_map.LoadObjectAsync(
                this,
                new SQL.CWhereCondition(p_object_map.m_p_unique_keys[0].m_p_column_name, "=", p_primary_key_value.ToString()),
                p_db_obj
            );
            if (!p_status.IsError)
            {
                try
                {
                    p_db_obj.MapToClass(p_obj);

                    if (p_object_map.m_p_end_load_call_back != null)
                    {
                        p_object_map.m_p_end_load_call_back(p_obj);
                    }
                }
                catch (Exception p_except)
                {
                    return new CDBWizardStatus(EDBWizardStatusCode.err_exception_thrown, p_except);
                }
            }
            return p_status;
        }
        /// <summary>
        /// Loads an Object of the given type with the given criteria. The criteria must uniquely identify an Object, otherwise an exception is thrown.
        /// </summary>
        /// <typeparam name="T">The type of the Object to load.</typeparam>
        /// <param name="p_object_conditions">The criteria the loaded Object must match.</param>
        /// <returns>The loaded Object that matched the criteria. If no Object matches the criteria, default(T) is returned, if multiple objects matched, an error is thrown.</returns>
        public async Task<CDBWizardStatus> LoadClassAsync<T>(T p_obj, params KeyValuePair<String, Object>[] p_object_conditions) where T : class
        {
            CObjectMap p_object_map = CObjectMap.Get(p_obj.GetType());

            if (p_object_map.m_p_begin_load_call_back != null)
            {
                p_object_map.m_p_begin_load_call_back(p_obj);
            }

            SQL.CWhereCondition[] p_conditions = new SQL.CWhereCondition[p_object_conditions.Length];
            for (Int32 i = 0; i < p_object_conditions.Length; ++i)
            {
                p_conditions[i] = new SQL.CWhereCondition(p_object_conditions[i].Key, "=", p_object_conditions[i].Value);
            }

            CDataBaseObject p_db_obj = new CDataBaseObject(p_object_map);
            CDBWizardStatus p_status = await p_object_map.LoadObjectAsync(
                this,
                new SQL.CWhereCondition(p_conditions),
                p_db_obj
            );
            if (!p_status.IsError)
            {
                try
                {
                    p_db_obj.MapToClass(p_obj);

                    if (p_object_map.m_p_end_load_call_back != null)
                    {
                        p_object_map.m_p_end_load_call_back(p_obj);
                    }
                }
                catch (Exception p_except)
                {
                    return new CDBWizardStatus(EDBWizardStatusCode.err_exception_thrown, p_except);
                }
            }
            return p_status;
        }
        /// <summary>
        /// Loads the database object for the given type with the given primary key value. The criteria must uniquely identify an object, otherwise an exception is thrown.
        /// </summary>
        /// <typeparam name="T">The type of the object to load.</typeparam>
        /// <param name="p_primary_key_value">The primary key value that should be used to search the database.</param>
        /// <returns>A tuple containing status-object that can be used to retrieve information about the success/failure of the operation as well as the database object that was retrieved, or null if the status indicates an error.</returns>
        public async Task<Tuple<CDBWizardStatus, CDataBaseObject>> LoadObjectAsync<T>(Object p_primary_key_value)
        {
            return await LoadObjectAsync(typeof(T), p_primary_key_value);
        }
        public async Task<Tuple<CDBWizardStatus, CDataBaseObject>> LoadObjectAsync(Type p_object_type, Object p_primary_key_value)
        {
            CObjectMap p_object_map = CObjectMap.Get(p_object_type);

            CDataBaseObject p_result = new CDataBaseObject(p_object_map);
            CDBWizardStatus p_status = await p_object_map.LoadObjectAsync(
                this,
                new SQL.CWhereCondition(p_object_map.m_p_unique_keys[0].m_p_column_name, "=", p_primary_key_value.ToString()),
                p_result
            );
            if (p_status.IsError)
                p_result = null;

            return new Tuple<CDBWizardStatus, CDataBaseObject>(p_status, p_result);
        }

        /// <summary>
        /// Saves an Object into the database. Returns true if the Object was successfully saved, false otherwise. The objects status is captured immediately, but the queries are run asynchronously.
        /// </summary>
        /// <param name="p_obj">The Object to save.</param>
        /// <returns>A status object indicating the success/failure of the operation.</returns>
        public CDBWizardStatus Save<T>(T p_obj)
        {
            CObjectMap p_object_map = CObjectMap.Get(p_obj.GetType());

            if (p_object_map.m_p_begin_delete_call_back != null)
            {
                p_object_map.m_p_begin_delete_call_back(p_obj);
            }

            if (p_object_map.m_p_unique_keys.Count == 0)
            {
                throw new Exception("Cannot save Object without any identifying primary keys defined");
            }

            CDBWizardStatus p_status = p_object_map.SaveObject(this, p_obj);
            if (!p_status.IsError)
            {
                if (p_object_map.m_p_end_save_call_back != null)
                {
                    p_object_map.m_p_end_save_call_back(p_obj);
                }
                return p_status;
            }
            return p_status;
        }
        /// <summary>
        /// Saves an Object into the database. Returns true if the Object was successfully saved, false otherwise. The objects status is captured immediately, but the queries are run asynchronously.
        /// </summary>
        /// <param name="p_obj">The Object to save.</param>
        /// <returns>A status object indicating the success/failure of the operation.</returns>
        public async Task<CDBWizardStatus> SaveAsync<T>(T p_obj)
        {
            CObjectMap p_object_map = CObjectMap.Get(p_obj.GetType());

            if (p_object_map.m_p_begin_delete_call_back != null)
            {
                p_object_map.m_p_begin_delete_call_back(p_obj);
            }

            if (p_object_map.m_p_unique_keys.Count == 0)
            {
                throw new Exception("Cannot save Object without any identifying primary keys defined");
            }

            CDBWizardStatus p_status = await p_object_map.SaveObjectAsync(this, p_obj);
            if (!p_status.IsError)
            {
                if (p_object_map.m_p_end_save_call_back != null)
                {
                    p_object_map.m_p_end_save_call_back(p_obj);
                }
                return p_status;
            }
            return p_status;
        }

        /// <summary>
        /// Deletes an object from the database.
        /// </summary>
        /// <param name="obj">The object that should be deleted from the database.</param>
        /// <returns>A status object indicating the success/failure of the operation</returns>
        public CDBWizardStatus Delete<T>(T obj)
        {
            CObjectMap p_object_map = CObjectMap.Get(obj.GetType());

            if (p_object_map.m_p_begin_delete_call_back != null)
            {
                p_object_map.m_p_begin_delete_call_back(obj);
            }

            if (p_object_map.m_p_unique_keys.Count == 0)
            {
                throw new Exception("Cannot delete Object without any identifying primary keys defined");
            }

            CDBWizardStatus p_status = p_object_map.DeleteObject(this, obj);
            return p_status;
        }
        /// <summary>
        /// Deletes an object from the database.
        /// </summary>
        /// <param name="obj">The object that should be deleted from the database.</param>
        /// <returns>A status object indicating the success/failure of the operation</returns>
        public async Task<CDBWizardStatus> DeleteAsync<T>(T obj)
        {
            CObjectMap p_object_map = CObjectMap.Get(obj.GetType());

            if (p_object_map.m_p_begin_delete_call_back != null)
            {
                p_object_map.m_p_begin_delete_call_back(obj);
            }

            if (p_object_map.m_p_unique_keys.Count == 0)
            {
                throw new Exception("Cannot delete Object without any identifying primary keys defined");
            }

            CDBWizardStatus p_status = await p_object_map.DeleteObjectAsync(this, obj);
            return p_status;
        }

        internal DbConnection GetConnection()
        {
            return GetConnection(10);
        }
        private DbConnection GetConnection(Int32 num_retries)
        {
            DbConnection p_connection;
            switch (m_driver_type)
            {
                case EDriverType.mysql:
                    p_connection = new MySqlConnection();
                    break;
                case EDriverType.mssql:
                    p_connection = new SqlConnection();
                    break;
                default:
                    throw new InvalidOperationException("Cannot get a connection for unknown driver type " + m_driver_type.ToString());
            }
            p_connection.ConnectionString = m_p_connection_string;
            Boolean has_faulted = false;
            Exception p_fault = null;
            try
            {
                p_connection.Open();
            }
            catch (Exception p_except)
            {
                p_fault = p_except;
                has_faulted = true;
            }
            if (has_faulted)
            {
                if (num_retries == 0)
                    throw p_fault;

                return GetConnection(num_retries - 1);
            }

            return p_connection;
        }
        internal async Task<DbConnection> GetConnectionAsync()
        {
            return await GetConnectionAsync(10);
        }
        private async Task<DbConnection> GetConnectionAsync(Int32 num_retries)
        {
            DbConnection p_connection;
            switch (m_driver_type)
            {
                case EDriverType.mysql:
                    p_connection = new MySqlConnection();
                    break;
                case EDriverType.mssql:
                    p_connection = new SqlConnection();
                    break;
                default:
                    throw new InvalidOperationException("Cannot get a connection for unknown driver type " + m_driver_type.ToString());
            }
            p_connection.ConnectionString = m_p_connection_string;
            Boolean has_faulted = false;
            Exception p_fault = null;
            try
            {
                await p_connection.OpenAsync();
            }
            catch (Exception p_except)
            {
                p_fault = p_except;
                has_faulted = true;
            }
            if (has_faulted)
            {
                if (num_retries == 0)
                    throw p_fault;

                return await GetConnectionAsync(num_retries - 1);
            }

            return p_connection;
        }

        internal void RaiseCommandExecuted(Object p_sender, DbCommand p_command)
        {
            if (CommandExecuted != null)
                CommandExecuted(p_sender, p_command);
        }
    }
}
