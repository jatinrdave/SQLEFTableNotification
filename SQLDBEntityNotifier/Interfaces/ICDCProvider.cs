using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLDBEntityNotifier.Models;

namespace SQLDBEntityNotifier.Interfaces
{
    /// <summary>
    /// Database-agnostic interface for Change Data Capture operations
    /// </summary>
    public interface ICDCProvider
    {
        /// <summary>
        /// Gets the database type this provider supports
        /// </summary>
        DatabaseType DatabaseType { get; }
        
        /// <summary>
        /// Gets the database configuration
        /// </summary>
        DatabaseConfiguration Configuration { get; }
        
        /// <summary>
        /// Initializes the CDC provider and sets up necessary infrastructure
        /// </summary>
        Task<bool> InitializeAsync();
        
        /// <summary>
        /// Checks if CDC is enabled for the specified table
        /// </summary>
        Task<bool> IsCDCEnabledAsync(string tableName);
        
        /// <summary>
        /// Enables CDC for the specified table if not already enabled
        /// </summary>
        Task<bool> EnableCDCAsync(string tableName);
        
        /// <summary>
        /// Gets the current change tracking version/position
        /// </summary>
        Task<string> GetCurrentChangePositionAsync();
        
        /// <summary>
        /// Gets changes since the specified position
        /// </summary>
        Task<List<ChangeRecord>> GetChangesAsync(string fromPosition, string? toPosition = null);
        
        /// <summary>
        /// Gets changes for a specific table since the specified position
        /// </summary>
        Task<List<ChangeRecord>> GetTableChangesAsync(string tableName, string fromPosition, string? toPosition = null);
        
        /// <summary>
        /// Gets changes for multiple tables since the specified position
        /// </summary>
        Task<Dictionary<string, List<ChangeRecord>>> GetMultiTableChangesAsync(IEnumerable<string> tableNames, string fromPosition, string? toPosition = null);
        
        /// <summary>
        /// Gets detailed change information including old and new values
        /// </summary>
        Task<List<DetailedChangeRecord>> GetDetailedChangesAsync(string fromPosition, string? toPosition = null);
        
        /// <summary>
        /// Gets the schema information for the specified table
        /// </summary>
        Task<TableSchema> GetTableSchemaAsync(string tableName);
        
        /// <summary>
        /// Validates the CDC configuration and connectivity
        /// </summary>
        Task<CDCValidationResult> ValidateConfigurationAsync();
        
        /// <summary>
        /// Cleans up old change data based on retention policy
        /// </summary>
        Task<bool> CleanupOldChangesAsync(TimeSpan retentionPeriod);
        
        /// <summary>
        /// Gets CDC statistics and health information
        /// </summary>
        Task<CDCHealthInfo> GetHealthInfoAsync();
        
        /// <summary>
        /// Disposes the provider and cleans up resources
        /// </summary>
        void Dispose();
    }
    
    /// <summary>
    /// Represents a database change record
    /// </summary>
    public class ChangeRecord
    {
        /// <summary>
        /// Gets or sets the unique identifier for this change
        /// </summary>
        public string ChangeId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the table name where the change occurred
        /// </summary>
        public string TableName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the type of change operation
        /// </summary>
        public ChangeOperation Operation { get; set; }
        
        /// <summary>
        /// Gets or sets the primary key values as a dictionary
        /// </summary>
        public Dictionary<string, object> PrimaryKeyValues { get; set; } = new();
        
        /// <summary>
        /// Gets or sets the change timestamp
        /// </summary>
        public DateTime ChangeTimestamp { get; set; }
        
        /// <summary>
        /// Gets or sets the database-specific change position
        /// </summary>
        public string ChangePosition { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the user who made the change
        /// </summary>
        public string? ChangedBy { get; set; }
        
        /// <summary>
        /// Gets or sets the application that made the change
        /// </summary>
        public string? ApplicationName { get; set; }
        
        /// <summary>
        /// Gets or sets the host where the change originated
        /// </summary>
        public string? HostName { get; set; }
        
        /// <summary>
        /// Gets or sets the transaction ID
        /// </summary>
        public string? TransactionId { get; set; }
    }
    
