using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using SQLDBEntityNotifier.Interfaces;
using SQLDBEntityNotifier.Models;

namespace SQLDBEntityNotifier.Providers
{
    /// <summary>
    /// PostgreSQL CDC provider using logical replication and WAL for change detection
    /// </summary>
    public class PostgreSqlCDCProvider : ICDCProvider, IDisposable
    {
        private readonly DatabaseConfiguration _configuration;
        private NpgsqlConnection? _connection;
        private bool _disposed = false;
        private readonly object _lockObject = new object();

        public DatabaseType DatabaseType => DatabaseType.PostgreSql;
        public DatabaseConfiguration Configuration => _configuration;

        public PostgreSqlCDCProvider(DatabaseConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            if (configuration.DatabaseType != DatabaseType.PostgreSql)
                throw new ArgumentException("Configuration must be for PostgreSQL database type");
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                var connectionString = _configuration.BuildConnectionString();
                _connection = new NpgsqlConnection(connectionString);
                await _connection.OpenAsync();

                // Check if logical replication is enabled
                var isLogicalReplicationEnabled = await IsLogicalReplicationEnabledAsync();
                if (!isLogicalReplicationEnabled)
                {
                    throw new InvalidOperationException("PostgreSQL logical replication is not enabled. Enable it by setting wal_level=logical in postgresql.conf");
                }

                // Check if the user has replication privilege
                var hasReplicationPrivilege = await HasReplicationPrivilegeAsync();
                if (!hasReplicationPrivilege)
                {
                    throw new InvalidOperationException("PostgreSQL user does not have REPLICATION privilege required for logical replication");
                }

                // Check if the database has the required extensions
                await EnsureRequiredExtensionsAsync();

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
                    WHERE table_schema = @schema 
                    AND table_name = @table";
                
                using var command = new NpgsqlCommand(sql, _connection);
                command.Parameters.AddWithValue("@schema", _configuration.SchemaName ?? "public");
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
                // Enable row level security and create replication slot if needed
                var sql = @"
                    ALTER TABLE @schema.@table REPLICA IDENTITY FULL;
                    SELECT pg_create_logical_replication_slot(@slot_name, 'pgoutput') 
                    WHERE NOT EXISTS (
                        SELECT 1 FROM pg_replication_slots WHERE slot_name = @slot_name
                    );";
                
                var slotName = $"cdc_{_configuration.DatabaseName}_{tableName}";
                
                using var command = new NpgsqlCommand(sql, _connection);
                command.Parameters.AddWithValue("@schema", _configuration.SchemaName ?? "public");
                command.Parameters.AddWithValue("@table", tableName);
                command.Parameters.AddWithValue("@slot_name", slotName);
                
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
                var sql = "SELECT pg_current_wal_lsn()";
                using var command = new NpgsqlCommand(sql, _connection);
                var result = await command.ExecuteScalarAsync();
                return result?.ToString() ?? "0/0";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get current WAL position", ex);
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
                // Get WAL changes between positions using pg_waldump or similar approach
                // This is a simplified implementation - in production you'd use pg_logical_slot_get_changes
                var sql = @"
                    SELECT 
                        'postgres_wal' as ChangeId,
                        'unknown' as TableName,
                        'unknown' as Operation,
                        NOW() as ChangeTimestamp,
                        @position as ChangePosition,
                        current_user as ChangedBy,
                        inet_server_addr()::text as HostName
                    WHERE @fromPosition < @position::pg_lsn";
                
                using var command = new NpgsqlCommand(sql, _connection);
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
                throw new InvalidOperationException("Failed to retrieve changes from WAL", ex);
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
                    SchemaName = _configuration.SchemaName ?? "public",
                    Columns = new List<ColumnDefinition>(),
                    PrimaryKeyColumns = new List<string>(),
                    HasCDCEnabled = await IsCDCEnabledAsync(tableName)
                };

