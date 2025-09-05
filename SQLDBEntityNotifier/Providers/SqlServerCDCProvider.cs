using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SQLDBEntityNotifier.Interfaces;
using SQLDBEntityNotifier.Models;

namespace SQLDBEntityNotifier.Providers
{
    /// <summary>
    /// SQL Server CDC provider using Change Data Capture for change detection
    /// </summary>
    public class SqlServerCDCProvider : ICDCProvider, IDisposable
    {
        private readonly DatabaseConfiguration _configuration;
        private SqlConnection? _connection;
        private bool _disposed = false;
        private readonly object _lockObject = new object();

        public DatabaseType DatabaseType => DatabaseType.SqlServer;
        public DatabaseConfiguration Configuration => _configuration;

        public SqlServerCDCProvider(DatabaseConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            if (configuration.DatabaseType != DatabaseType.SqlServer)
                throw new ArgumentException("Configuration must be for SQL Server database type");
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                var connectionString = _configuration.BuildConnectionString();
                _connection = new SqlConnection(connectionString);
                await _connection.OpenAsync();

                // Check if CDC is enabled at database level
                var isCDCEnabled = await IsDatabaseCDCEnabledAsync();
                if (!isCDCEnabled)
                {
                    throw new InvalidOperationException("SQL Server CDC is not enabled at database level. Enable it using sys.sp_cdc_enable_db");
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
                    FROM sys.tables t
                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                    WHERE s.name = @schema 
                    AND t.name = @table
                    AND t.is_tracked_by_cdc = 1";
                
                using var command = new SqlCommand(sql, _connection);
                command.Parameters.AddWithValue("@schema", _configuration.SchemaName ?? "dbo");
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
            if (_connection?.State != ConnectionState.Open)
                return false;

            try
            {
                // Enable CDC for the specific table
                var sql = @"
                    EXEC sys.sp_cdc_enable_table
                        @source_schema = @schema,
                        @source_name = @table,
                        @role_name = NULL,
                        @supports_net_changes = 1";
                
                using var command = new SqlCommand(sql, _connection);
                command.Parameters.AddWithValue("@schema", _configuration.SchemaName ?? "dbo");
                command.Parameters.AddWithValue("@table", tableName);
                
                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetCurrentChangePositionAsync()
        {
            if (_connection?.State != ConnectionState.Open)
                throw new InvalidOperationException("Connection is not open");

            try
            {
                var sql = "SELECT ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0)";
                using var command = new SqlCommand(sql, _connection);
                var result = await command.ExecuteScalarAsync();
                return result?.ToString() ?? "0";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get current change tracking version", ex);
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
                // Get all CDC-enabled tables
                var tables = await GetCDCEnabledTablesAsync();
                
                foreach (var table in tables)
                {
                    var tableChanges = await GetTableChangesAsync(table, fromPosition, currentPosition);
                    changes.AddRange(tableChanges);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve changes from CDC", ex);
            }

            return changes;
        }

        public async Task<List<ChangeRecord>> GetTableChangesAsync(string tableName, string fromPosition, string? toPosition = null)
        {
            if (_connection?.State != ConnectionState.Open)
                throw new InvalidOperationException("Connection is not open");

            var changes = new List<ChangeRecord>();
            var currentPosition = toPosition ?? await GetCurrentChangePositionAsync();
            
            try
            {
                var schema = _configuration.SchemaName ?? "dbo";
                var captureInstance = await GetCaptureInstanceAsync(schema, tableName);
                
                if (string.IsNullOrEmpty(captureInstance))
                    return changes;

                var sql = $@"
                    SELECT 
                        ct.__$start_lsn as ChangeId,
                        @tableName as TableName,
                        ct.__$operation as Operation,
                        ct.__$update_mask as UpdateMask,
                        ct.__$seqval as SequenceValue,
                        ct.__$command_id as CommandId,
                        @fromPosition as FromPosition,
                        @currentPosition as CurrentPosition
                    FROM cdc.fn_cdc_get_all_changes_{captureInstance}(@fromPosition, @currentPosition, 'all') ct
                    WHERE ct.__$start_lsn > @fromPosition";
                
                using var command = new SqlCommand(sql, _connection);
                command.Parameters.AddWithValue("@tableName", tableName);
                command.Parameters.AddWithValue("@fromPosition", fromPosition);
                command.Parameters.AddWithValue("@currentPosition", currentPosition);
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var change = new ChangeRecord
                    {
                        ChangeId = Convert.ToBase64String((byte[])reader["ChangeId"]),
                        Operation = ParseOperation(reader.GetInt32("Operation")),
                        ChangeTimestamp = DateTime.UtcNow, // CDC doesn't provide timestamp, use current time
                        ChangePosition = reader.GetString("ChangeId"),
                        Metadata = new Dictionary<string, object>
                        {
                            ["UpdateMask"] = reader.GetString("UpdateMask"),
                            ["SequenceValue"] = reader.GetInt32("SequenceValue"),
                            ["CommandId"] = reader.GetInt32("CommandId")
                        }
                    };
                    var updateMask = reader.IsDBNull("UpdateMask")
                        ? null
                        : (byte[])reader["UpdateMask"];
                    changes.Add(change);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve changes for table {tableName}", ex);
            }

            return changes;
        }

        public async Task<Dictionary<string, List<ChangeRecord>>> GetMultiTableChangesAsync(IEnumerable<string> tableNames, string fromPosition, string? toPosition = null)
        {
            var result = new Dictionary<string, List<ChangeRecord>>();
            
            foreach (var tableName in tableNames)
            {
                result[tableName] = await GetTableChangesAsync(tableName, fromPosition, toPosition);
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
                    Metadata = change.Metadata ?? new Dictionary<string, object>()
                };
                
                // Get detailed change information if available
                if (change.Operation == ChangeOperation.Update)
                {
                    var detailedInfo = await GetDetailedUpdateInfoAsync(change.TableName, change.ChangeId);
                    if (detailedInfo != null)
                    {
                        detailedChange.OldValues = detailedInfo.OldValues;
                        detailedChange.NewValues = detailedInfo.NewValues;
                        detailedChange.AffectedColumns = detailedInfo.AffectedColumns;
                    }
                }
                
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
                    SchemaName = _configuration.SchemaName ?? "dbo",
                    Columns = new List<ColumnDefinition>(),
                    PrimaryKeyColumns = new List<string>(),
                    HasCDCEnabled = await IsCDCEnabledAsync(tableName)
                };

                // Get column information
                var columnSql = @"
                    SELECT 
                        c.name as ColumnName,
                        t.name as DataType,
                        c.is_nullable as IsNullable,
                        c.max_length as MaxLength,
                        c.precision as Precision,
                        c.scale as Scale
                    FROM sys.columns c
                    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                    INNER JOIN sys.tables tab ON c.object_id = tab.object_id
                    INNER JOIN sys.schemas s ON tab.schema_id = s.schema_id
                    WHERE s.name = @schema 
                    AND tab.name = @table
                    ORDER BY c.column_id";
                
                using var columnCommand = new SqlCommand(columnSql, _connection);
                columnCommand.Parameters.AddWithValue("@schema", _configuration.SchemaName ?? "dbo");
                columnCommand.Parameters.AddWithValue("@table", tableName);
                
                using var columnReader = await columnCommand.ExecuteReaderAsync();
                while (await columnReader.ReadAsync())
                {
                    var column = new ColumnDefinition
                    {
                        ColumnName = columnReader.GetString("ColumnName"),
                        DataType = columnReader.GetString("DataType"),
                        IsNullable = columnReader.GetBoolean("IsNullable"),
                        MaxLength = columnReader.IsDBNull("MaxLength") ? null : (int?)columnReader.GetInt32("MaxLength"),
                        Precision = columnReader.IsDBNull("Precision") ? null : (int?)columnReader.GetInt32("Precision"),
                        Scale = columnReader.IsDBNull("Scale") ? null : (int?)columnReader.GetInt32("Scale")
                    };
                    
                    schema.Columns.Add(column);
                }
                
                // Get primary key information
                var pkSql = @"
                    SELECT c.name as ColumnName
                    FROM sys.indexes i
                    INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                    INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                    INNER JOIN sys.tables t ON i.object_id = t.object_id
                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                    WHERE i.is_primary_key = 1
                    AND s.name = @schema 
                    AND t.name = @table
                    ORDER BY ic.key_ordinal";
                
                using var pkCommand = new SqlCommand(pkSql, _connection);
                pkCommand.Parameters.AddWithValue("@schema", _configuration.SchemaName ?? "dbo");
                pkCommand.Parameters.AddWithValue("@table", tableName);
                
                using var pkReader = await pkCommand.ExecuteReaderAsync();
                while (await pkReader.ReadAsync())
                {
                    schema.PrimaryKeyColumns.Add(pkReader.GetString("ColumnName"));
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

                // Check database CDC
                var isDatabaseCDCEnabled = await IsDatabaseCDCEnabledAsync();
                if (!isDatabaseCDCEnabled)
                {
                    result.IsValid = false;
                    result.Errors.Add("SQL Server CDC is not enabled at database level");
                }
                else
                {
                    result.Messages.Add("Database CDC is enabled");
                }

                // Check server version
                var serverVersion = await GetServerVersionAsync();
                result.Messages.Add($"SQL Server Version: {serverVersion}");

                // Check CDC jobs
                var cdcJobs = await GetCDCJobsAsync();
                if (cdcJobs.Any())
                {
                    result.Messages.Add($"CDC Jobs: {string.Join(", ", cdcJobs)}");
                }

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
            if (_connection?.State != ConnectionState.Open)
                return false;

            try
            {
                // SQL Server automatically manages CDC retention via retention value
                // This method can be used to manually clean up if needed
                var sql = @"
                    EXEC sys.sp_cdc_cleanup_change_table 
                        @capture_instance = 'all',
                        @low_water_mark = NULL";
                
                using var command = new SqlCommand(sql, _connection);
                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch
            {
                return false;
            }
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

                // Get CDC status
                var sql = @"
                    SELECT 
                        CHANGE_TRACKING_CURRENT_VERSION() as CurrentVersion,
                        CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID('dbo.YourTable')) as MinValidVersion,
                        GETDATE() as CurrentTime";
                
                using var command = new SqlCommand(sql, _connection);
                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var currentVersion = reader.GetInt64("CurrentVersion");
                    var minValidVersion = reader.GetInt64("MinValidVersion");
                    var currentTime = reader.GetDateTime("CurrentTime");
                    
                    healthInfo.Metrics["CurrentVersion"] = currentVersion;
                    healthInfo.Metrics["MinValidVersion"] = minValidVersion;
                    healthInfo.Metrics["CurrentTime"] = currentTime;
                    healthInfo.Status = CDCHealthStatus.Healthy;
                }
                else
                {
                    healthInfo.Status = CDCHealthStatus.Degraded;
                }

                // Get CDC job status
                var cdcJobs = await GetCDCJobsAsync();
                healthInfo.Metrics["CDCJobs"] = cdcJobs;
            }
            catch (Exception)
            {
                healthInfo.Status = CDCHealthStatus.Unhealthy;
            }

            return healthInfo;
        }

        private async Task<bool> IsDatabaseCDCEnabledAsync()
        {
            try
            {
                var sql = "SELECT is_cdc_enabled FROM sys.databases WHERE name = @database";
                using var command = new SqlCommand(sql, _connection);
                command.Parameters.AddWithValue("@database", _configuration.DatabaseName);
                
                var result = await command.ExecuteScalarAsync();
                return Convert.ToBoolean(result);
            }
            catch
            {
                return false;
            }
        }

        private async Task<List<string>> GetCDCEnabledTablesAsync()
        {
            var tables = new List<string>();
            
            try
            {
                var sql = @"
                    SELECT t.name as TableName
                    FROM sys.tables t
                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                    WHERE t.is_tracked_by_cdc = 1
                    AND s.name = @schema";
                
                using var command = new SqlCommand(sql, _connection);
                command.Parameters.AddWithValue("@schema", _configuration.SchemaName ?? "dbo");
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString("TableName"));
                }
            }
            catch
            {
                // Return empty list if query fails
            }
            
            return tables;
        }

