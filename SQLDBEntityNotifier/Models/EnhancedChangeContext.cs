using System;
using System.Collections.Generic;

namespace SQLDBEntityNotifier.Models
{
    /// <summary>
    /// Enhanced change context with rich metadata and tracking information
    /// </summary>
    public class EnhancedChangeContext
    {
        /// <summary>
        /// Gets or sets the unique identifier for this change context
        /// </summary>
        public string ChangeId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the source of the change
        /// </summary>
        public ChangeContext Source { get; set; } = ChangeContext.Unknown;

        /// <summary>
        /// Gets or sets the name of the application that made the change
        /// </summary>
        public string? ApplicationName { get; set; }

        /// <summary>
        /// Gets or sets the version of the application
        /// </summary>
        public string? ApplicationVersion { get; set; }

        /// <summary>
        /// Gets or sets the host name where the change originated
        /// </summary>
        public string? HostName { get; set; }

        /// <summary>
        /// Gets or sets the IP address of the host
        /// </summary>
        public string? HostIPAddress { get; set; }

        /// <summary>
        /// Gets or sets the user name who made the change
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// Gets or sets the user's domain
        /// </summary>
        public string? UserDomain { get; set; }

        /// <summary>
        /// Gets or sets the session ID
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// Gets or sets the transaction ID
        /// </summary>
        public string? TransactionId { get; set; }

        /// <summary>
        /// Gets or sets the batch ID if this change is part of a batch
        /// </summary>
        public string? BatchId { get; set; }

        /// <summary>
        /// Gets or sets the process ID
        /// </summary>
        public int? ProcessId { get; set; }

        /// <summary>
        /// Gets or sets the thread ID
        /// </summary>
        public int? ThreadId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the change was detected
        /// </summary>
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the timestamp when the change was processed
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Gets or sets the latency between detection and processing
        /// </summary>
        public TimeSpan? ProcessingLatency => ProcessedAt?.Subtract(DetectedAt);

        /// <summary>
        /// Gets or sets the priority of the change
        /// </summary>
        public ChangePriority Priority { get; set; } = ChangePriority.Normal;

        /// <summary>
        /// Gets or sets the confidence level of the change detection
        /// </summary>
        public ChangeConfidence Confidence { get; set; } = ChangeConfidence.High;

        /// <summary>
        /// Gets or sets additional metadata for the change context
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the tags associated with this change
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Gets or sets custom metadata for the change context
        /// </summary>
        public Dictionary<string, object>? CustomMetadata { get; set; }

        /// <summary>
        /// Gets or sets the correlation ID for linking related changes
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the parent change ID if this is a child change
        /// </summary>
        public string? ParentChangeId { get; set; }

        /// <summary>
        /// Gets or sets the sequence number within a batch or transaction
        /// </summary>
        public int? SequenceNumber { get; set; }

        /// <summary>
        /// Gets or sets whether this change is part of a rollback operation
        /// </summary>
        public bool IsRollback { get; set; }

        /// <summary>
        /// Gets or sets the reason for the change
        /// </summary>
        public string? ChangeReason { get; set; }

        /// <summary>
        /// Gets or sets the business process or workflow that triggered the change
        /// </summary>
        public string? BusinessProcess { get; set; }

        /// <summary>
        /// Gets or sets the environment where the change occurred
        /// </summary>
        public string? Environment { get; set; }

        /// <summary>
        /// Creates a shallow copy of this change context
        /// </summary>
        public EnhancedChangeContext Clone()
        {
            return new EnhancedChangeContext
            {
                ChangeId = this.ChangeId,
                Source = this.Source,
                ApplicationName = this.ApplicationName,
                ApplicationVersion = this.ApplicationVersion,
                HostName = this.HostName,
                HostIPAddress = this.HostIPAddress,
                UserName = this.UserName,
                UserDomain = this.UserDomain,
                SessionId = this.SessionId,
                TransactionId = this.TransactionId,
                BatchId = this.BatchId,
                ProcessId = this.ProcessId,
                ThreadId = this.ThreadId,
                DetectedAt = this.DetectedAt,
                ProcessedAt = this.ProcessedAt,
                Priority = this.Priority,
                Confidence = this.Confidence,
                Tags = new List<string>(this.Tags),
                CustomMetadata = this.CustomMetadata != null ? new Dictionary<string, object>(this.CustomMetadata) : null,
                CorrelationId = this.CorrelationId,
                ParentChangeId = this.ParentChangeId,
                SequenceNumber = this.SequenceNumber,
                IsRollback = this.IsRollback,
                ChangeReason = this.ChangeReason,
                BusinessProcess = this.BusinessProcess,
                Environment = this.Environment
            };
        }

        /// <summary>
        /// Gets a string representation of the change context
        /// </summary>
        public override string ToString()
        {
            return $"{Source} by {UserName}@{HostName} at {DetectedAt:yyyy-MM-dd HH:mm:ss}";
        }
    }

    /// <summary>
    /// Represents the priority level of a change
    /// </summary>
    public enum ChangePriority
    {
        /// <summary>
        /// Low priority change
        /// </summary>
        Low = 1,

        /// <summary>
        /// Normal priority change
        /// </summary>
        Normal = 2,

        /// <summary>
        /// Medium priority change
        /// </summary>
        Medium = 3,

        /// <summary>
        /// High priority change
        /// </summary>
        High = 4,

        /// <summary>
        /// Critical priority change
        /// </summary>
        Critical = 5,

        /// <summary>
        /// Emergency priority change
        /// </summary>
        Emergency = 6
    }

    /// <summary>
    /// Represents the confidence level of change detection
    /// </summary>
    public enum ChangeConfidence
    {
        /// <summary>
        /// Very low confidence in change detection
        /// </summary>
        VeryLow = 1,

        /// <summary>
        /// Low confidence in change detection
        /// </summary>
        Low = 2,

        /// <summary>
        /// Medium confidence in change detection
        /// </summary>
        Medium = 3,

        /// <summary>
        /// High confidence in change detection
        /// </summary>
        High = 4,

        /// <summary>
        /// Very high confidence in change detection
        /// </summary>
        VeryHigh = 5
    }
}
