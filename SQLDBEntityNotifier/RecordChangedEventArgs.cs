using System;
using System.Collections.Generic;
using SQLDBEntityNotifier.Models;

namespace SQLDBEntityNotifier
{
    /// <summary>
    /// Event arguments for database record change notifications
    /// </summary>
    public class RecordChangedEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Gets or sets the changed entities
        /// </summary>
        public IEnumerable<T>? Entities { get; set; }

        /// <summary>
        /// Gets or sets the change context information
        /// </summary>
        public ChangeContextInfo? ChangeContext { get; set; }

        /// <summary>
        /// Gets or sets the change version number
        /// </summary>
        public long ChangeVersion { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the change was detected
        /// </summary>
        public DateTime ChangeDetectedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Information about the context of a database change
    /// </summary>
    public class ChangeContextInfo
    {
        /// <summary>
        /// Gets or sets the source context of the change
        /// </summary>
        public ChangeContext Context { get; set; }

        /// <summary>
        /// Gets or sets the name of the application that made the change
        /// </summary>
        public string? ApplicationName { get; set; }

        /// <summary>
        /// Gets or sets the name of the user who made the change
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// Gets or sets the host name where the change originated
        /// </summary>
        public string? HostName { get; set; }

        /// <summary>
        /// Gets or sets the SQL query that caused the change (if available)
        /// </summary>
        public string? ChangeQuery { get; set; }

        /// <summary>
        /// Gets or sets additional metadata about the change
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