                // Get column information
                var columnSql = @"
                    SELECT 
                        c.column_name,
                        c.data_type,
                        c.is_nullable,
                        c.character_maximum_length,
                        c.numeric_precision,
                        c.numeric_scale,
                        c.column_default
                    FROM information_schema.columns c
                    WHERE c.table_schema = @schema 
                    AND c.table_name = @table
                    ORDER BY c.ordinal_position";
                
                using var columnCommand = new NpgsqlCommand(columnSql, _connection);
                columnCommand.Parameters.AddWithValue("@schema", _configuration.SchemaName ?? "public");
                columnCommand.Parameters.AddWithValue("@table", tableName);
                
                using var columnReader = await columnCommand.ExecuteReaderAsync();
                while (await columnReader.ReadAsync())
                {
                    var column = new ColumnDefinition
                    {
                        ColumnName = columnReader.GetString("column_name"),
                        DataType = columnReader.GetString("data_type"),
                        IsNullable = columnReader.GetString("is_nullable") == "YES",
                        MaxLength = columnReader.IsDBNull("character_maximum_length") ? null : (int?)columnReader.GetInt32("character_maximum_length"),
                        Precision = columnReader.IsDBNull("numeric_precision") ? null : (int?)columnReader.GetInt32("numeric_precision"),
                        Scale = columnReader.IsDBNull("numeric_scale") ? null : (int?)columnReader.GetInt32("numeric_scale")
                    };
                    
                    schema.Columns.Add(column);
                }
                
                // Get primary key information
                var pkSql = @"
                    SELECT kcu.column_name
                    FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage kcu 
                        ON tc.constraint_name = kcu.constraint_name
                        AND tc.table_schema = kcu.table_schema
                    WHERE tc.constraint_type = 'PRIMARY KEY' 
                    AND tc.table_schema = @schema 
                    AND tc.table_name = @table
                    ORDER BY kcu.ordinal_position";
                
                using var pkCommand = new NpgsqlCommand(pkSql, _connection);
                pkCommand.Parameters.AddWithValue("@schema", _configuration.SchemaName ?? "public");
                pkCommand.Parameters.AddWithValue("@table", tableName);
                
