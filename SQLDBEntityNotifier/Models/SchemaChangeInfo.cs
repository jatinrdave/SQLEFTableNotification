using System;
using System.Collections.Generic;

namespace SQLDBEntityNotifier.Models
{
    /// <summary>
    /// Represents detailed information about a database schema change
    /// </summary>
    public class SchemaChangeInfo
    {
        /// <summary>
        /// Gets or sets the unique identifier for this schema change
        /// </summary>
        public string ChangeId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the table that was affected
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the schema name
        /// </summary>
        public string SchemaName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of schema change
        /// </summary>
        public SchemaChangeType ChangeType { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the schema change occurred
        /// </summary>
        public DateTime ChangeTimestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the user who made the schema change
        /// </summary>
        public string? ChangedBy { get; set; }

        /// <summary>
        /// Gets or sets the application that made the schema change
        /// </summary>
        public string? ApplicationName { get; set; }

        /// <summary>
        /// Gets or sets the host where the schema change originated
        /// </summary>
        public string? HostName { get; set; }

        /// <summary>
        /// Gets or sets the transaction ID
        /// </summary>
        public string? TransactionId { get; set; }

        /// <summary>
        /// Gets or sets the description of the schema change
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the SQL script that was executed
        /// </summary>
        public string? SqlScript { get; set; }

        /// <summary>
        /// Gets or sets the version of the schema after the change
        /// </summary>
        public string? SchemaVersion { get; set; }

        /// <summary>
        /// Gets or sets the previous version of the schema
        /// </summary>
        public string? PreviousSchemaVersion { get; set; }

        /// <summary>
        /// Gets or sets whether the change was successful
        /// </summary>
        public bool IsSuccessful { get; set; } = true;

        /// <summary>
        /// Gets or sets the error message if the change failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the rollback script if available
        /// </summary>
        public string? RollbackScript { get; set; }

        /// <summary>
        /// Gets or sets the estimated downtime for the change
        /// </summary>
        public TimeSpan? EstimatedDowntime { get; set; }

        /// <summary>
        /// Gets or sets the actual downtime for the change
        /// </summary>
        public TimeSpan? ActualDowntime { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the schema change occurred (alias for ChangeTimestamp)
        /// </summary>
        public DateTime Timestamp => ChangeTimestamp;

        /// <summary>
        /// Gets or sets the affected columns
        /// </summary>
        public List<string> AffectedColumns { get; set; } = new();

        /// <summary>
        /// Gets or sets the impact level of the schema change
        /// </summary>
        public SchemaChangeImpact Impact { get; set; } = SchemaChangeImpact.Low;

        /// <summary>
        /// Gets or sets the risk level of the schema change
        /// </summary>
        public SchemaChangeRisk Risk { get; set; } = SchemaChangeRisk.Low;

        /// <summary>
        /// Gets or sets the approval status of the schema change
        /// </summary>
        public SchemaChangeApproval ApprovalStatus { get; set; } = SchemaChangeApproval.Approved;

        /// <summary>
        /// Gets or sets the approved by user
        /// </summary>
        public string? ApprovedBy { get; set; }

        /// <summary>
        /// Gets or sets the approval timestamp
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// Gets or sets the change request number
        /// </summary>
        public string? ChangeRequestNumber { get; set; }

        /// <summary>
        /// Gets or sets the business justification for the change
        /// </summary>
        public string? BusinessJustification { get; set; }

        /// <summary>
        /// Gets or sets the testing status
        /// </summary>
        public SchemaChangeTesting TestingStatus { get; set; } = SchemaChangeTesting.NotTested;

        /// <summary>
        /// Gets or sets the testing notes
        /// </summary>
        public string? TestingNotes { get; set; }

        /// <summary>
        /// Gets or sets the deployment environment
        /// </summary>
        public string? DeploymentEnvironment { get; set; }

        /// <summary>
        /// Gets or sets the scheduled deployment time
        /// </summary>
        public DateTime? ScheduledDeploymentTime { get; set; }

        /// <summary>
        /// Gets or sets the actual deployment time
        /// </summary>
        public DateTime? ActualDeploymentTime { get; set; }

        /// <summary>
        /// Gets or sets the tags associated with this schema change
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Gets or sets custom metadata for the schema change
        /// </summary>
        public Dictionary<string, object>? CustomMetadata { get; set; }

        /// <summary>
        /// Gets or sets the column changes if this is a column-related schema change
        /// </summary>
        public List<ColumnSchemaChange>? ColumnChanges { get; set; }

        /// <summary>
        /// Gets or sets the index changes if this is an index-related schema change
        /// </summary>
        public List<IndexChange>? IndexChanges { get; set; }

        /// <summary>
        /// Gets or sets the constraint changes if this is a constraint-related schema change
        /// </summary>
        public List<ConstraintChange>? ConstraintChanges { get; set; }

        /// <summary>
        /// Gets or sets the table changes if this is a table-related schema change
        /// </summary>
        public List<TableSchemaChange>? TableChanges { get; set; }

        /// <summary>
        /// Gets or sets the correlation ID for linking related schema changes
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the parent change ID if this is a child schema change
        /// </summary>
        public string? ParentChangeId { get; set; }

        /// <summary>
        /// Gets or sets the sequence number within a batch of schema changes
        /// </summary>
        public int? SequenceNumber { get; set; }

        /// <summary>
        /// Gets or sets whether this schema change is part of a rollback operation
        /// </summary>
        public bool IsRollback { get; set; }

        /// <summary>
        /// Gets or sets the reason for the schema change
        /// </summary>
        public string? ChangeReason { get; set; }

        /// <summary>
        /// Gets or sets the business process or workflow that triggered the schema change
        /// </summary>
        public string? BusinessProcess { get; set; }

        /// <summary>
        /// Creates a shallow copy of this schema change info
        /// </summary>
        public SchemaChangeInfo Clone()
        {
            return new SchemaChangeInfo
            {
                ChangeId = this.ChangeId,
                TableName = this.TableName,
                SchemaName = this.SchemaName,
                ChangeType = this.ChangeType,
                ChangeTimestamp = this.ChangeTimestamp,
                ChangedBy = this.ChangedBy,
                ApplicationName = this.ApplicationName,
                HostName = this.HostName,
                TransactionId = this.TransactionId,
                Description = this.Description,
                SqlScript = this.SqlScript,
                SchemaVersion = this.SchemaVersion,
                PreviousSchemaVersion = this.PreviousSchemaVersion,
                IsSuccessful = this.IsSuccessful,
                ErrorMessage = this.ErrorMessage,
                RollbackScript = this.RollbackScript,
                EstimatedDowntime = this.EstimatedDowntime,
                ActualDowntime = this.ActualDowntime,
                Impact = this.Impact,
                Risk = this.Risk,
                ApprovalStatus = this.ApprovalStatus,
                ApprovedBy = this.ApprovedBy,
                ApprovedAt = this.ApprovedAt,
                ChangeRequestNumber = this.ChangeRequestNumber,
                BusinessJustification = this.BusinessJustification,
                TestingStatus = this.TestingStatus,
                TestingNotes = this.TestingNotes,
                DeploymentEnvironment = this.DeploymentEnvironment,
                ScheduledDeploymentTime = this.ScheduledDeploymentTime,
                ActualDeploymentTime = this.ActualDeploymentTime,
                Tags = new List<string>(this.Tags),
                CustomMetadata = this.CustomMetadata != null ? new Dictionary<string, object>(this.CustomMetadata) : null,
                ColumnChanges = this.ColumnChanges?.ConvertAll(c => c.Clone()),
                IndexChanges = this.IndexChanges?.ConvertAll(i => i.Clone()),
                ConstraintChanges = this.ConstraintChanges?.ConvertAll(c => c.Clone()),
                TableChanges = this.TableChanges?.ConvertAll(t => t.Clone()),
                CorrelationId = this.CorrelationId,
                ParentChangeId = this.ParentChangeId,
                SequenceNumber = this.SequenceNumber,
                IsRollback = this.IsRollback,
                ChangeReason = this.ChangeReason,
                BusinessProcess = this.BusinessProcess
            };
        }

        /// <summary>
        /// Gets a string representation of the schema change
        /// </summary>
        public override string ToString()
        {
            return $"{ChangeType} on {SchemaName}.{TableName} at {ChangeTimestamp:yyyy-MM-dd HH:mm:ss}";
        }
    }

    /// <summary>
    /// Represents the type of schema change
    /// </summary>
    public enum SchemaChangeType
    {
        /// <summary>
        /// Table was created
        /// </summary>
        TableCreated = 1,

        /// <summary>
        /// Table was dropped
        /// </summary>
        TableDropped = 2,

        /// <summary>
        /// Table was renamed
        /// </summary>
        TableRenamed = 3,

        /// <summary>
        /// Table was truncated
        /// </summary>
        TableTruncated = 4,

        /// <summary>
        /// Column was added
        /// </summary>
        ColumnAdded = 5,

        /// <summary>
        /// Column was dropped
        /// </summary>
        ColumnDropped = 6,

        /// <summary>
        /// Column was renamed
        /// </summary>
        ColumnRenamed = 7,

        /// <summary>
        /// Column data type was changed
        /// </summary>
        ColumnDataTypeChanged = 8,

        /// <summary>
        /// Column constraints were modified
        /// </summary>
        ColumnConstraintsModified = 9,

        /// <summary>
        /// Index was created
        /// </summary>
        IndexCreated = 10,

        /// <summary>
        /// Index was dropped
        /// </summary>
        IndexDropped = 11,

        /// <summary>
        /// Index was modified
        /// </summary>
        IndexModified = 12,

        /// <summary>
        /// Constraint was added
        /// </summary>
        ConstraintAdded = 13,

        /// <summary>
        /// Constraint was dropped
        /// </summary>
        ConstraintDropped = 14,

        /// <summary>
        /// Constraint was modified
        /// </summary>
        ConstraintModified = 15,

        /// <summary>
        /// View was created
        /// </summary>
        ViewCreated = 16,

        /// <summary>
        /// View was dropped
        /// </summary>
        ViewDropped = 17,

        /// <summary>
        /// View was modified
        /// </summary>
        ViewModified = 18,

        /// <summary>
        /// Stored procedure was created
        /// </summary>
        StoredProcedureCreated = 19,

        /// <summary>
        /// Stored procedure was dropped
        /// </summary>
        StoredProcedureDropped = 20,

        /// <summary>
        /// Stored procedure was modified
        /// </summary>
        StoredProcedureModified = 21,

        /// <summary>
        /// Function was created
        /// </summary>
        FunctionCreated = 22,

        /// <summary>
        /// Function was dropped
        /// </summary>
        FunctionDropped = 23,

        /// <summary>
        /// Function was modified
        /// </summary>
        FunctionModified = 24,

        /// <summary>
        /// Trigger was created
        /// </summary>
        TriggerCreated = 25,

        /// <summary>
        /// Trigger was dropped
        /// </summary>
        TriggerDropped = 26,

        /// <summary>
        /// Trigger was modified
        /// </summary>
        TriggerModified = 27,

        /// <summary>
        /// Schema was created
        /// </summary>
        SchemaCreated = 28,

        /// <summary>
        /// Schema was dropped
        /// </summary>
        SchemaDropped = 29,

        /// <summary>
        /// Schema was renamed
        /// </summary>
        SchemaRenamed = 30,

        /// <summary>
        /// Database was created
        /// </summary>
        DatabaseCreated = 31,

        /// <summary>
        /// Database was dropped
        /// </summary>
        DatabaseDropped = 32,

        /// <summary>
        /// Database was renamed
        /// </summary>
        DatabaseRenamed = 33,



        /// <summary>
        /// Constraint was removed
        /// </summary>
        ConstraintRemoved = 39,



        /// <summary>
        /// Other or unspecified schema change
        /// </summary>
        Other = 99
    }

    /// <summary>
    /// Represents the impact level of a schema change
    /// </summary>
    public enum SchemaChangeImpact
    {
        /// <summary>
        /// No impact
        /// </summary>
        None = 0,

        /// <summary>
        /// Low impact
        /// </summary>
        Low = 1,

        /// <summary>
        /// Medium impact
        /// </summary>
        Medium = 2,

        /// <summary>
        /// High impact
        /// </summary>
        High = 3,

        /// <summary>
        /// Critical impact
        /// </summary>
        Critical = 4
    }

    /// <summary>
    /// Represents the risk level of a schema change
    /// </summary>
    public enum SchemaChangeRisk
    {
        /// <summary>
        /// No risk
        /// </summary>
        None = 0,

        /// <summary>
        /// Low risk
        /// </summary>
        Low = 1,

        /// <summary>
        /// Medium risk
        /// </summary>
        Medium = 2,

        /// <summary>
        /// High risk
        /// </summary>
        High = 3,

        /// <summary>
        /// Critical risk
        /// </summary>
        Critical = 4
    }

    /// <summary>
    /// Represents the approval status of a schema change
    /// </summary>
    public enum SchemaChangeApproval
    {
        /// <summary>
        /// Pending approval
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Approved
        /// </summary>
        Approved = 1,

        /// <summary>
        /// Rejected
        /// </summary>
        Rejected = 2,

        /// <summary>
        /// Under review
        /// </summary>
        UnderReview = 3,

        /// <summary>
        /// Conditional approval
        /// </summary>
        Conditional = 4
    }

    /// <summary>
    /// Represents the testing status of a schema change
    /// </summary>
    public enum SchemaChangeTesting
    {
        /// <summary>
        /// Not tested
        /// </summary>
        NotTested = 0,

        /// <summary>
        /// Testing in progress
        /// </summary>
        InProgress = 1,

        /// <summary>
        /// Testing completed successfully
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Testing failed
        /// </summary>
        Failed = 3,

        /// <summary>
        /// Testing waived
        /// </summary>
        Waived = 4
    }
}

