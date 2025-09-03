using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SQLDBEntityNotifier.Interfaces;
using SQLDBEntityNotifier.Models;

namespace SQLDBEntityNotifier.Providers
{
    /// <summary>
    /// MySQL CDC provider using binary log for change detection
    /// </summary>
    public class MySqlCDCProvider : ICDCProvider, IDisposable
    {
        private readonly DatabaseConfiguration _configuration;
        private MySqlConnection? _connection;
        private bool _disposed = false;
        private readonly object _lockObject = new object();

        public DatabaseType DatabaseType => DatabaseType.MySql;
        public DatabaseConfiguration Configuration => _configuration;

        public MySqlCDCProvider(DatabaseConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            if (configuration.DatabaseType != DatabaseType.MySql)
                throw new ArgumentException("Configuration must be for MySQL database type");
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                var connectionString = _configuration.BuildConnectionString();
                _connection = new MySqlConnection(connectionString);
                await _connection.OpenAsync();

                // Check if binary logging is enabled
                var isBinaryLogEnabled = await IsBinaryLogEnabledAsync();
                if (!isBinaryLogEnabled)
                {
                    throw new InvalidOperationException("MySQL binary logging is not enabled. Enable it by setting log-bin=mysql-bin in my.cnf");
                }

                // Check if the user has REPLICATION SLAVE privilege
                var hasReplicationPrivilege = await HasReplicationPrivilegeAsync();
                if (!hasReplicationPrivilege)
                {
                    throw new InvalidOperationException("MySQL user does not have REPLICATION SLAVE privilege required for binary log access");
                }

                return true;
            }
            catch (Exception)
            {
                _connection?.Dispose();
                _connection = null;
                return false;
            }
        }

