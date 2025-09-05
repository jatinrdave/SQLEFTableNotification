using System;
using System.Collections.Generic;

namespace SQLDBEntityNotifier.Models
{
    /// <summary>
    /// Enhanced event arguments for database change notifications with operation details
    /// </summary>
    public class EnhancedRecordChangedEventArgs<T> : RecordChangedEventArgs<T> where T : class, new()
    {
        /// <summary>
        /// Gets or sets the type of change operation that occurred
        /// </summary>
        public ChangeOperation Operation { get; set; } = ChangeOperation.Unknown;
        
        /// <summary>
        /// Gets or sets the database type where the change occurred
        /// </summary>
        public DatabaseType DatabaseType { get; set; }
        
        /// <summary>
        /// Gets or sets the database-specific change identifier (e.g., LSN for SQL Server, WAL position for PostgreSQL)
        /// </summary>
        public string? ChangeIdentifier { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when the change occurred in the database
        /// </summary>
        public DateTime? DatabaseChangeTimestamp { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the user who made the change (if available)
        /// </summary>
        public string? ChangedBy { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the application that made the change (if available)
        /// </summary>
        public string? ApplicationName { get; set; }
        
        /// <summary>
        /// Gets or sets the host name where the change originated (if available)
        /// </summary>
        public string? HostName { get; set; }
        
        /// <summary>
        /// Gets or sets additional metadata about the change (database-specific)
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
        
        /// <summary>
        /// Gets or sets the old values for update operations (if available)
        /// </summary>
        public T? OldValues { get; set; }
        
        /// <summary>
        /// Gets or sets the new values for insert/update operations (if available)
        /// </summary>
        public T? NewValues { get; set; }
        
        /// <summary>
        /// Gets or sets the affected columns for update operations (if available)
        /// </summary>
        public List<string>? AffectedColumns { get; set; }
        
        /// <summary>
        /// Gets or sets the transaction ID if available
        /// </summary>
        public string? TransactionId { get; set; }
        
        /// <summary>
        /// Gets or sets whether this change is part of a batch operation
        /// </summary>
        public bool IsBatchOperation { get; set; }
        
        /// <summary>
        /// Gets or sets the batch sequence number if applicable
        /// </summary>
        public int? BatchSequence { get; set; }
    }
}