        private async Task<string?> GetCaptureInstanceAsync(string schema, string tableName)
        {
            try
            {
                var sql = @"
                    SELECT capture_instance
                    FROM sys.dm_cdc_capture_instances
                    WHERE source_schema_name = @schema
                    AND source_object_name = @table";
                
                using var command = new SqlCommand(sql, _connection);
                command.Parameters.AddWithValue("@schema", schema);
                command.Parameters.AddWithValue("@table", tableName);
                
                var result = await command.ExecuteScalarAsync();
                return result?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private async Task<DetailedChangeRecord?> GetDetailedUpdateInfoAsync(string tableName, string changeId)
        {
            try
            {
                var schema = _configuration.SchemaName ?? "dbo";
                var captureInstance = await GetCaptureInstanceAsync(schema, tableName);
                
                if (string.IsNullOrEmpty(captureInstance))
                    return null;

                // Validate capture instance name to prevent SQL injection
                if (!System.Text.RegularExpressions.Regex.IsMatch(captureInstance, @"^[a-zA-Z0-9_]+$"))
                    throw new InvalidOperationException($"Invalid capture instance name: {captureInstance}");

                var sql = $@"
                    SELECT 
                        ct.__$update_mask as UpdateMask,
                        ct.__$seqval as SequenceValue
                    FROM cdc.fn_cdc_get_all_changes_{captureInstance}(@changeId, @changeId, 'all') ct
                    WHERE ct.__$start_lsn = @changeId";

                using var command = new SqlCommand(sql, _connection);
                command.Parameters.AddWithValue("@changeId", changeId);
                
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var updateMask = reader.GetString("UpdateMask");
                    var sequenceValue = reader.GetInt32("SequenceValue");
                    
                    return new DetailedChangeRecord
                    {
                        OldValues = new Dictionary<string, object>(),
                        NewValues = new Dictionary<string, object>(),
                        AffectedColumns = ParseUpdateMask(updateMask),
                        Metadata = new Dictionary<string, object>
                        {
                            ["UpdateMask"] = updateMask,
                            ["SequenceValue"] = sequenceValue
                        }
                    };
                }
            }
            catch
            {
                // Return null if detailed info cannot be retrieved
            }
            
            return null;
        }

        private List<string> ParseUpdateMask(string updateMask)
        {
            // This is a simplified implementation
            // In production, you'd parse the update mask to determine affected columns
            return new List<string>();
        }

        private async Task<string> GetServerVersionAsync()
        {
            try
            {
                var sql = "SELECT @@VERSION";
                using var command = new SqlCommand(sql, _connection);
                var version = await command.ExecuteScalarAsync();
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private async Task<List<string>> GetCDCJobsAsync()
        {
            var jobs = new List<string>();
            
            try
            {
                var sql = @"
                    SELECT name
                    FROM msdb.dbo.sysjobs
                    WHERE name LIKE '%cdc%'
                    ORDER BY name";
                
                using var command = new SqlCommand(sql, _connection);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    jobs.Add(reader.GetString("name"));
                }
            }
            catch
            {
                // Return empty list if query fails
            }
            
            return jobs;
        }

        private ChangeOperation ParseOperation(int operationCode)
        {
            return operationCode switch
            {
                1 => ChangeOperation.Delete,
                2 => ChangeOperation.Insert,
                3 => ChangeOperation.Update,
                4 => ChangeOperation.Update,
                _ => ChangeOperation.Unknown
            };
        }

        public async Task<List<ColumnDefinition>> GetTableColumnsAsync(string tableName)
        {
            if (_connection?.State != ConnectionState.Open)
                return new List<ColumnDefinition>();

            try
            {
                var sql = @"
                    SELECT 
                        c.name,
                        t.name as data_type,
                        c.max_length,
                        c.precision,
                        c.scale,
                        c.is_nullable,
                        c.column_id
                    FROM sys.columns c
                    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                    INNER JOIN sys.tables tab ON c.object_id = tab.object_id
                    INNER JOIN sys.schemas s ON tab.schema_id = s.schema_id
                    WHERE s.name = @schema AND tab.name = @table
                    ORDER BY c.column_id";

                using var command = new SqlCommand(sql, _connection);
                command.Parameters.AddWithValue("@schema", _configuration.SchemaName ?? "dbo");
                command.Parameters.AddWithValue("@table", tableName);

                var columns = new List<ColumnDefinition>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    columns.Add(new ColumnDefinition
                    {
                        ColumnName = reader.GetString("name"),
                        DataType = reader.GetString("data_type"),
                        MaxLength = reader.IsDBNull("max_length") ? null : reader.GetInt32("max_length"),
                        Precision = reader.IsDBNull("precision") ? null : reader.GetInt32("precision"),
                        Scale = reader.IsDBNull("scale") ? null : reader.GetInt32("scale"),
                        IsNullable = reader.GetBoolean("is_nullable")
                    });
                }
                return columns;
            }
            catch
            {
                return new List<ColumnDefinition>();
            }
        }

        public async Task<List<IndexDefinition>> GetTableIndexesAsync(string tableName)
        {
            if (_connection?.State != ConnectionState.Open)
                return new List<IndexDefinition>();

            try
            {
                var sql = @"
                    SELECT 
                        i.name,
                        i.type_desc,
                        i.is_unique,
                        i.is_primary_key,
                        i.is_unique_constraint
                    FROM sys.indexes i
                    INNER JOIN sys.tables t ON i.object_id = t.object_id
                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                    WHERE s.name = @schema AND t.name = @table
                    ORDER BY i.name";

                using var command = new SqlCommand(sql, _connection);
                command.Parameters.AddWithValue("@schema", _configuration.SchemaName ?? "dbo");
                command.Parameters.AddWithValue("@table", tableName);

                var indexes = new List<IndexDefinition>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    indexes.Add(new IndexDefinition
                    {
                        Name = reader.GetString("name"),
                        Type = reader.GetString("type_desc"),
                        IsUnique = reader.GetBoolean("is_unique"),
                        IsPrimaryKey = reader.GetBoolean("is_primary_key"),
                        IsUniqueConstraint = reader.GetBoolean("is_unique_constraint")
                    });
                }
                return indexes;
            }
            catch
            {
                return new List<IndexDefinition>();
            }
        }