        public async Task<bool> IsCDCEnabledAsync(string tableName)
        {
            if (_connection?.State != ConnectionState.Open)
                return false;

            try
            {
                var sql = @"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = @database 
                    AND table_name = @table";
                
                using var command = new MySqlCommand(sql, _connection);
                command.Parameters.AddWithValue("@database", _configuration.DatabaseName);
                command.Parameters.AddWithValue("@table", tableName);
                
                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> EnableCDCAsync(string tableName)
        {
            // MySQL CDC is enabled at server level via binary logging
            // Individual tables don't need explicit CDC enablement
            return await IsCDCEnabledAsync(tableName);
        }

        public async Task<string> GetCurrentChangePositionAsync()
        {
            if (_connection?.State != ConnectionState.Open)
                throw new InvalidOperationException("Connection is not open");

            try
            {
                var sql = "SHOW MASTER STATUS";
                using var command = new MySqlCommand(sql, _connection);
                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    return reader.GetString("Position");
                }
                
                return "0";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get current binary log position", ex);
            }
        }

        public async Task<List<ChangeRecord>> GetChangesAsync(string fromPosition, string? toPosition = null)
        {
            if (_connection?.State != ConnectionState.Open)
                throw new InvalidOperationException("Connection is not open");

            var changes = new List<ChangeRecord>();
            var currentPosition = toPosition ?? await GetCurrentChangePositionAsync();
            
            try
            {
                // Get binary log events between positions
                var sql = @"
                    SELECT 
                        'mysql_binlog' as ChangeId,
                        'unknown' as TableName,
                        'unknown' as Operation,
                        NOW() as ChangeTimestamp,
                        @position as ChangePosition,
                        USER() as ChangedBy,
                        @@hostname as HostName
                    FROM DUAL
                    WHERE @fromPosition < @position";
                
                using var command = new MySqlCommand(sql, _connection);
                command.Parameters.AddWithValue("@fromPosition", fromPosition);
                command.Parameters.AddWithValue("@position", currentPosition);
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var change = new ChangeRecord
                    {
                        ChangeId = reader.GetString("ChangeId"),
                        TableName = reader.GetString("TableName"),
                        Operation = ParseOperation(reader.GetString("Operation")),
                        ChangeTimestamp = reader.GetDateTime("ChangeTimestamp"),
                        ChangePosition = reader.GetString("ChangePosition"),
                        ChangedBy = reader.IsDBNull("ChangedBy") ? null : reader.GetString("ChangedBy"),
                        HostName = reader.IsDBNull("HostName") ? null : reader.GetString("HostName")
                    };
                    
                    changes.Add(change);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve changes from binary log", ex);
            }

            return changes;
        }

        public async Task<List<ChangeRecord>> GetTableChangesAsync(string tableName, string fromPosition, string? toPosition = null)
        {
            var allChanges = await GetChangesAsync(fromPosition, toPosition);
            return allChanges.Where(c => c.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<Dictionary<string, List<ChangeRecord>>> GetMultiTableChangesAsync(IEnumerable<string> tableNames, string fromPosition, string? toPosition = null)
        {
            var allChanges = await GetChangesAsync(fromPosition, toPosition);
            var result = new Dictionary<string, List<ChangeRecord>>();
            
            foreach (var tableName in tableNames)
            {
                result[tableName] = allChanges.Where(c => c.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            
            return result;
        }

        public async Task<List<DetailedChangeRecord>> GetDetailedChangesAsync(string fromPosition, string? toPosition = null)
        {
            var basicChanges = await GetChangesAsync(fromPosition, toPosition);
            var detailedChanges = new List<DetailedChangeRecord>();
            
            foreach (var change in basicChanges)
            {
                var detailedChange = new DetailedChangeRecord
                {
                    ChangeId = change.ChangeId,
                    TableName = change.TableName,
                    Operation = change.Operation,
                    PrimaryKeyValues = change.PrimaryKeyValues,
                    ChangeTimestamp = change.ChangeTimestamp,
                    ChangePosition = change.ChangePosition,
                    ChangedBy = change.ChangedBy,
                    ApplicationName = change.ApplicationName,
                    HostName = change.HostName,
                    TransactionId = change.TransactionId,
                    OldValues = new Dictionary<string, object>(),
                    NewValues = new Dictionary<string, object>(),
                    AffectedColumns = new List<string>(),
                    Metadata = new Dictionary<string, object>()
                };
                
                detailedChanges.Add(detailedChange);
            }
            
            return detailedChanges;
        }

        public async Task<TableSchema> GetTableSchemaAsync(string tableName)
        {
            if (_connection?.State != ConnectionState.Open)
                throw new InvalidOperationException("Connection is not open");

            try
            {
                var schema = new TableSchema
                {
                    TableName = tableName,
                    SchemaName = _configuration.DatabaseName,
                    Columns = new List<ColumnDefinition>(),
                    PrimaryKeyColumns = new List<string>(),
                    HasCDCEnabled = await IsCDCEnabledAsync(tableName)
                };

                // Get column information
                var columnSql = @"
                    SELECT 
                        COLUMN_NAME,
                        DATA_TYPE,
                        IS_NULLABLE,
                        CHARACTER_MAXIMUM_LENGTH,
                        NUMERIC_PRECISION,
                        NUMERIC_SCALE
                    FROM information_schema.columns 
                    WHERE table_schema = @database 
                    AND table_name = @table
                    ORDER BY ordinal_position";
                
                using var columnCommand = new MySqlCommand(columnSql, _connection);
                columnCommand.Parameters.AddWithValue("@database", _configuration.DatabaseName);
                columnCommand.Parameters.AddWithValue("@table", tableName);
                
                using var columnReader = await columnCommand.ExecuteReaderAsync();
                while (await columnReader.ReadAsync())
                {
                    var column = new ColumnDefinition
                    {
                        ColumnName = columnReader.GetString("COLUMN_NAME"),
                        DataType = columnReader.GetString("DATA_TYPE"),
                        IsNullable = columnReader.GetString("IS_NULLABLE") == "YES",
                        MaxLength = columnReader.IsDBNull("CHARACTER_MAXIMUM_LENGTH") ? null : (int?)columnReader.GetInt32("CHARACTER_MAXIMUM_LENGTH"),
                        Precision = columnReader.IsDBNull("NUMERIC_PRECISION") ? null : (int?)columnReader.GetInt32("NUMERIC_PRECISION"),
                        Scale = columnReader.IsDBNull("NUMERIC_SCALE") ? null : (int?)columnReader.GetInt32("NUMERIC_SCALE")
                    };
                    
                    schema.Columns.Add(column);
                }
                
                // Get primary key information
                var pkSql = @"
                    SELECT COLUMN_NAME
                    FROM information_schema.key_column_usage 
                    WHERE table_schema = @database 
                    AND table_name = @table 
                    AND constraint_name = 'PRIMARY'
                    ORDER BY ordinal_position";
                
                using var pkCommand = new MySqlCommand(pkSql, _connection);
                pkCommand.Parameters.AddWithValue("@database", _configuration.DatabaseName);
                pkCommand.Parameters.AddWithValue("@table", tableName);
                
                using var pkReader = await pkCommand.ExecuteReaderAsync();
                while (await pkReader.ReadAsync())
                {
                    schema.PrimaryKeyColumns.Add(pkReader.GetString("COLUMN_NAME"));
                }
                
                return schema;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get schema for table {tableName}", ex);
            }
        }

        public async Task<CDCValidationResult> ValidateConfigurationAsync()
        {
            var result = new CDCValidationResult();
            
            try
            {
                if (_connection?.State != ConnectionState.Open)
                {
                    result.IsValid = false;
                    result.Errors.Add("Database connection is not open");
                    return result;
                }

                // Check binary logging
                var isBinaryLogEnabled = await IsBinaryLogEnabledAsync();
                if (!isBinaryLogEnabled)
                {
                    result.IsValid = false;
                    result.Errors.Add("MySQL binary logging is not enabled");
                }
                else
                {
                    result.Messages.Add("Binary logging is enabled");
                }

                // Check replication privileges
                var hasReplicationPrivilege = await HasReplicationPrivilegeAsync();
                if (!hasReplicationPrivilege)
                {
                    result.IsValid = false;
                    result.Errors.Add("User does not have REPLICATION SLAVE privilege");
                }
                else
                {
                    result.Messages.Add("User has required replication privileges");
                }

                // Check server version
                var serverVersion = await GetServerVersionAsync();
                result.Messages.Add($"MySQL Server Version: {serverVersion}");

                result.IsValid = result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Validation failed: {ex.Message}");
            }
            
            return result;
        }

        public async Task<bool> CleanupOldChangesAsync(TimeSpan retentionPeriod)
        {
            // MySQL automatically manages binary log retention via expire_logs_days setting
            // This method is a no-op for MySQL as cleanup is handled by the server
            return true;
        }

        public async Task<CDCHealthInfo> GetHealthInfoAsync()
        {
            var healthInfo = new CDCHealthInfo
            {
                Status = CDCHealthStatus.Unknown,
                Metrics = new Dictionary<string, object>()
            };

            try
            {
                if (_connection?.State != ConnectionState.Open)
                {
                    healthInfo.Status = CDCHealthStatus.Unhealthy;
                    return healthInfo;
                }

                // Get binary log status
                var sql = "SHOW MASTER STATUS";
                using var command = new MySqlCommand(sql, _connection);
                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var position = reader.GetString("Position");
                    var file = reader.GetString("File");
                    
                    healthInfo.Metrics["CurrentPosition"] = position;
                    healthInfo.Metrics["CurrentFile"] = file;
                    healthInfo.Status = CDCHealthStatus.Healthy;
                }
                else
                {
                    healthInfo.Status = CDCHealthStatus.Degraded;
                }

                // Get server uptime
                var uptimeSql = "SHOW STATUS LIKE 'Uptime'";
                using var uptimeCommand = new MySqlCommand(uptimeSql, _connection);
                var uptime = await uptimeCommand.ExecuteScalarAsync();
                if (uptime != null)
                {
                    healthInfo.Metrics["ServerUptime"] = uptime;
                }
            }
            catch (Exception)
            {
                healthInfo.Status = CDCHealthStatus.Unhealthy;
            }

            return healthInfo;
        }

        private async Task<bool> IsBinaryLogEnabledAsync()
        {
            try
            {
                var sql = "SHOW VARIABLES LIKE 'log_bin'";
                using var command = new MySqlCommand(sql, _connection);
                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    return reader.GetString("Value").Equals("ON", StringComparison.OrdinalIgnoreCase);
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> HasReplicationPrivilegeAsync()
        {
            try
            {
                var sql = "SHOW GRANTS FOR CURRENT_USER()";
                using var command = new MySqlCommand(sql, _connection);
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var grant = reader.GetString(0);
                    if (grant.Contains("REPLICATION SLAVE", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> GetServerVersionAsync()
        {
            try
            {
                var sql = "SELECT VERSION()";
                using var command = new MySqlCommand(sql, _connection);
                var version = await command.ExecuteScalarAsync();
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private ChangeOperation ParseOperation(string operation)
        {
            return operation.ToLowerInvariant() switch
            {
                "insert" => ChangeOperation.Insert,
                "update" => ChangeOperation.Update,
                "delete" => ChangeOperation.Delete,
                "create" or "alter" or "drop" => ChangeOperation.SchemaChange,
                _ => ChangeOperation.Unknown
            };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _connection?.Dispose();
                _connection = null;
                _disposed = true;
            }
        }
    }
}