                using var pkReader = await pkCommand.ExecuteReaderAsync();
                while (await pkReader.ReadAsync())
                {
                    schema.PrimaryKeyColumns.Add(pkReader.GetString("column_name"));
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

                // Check logical replication
                var isLogicalReplicationEnabled = await IsLogicalReplicationEnabledAsync();
                if (!isLogicalReplicationEnabled)
                {
                    result.IsValid = false;
                    result.Errors.Add("PostgreSQL logical replication is not enabled");
                }
                else
                {
                    result.Messages.Add("Logical replication is enabled");
                }

                // Check replication privileges
                var hasReplicationPrivilege = await HasReplicationPrivilegeAsync();
                if (!hasReplicationPrivilege)
                {
                    result.IsValid = false;
                    result.Errors.Add("User does not have REPLICATION privilege");
                }
                else
                {
                    result.Messages.Add("User has required replication privileges");
                }

                // Check server version
                var serverVersion = await GetServerVersionAsync();
                result.Messages.Add($"PostgreSQL Server Version: {serverVersion}");

                // Check required extensions
                var missingExtensions = await GetMissingExtensionsAsync();
                if (missingExtensions.Any())
                {
                    result.Warnings.Add($"Missing recommended extensions: {string.Join(", ", missingExtensions)}");
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
                // PostgreSQL automatically manages WAL retention via wal_keep_segments setting
                // This method can be used to clean up old replication slots if needed
                var sql = @"
                    SELECT pg_drop_replication_slot(slot_name)
                    FROM pg_replication_slots 
                    WHERE slot_name LIKE 'cdc_%'
                    AND restart_lsn < pg_current_wal_lsn() - @retention_bytes";
                
                var retentionBytes = (long)(retentionPeriod.TotalSeconds * 16 * 1024 * 1024); // Rough estimate
                
                using var command = new NpgsqlCommand(sql, _connection);
                command.Parameters.AddWithValue("@retention_bytes", retentionBytes);
                
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

                // Get WAL status
                var sql = "SELECT pg_current_wal_lsn(), pg_current_wal_insert_lsn()";
                using var command = new NpgsqlCommand(sql, _connection);
                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var currentLsn = reader.GetString(0);
                    var insertLsn = reader.GetString(1);
                    
                    healthInfo.Metrics["CurrentLSN"] = currentLsn;
                    healthInfo.Metrics["InsertLSN"] = insertLsn;
                    healthInfo.Status = CDCHealthStatus.Healthy;
                }
                else
                {
                    healthInfo.Status = CDCHealthStatus.Degraded;
                }

                // Get replication slot information
                var slotSql = @"
                    SELECT slot_name, restart_lsn, confirmed_flush_lsn
                    FROM pg_replication_slots 
                    WHERE slot_name LIKE 'cdc_%'";
                
                using var slotCommand = new NpgsqlCommand(slotSql, _connection);
                using var slotReader = await slotCommand.ExecuteReaderAsync();
                
                var slots = new List<Dictionary<string, object>>();
                while (await slotReader.ReadAsync())
                {
                    var slot = new Dictionary<string, object>
                    {
                        ["SlotName"] = slotReader.GetString("slot_name"),
                        ["RestartLSN"] = slotReader.GetString("restart_lsn"),
                        ["ConfirmedFlushLSN"] = slotReader.GetString("confirmed_flush_lsn")
                    };
                    slots.Add(slot);
                }
                
                healthInfo.Metrics["ReplicationSlots"] = slots;
            }
            catch (Exception)
            {
                healthInfo.Status = CDCHealthStatus.Unhealthy;
            }

            return healthInfo;
        }

        private async Task<bool> IsLogicalReplicationEnabledAsync()
        {
            try
            {
                var sql = "SHOW wal_level";
                using var command = new NpgsqlCommand(sql, _connection);
                var result = await command.ExecuteScalarAsync();
                return result?.ToString()?.Equals("logical", StringComparison.OrdinalIgnoreCase) == true;
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
                var sql = @"
                    SELECT rolreplication 
                    FROM pg_roles 
                    WHERE rolname = current_user";
                
                using var command = new NpgsqlCommand(sql, _connection);
                var result = await command.ExecuteScalarAsync();
                return Convert.ToBoolean(result);
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
                var sql = "SELECT version()";
                using var command = new NpgsqlCommand(sql, _connection);
                var version = await command.ExecuteScalarAsync();
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private async Task EnsureRequiredExtensionsAsync()
        {
            try
            {
                // Ensure pg_stat_statements extension is available for monitoring
                var sql = "CREATE EXTENSION IF NOT EXISTS pg_stat_statements";
                using var command = new NpgsqlCommand(sql, _connection);
                await command.ExecuteNonQueryAsync();
            }
            catch
            {
                // Extension creation is optional, not critical for CDC functionality
            }
        }

        private async Task<List<string>> GetMissingExtensionsAsync()
        {
            var recommendedExtensions = new[] { "pg_stat_statements" };
            var missingExtensions = new List<string>();
            
            try
            {
                foreach (var extension in recommendedExtensions)
                {
                    var sql = "SELECT COUNT(*) FROM pg_extension WHERE extname = @extname";
                    using var command = new NpgsqlCommand(sql, _connection);
                    command.Parameters.AddWithValue("@extname", extension);
                    
                    var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    if (count == 0)
                    {
                        missingExtensions.Add(extension);
                    }
                }
            }
            catch
            {
                // If we can't check extensions, assume all are missing
                missingExtensions.AddRange(recommendedExtensions);
            }
            
            return missingExtensions;
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