        public async Task<List<ConstraintDefinition>> GetTableConstraintsAsync(string tableName)
        {
            if (_connection?.State != ConnectionState.Open)
                return new List<ConstraintDefinition>();

            try
            {
                var sql = @"
                    SELECT 
                        c.name,
                        c.type_desc,
                        c.definition
                    FROM sys.check_constraints c
                    INNER JOIN sys.tables t ON c.parent_object_id = t.object_id
                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                    WHERE s.name = @schema AND t.name = @table
                    UNION ALL
                    SELECT 
                        fk.name,
                        'FOREIGN_KEY' as type_desc,
                        'FOREIGN KEY CONSTRAINT' as definition
                    FROM sys.foreign_keys fk
                    INNER JOIN sys.tables t ON fk.parent_object_id = t.object_id
                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                    WHERE s.name = @schema AND t.name = @table
                    ORDER BY name";

                using var command = new SqlCommand(sql, _connection);
                command.Parameters.AddWithValue("@schema", _configuration.SchemaName ?? "dbo");
                command.Parameters.AddWithValue("@table", tableName);

                var constraints = new List<ConstraintDefinition>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    constraints.Add(new ConstraintDefinition
                    {
                        Name = reader.GetString("name"),
                        Type = ConstraintType.Other, // Default type since we can't easily map string to enum
                        Expression = reader.IsDBNull("definition") ? null : reader.GetString("definition")
                    });
                }
                return constraints;
            }
            catch
            {
                return new List<ConstraintDefinition>();
            }
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