    /// <summary>
    /// Represents a detailed change record with old and new values
    /// </summary>
    public class DetailedChangeRecord : ChangeRecord
    {
        /// <summary>
        /// Gets or sets the old values before the change
        /// </summary>
        public Dictionary<string, object>? OldValues { get; set; }
        
        /// <summary>
        /// Gets or sets the new values after the change
        /// </summary>
        public Dictionary<string, object>? NewValues { get; set; }
        
        /// <summary>
        /// Gets or sets the affected columns
        /// </summary>
        public List<string> AffectedColumns { get; set; } = new();
        
        /// <summary>
        /// Gets or sets additional metadata
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }
    
    /// <summary>
    /// Represents table schema information
    /// </summary>
    public class TableSchema
    {
        /// <summary>
        /// Gets or sets the table name
        /// </summary>
        public string TableName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the schema name
        /// </summary>
        public string SchemaName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the column definitions
        /// </summary>
        public List<ColumnDefinition> Columns { get; set; } = new();
        
        /// <summary>
        /// Gets or sets the primary key columns
        /// </summary>
        public List<string> PrimaryKeyColumns { get; set; } = new();
        
        /// <summary>
        /// Gets or sets whether the table has CDC enabled
        /// </summary>
        public bool HasCDCEnabled { get; set; }
    }
    
    /// <summary>
    /// Represents a column definition
    /// </summary>
    public class ColumnDefinition
    {
        /// <summary>
        /// Gets or sets the column name
        /// </summary>
        public string ColumnName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the data type
        /// </summary>
        public string DataType { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets whether the column is nullable
        /// </summary>
        public bool IsNullable { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum length for string types
        /// </summary>
        public int? MaxLength { get; set; }
        
        /// <summary>
        /// Gets or sets the precision for numeric types
        /// </summary>
        public int? Precision { get; set; }
        
        /// <summary>
        /// Gets or sets the scale for numeric types
        /// </summary>
        public int? Scale { get; set; }
    }
    
    /// <summary>
    /// Represents CDC validation result
    /// </summary>
    public class CDCValidationResult
    {
        /// <summary>
        /// Gets or sets whether the validation was successful
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// Gets or sets the validation messages
        /// </summary>
        public List<string> Messages { get; set; } = new();
        
        /// <summary>
        /// Gets or sets the validation errors
        /// </summary>
        public List<string> Errors { get; set; } = new();
        
        /// <summary>
        /// Gets or sets the warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new();
    }
    
    /// <summary>
    /// Represents CDC health information
    /// </summary>
    public class CDCHealthInfo
    {
        /// <summary>
        /// Gets or sets the overall health status
        /// </summary>
        public CDCHealthStatus Status { get; set; }
        
        /// <summary>
        /// Gets or sets the last successful change detection time
        /// </summary>
        public DateTime? LastSuccessfulDetection { get; set; }
        
        /// <summary>
        /// Gets or sets the number of changes detected in the last hour
        /// </summary>
        public long ChangesLastHour { get; set; }
        
        /// <summary>
        /// Gets or sets the number of errors in the last hour
        /// </summary>
        public long ErrorsLastHour { get; set; }
        
        /// <summary>
        /// Gets or sets the average response time for change detection
        /// </summary>
        public TimeSpan AverageResponseTime { get; set; }
        
        /// <summary>
        /// Gets or sets the CDC lag (delay between change and detection)
        /// </summary>
        public TimeSpan CDCLag { get; set; }
        
        /// <summary>
        /// Gets or sets additional health metrics
        /// </summary>
        public Dictionary<string, object> Metrics { get; set; } = new();
    }
    
    /// <summary>
    /// Represents CDC health status
    /// </summary>
    public enum CDCHealthStatus
    {
        /// <summary>
        /// CDC is healthy and working normally
        /// </summary>
        Healthy = 1,
        
        /// <summary>
        /// CDC has minor issues but is still functional
        /// </summary>
        Degraded = 2,
        
        /// <summary>
        /// CDC has significant issues and may not be working properly
        /// </summary>
        Unhealthy = 3,
        
        /// <summary>
        /// CDC status is unknown
        /// </summary>
        Unknown = 4
    }
}