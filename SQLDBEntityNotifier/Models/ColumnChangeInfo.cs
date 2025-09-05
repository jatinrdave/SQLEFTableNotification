using System;
using System.Collections.Generic;

namespace SQLDBEntityNotifier.Models
{
    /// <summary>
    /// Represents detailed information about a column change
    /// </summary>
    public class ColumnChangeInfo
    {
        /// <summary>
        /// Gets or sets the name of the column
        /// </summary>
        public string ColumnName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the data type of the column
        /// </summary>
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the old value before the change
        /// </summary>
        public object? OldValue { get; set; }

        /// <summary>
        /// Gets or sets the new value after the change
        /// </summary>
        public object? NewValue { get; set; }

        /// <summary>
        /// Gets or sets whether the column value has actually changed
        /// </summary>
        public bool HasChanged { get; set; }

        /// <summary>
        /// Gets or sets the type of change for this column
        /// </summary>
        public ColumnChangeType ChangeType { get; set; }

        /// <summary>
        /// Gets or sets the ordinal position of the column in the table
        /// </summary>
        public int OrdinalPosition { get; set; }

        /// <summary>
        /// Gets or sets whether the column is part of the primary key
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// Gets or sets whether the column is nullable
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Gets or sets the maximum length for string columns
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// Gets or sets the precision for numeric columns
        /// </summary>
        public int? Precision { get; set; }

        /// <summary>
        /// Gets or sets the scale for numeric columns
        /// </summary>
        public int? Scale { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the column
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Gets a string representation of the change
        /// </summary>
        public override string ToString()
        {
            return $"{ColumnName}: {OldValue} -> {NewValue} ({ChangeType})";
        }
    }

    /// <summary>
    /// Represents the type of change for a specific column
    /// </summary>
    public enum ColumnChangeType
    {
        /// <summary>
        /// Column value was inserted (new record)
        /// </summary>
        Inserted = 1,

        /// <summary>
        /// Column value was updated
        /// </summary>
        Updated = 2,

        /// <summary>
        /// Column value was deleted (record deletion)
        /// </summary>
        Deleted = 3,

        /// <summary>
        /// Column was added to the table schema
        /// </summary>
        Added = 4,

        /// <summary>
        /// Column was removed from the table schema
        /// </summary>
        Removed = 5,

        /// <summary>
        /// Column definition was modified (data type, constraints, etc.)
        /// </summary>
        Modified = 6,

        /// <summary>
        /// No change occurred for this column
        /// </summary>
        Unchanged = 7
    }
}

