using System;
using System.Collections.Generic;
using SQLDBEntityNotifier.Interfaces;

namespace SQLDBEntityNotifier.Models
{
    /// <summary>
    /// Represents a column schema change
    /// </summary>
    public class ColumnSchemaChange
    {
        /// <summary>
        /// Gets or sets the name of the column
        /// </summary>
        public string ColumnName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of column change
        /// </summary>
        public ColumnChangeType ChangeType { get; set; }

        /// <summary>
        /// Gets or sets the old column definition
        /// </summary>
        public ColumnDefinition? OldDefinition { get; set; }

        /// <summary>
        /// Gets or sets the new column definition
        /// </summary>
        public ColumnDefinition? NewDefinition { get; set; }

        /// <summary>
        /// Gets or sets the old column name (for rename operations)
        /// </summary>
        public string? OldColumnName { get; set; }

        /// <summary>
        /// Gets or sets the new column name (for rename operations)
        /// </summary>
        public string? NewColumnName { get; set; }

        /// <summary>
        /// Gets or sets the ordinal position of the column
        /// </summary>
        public int OrdinalPosition { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the column change
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Creates a shallow copy of this column schema change
        /// </summary>
        public ColumnSchemaChange Clone()
        {
            return new ColumnSchemaChange
            {
                ColumnName = this.ColumnName,
                ChangeType = this.ChangeType,
                OldDefinition = this.OldDefinition?.Clone(),
                NewDefinition = this.NewDefinition?.Clone(),
                OldColumnName = this.OldColumnName,
                NewColumnName = this.NewColumnName,
                OrdinalPosition = this.OrdinalPosition,
                Metadata = this.Metadata != null ? new Dictionary<string, object>(this.Metadata) : null
            };
        }
    }

    /// <summary>
    /// Represents an index change
    /// </summary>
    public class IndexChange
    {
        /// <summary>
        /// Gets or sets the name of the index
        /// </summary>
        public string IndexName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of index change
        /// </summary>
        public IndexChangeType ChangeType { get; set; }

        /// <summary>
        /// Gets or sets the old index definition
        /// </summary>
        public IndexDefinition? OldDefinition { get; set; }

        /// <summary>
        /// Gets or sets the new index definition
        /// </summary>
        public IndexDefinition? NewDefinition { get; set; }

        /// <summary>
        /// Gets or sets the old index name (for rename operations)
        /// </summary>
        public string? OldIndexName { get; set; }

        /// <summary>
        /// Gets or sets the new index name (for rename operations)
        /// </summary>
        public string? NewIndexName { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the index change
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Creates a shallow copy of this index change
        /// </summary>
        public IndexChange Clone()
        {
            return new IndexChange
            {
                IndexName = this.IndexName,
                ChangeType = this.ChangeType,
                OldDefinition = this.OldDefinition?.Clone(),
                NewDefinition = this.NewDefinition?.Clone(),
                OldIndexName = this.OldIndexName,
                NewIndexName = this.NewIndexName,
                Metadata = this.Metadata != null ? new Dictionary<string, object>(this.Metadata) : null
            };
        }
    }

    /// <summary>
    /// Represents a constraint change
    /// </summary>
    public class ConstraintChange
    {
        /// <summary>
        /// Gets or sets the name of the constraint
        /// </summary>
        public string ConstraintName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of constraint
        /// </summary>
        public ConstraintType ConstraintType { get; set; }

        /// <summary>
        /// Gets or sets the type of constraint change
        /// </summary>
        public ConstraintChangeType ChangeType { get; set; }

        /// <summary>
        /// Gets or sets the old constraint definition
        /// </summary>
        public ConstraintDefinition? OldDefinition { get; set; }

        /// <summary>
        /// Gets or sets the new constraint definition
        /// </summary>
        public ConstraintDefinition? NewDefinition { get; set; }

        /// <summary>
        /// Gets or sets the old constraint name (for rename operations)
        /// </summary>
        public string? OldConstraintName { get; set; }

        /// <summary>
        /// Gets or sets the new constraint name (for rename operations)
        /// </summary>
        public string? NewConstraintName { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the constraint change
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Creates a shallow copy of this constraint change
        /// </summary>
        public ConstraintChange Clone()
        {
            return new ConstraintChange
            {
                ConstraintName = this.ConstraintName,
                ConstraintType = this.ConstraintType,
                ChangeType = this.ChangeType,
                OldDefinition = this.OldDefinition?.Clone(),
                NewDefinition = this.NewDefinition?.Clone(),
                OldConstraintName = this.OldConstraintName,
                NewConstraintName = this.NewConstraintName,
                Metadata = this.Metadata != null ? new Dictionary<string, object>(this.Metadata) : null
            };
        }
    }

    /// <summary>
    /// Represents a table schema change
    /// </summary>
    public class TableSchemaChange
    {
        /// <summary>
        /// Gets or sets the type of table change
        /// </summary>
        public TableChangeType ChangeType { get; set; }

        /// <summary>
        /// Gets or sets the old table definition
        /// </summary>
        public TableDefinition? OldDefinition { get; set; }

        /// <summary>
        /// Gets or sets the new table definition
        /// </summary>
        public TableDefinition? NewDefinition { get; set; }

        /// <summary>
        /// Gets or sets the old table name (for rename operations)
        /// </summary>
        public string? OldTableName { get; set; }

        /// <summary>
        /// Gets or sets the new table name (for rename operations)
        /// </summary>
        public string? NewTableName { get; set; }

        /// <summary>
        /// Gets or sets the old schema name (for schema changes)
        /// </summary>
        public string? OldSchemaName { get; set; }

        /// <summary>
        /// Gets or sets the new schema name (for schema changes)
        /// </summary>
        public string? NewSchemaName { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the table change
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Creates a shallow copy of this table schema change
        /// </summary>
        public TableSchemaChange Clone()
        {
            return new TableSchemaChange
            {
                ChangeType = this.ChangeType,
                OldDefinition = this.OldDefinition?.Clone(),
                NewDefinition = this.NewDefinition?.Clone(),
                OldTableName = this.OldTableName,
                NewTableName = this.NewTableName,
                OldSchemaName = this.OldSchemaName,
                NewSchemaName = this.NewSchemaName,
                Metadata = this.Metadata != null ? new Dictionary<string, object>(this.Metadata) : null
            };
        }
    }

    /// <summary>
    /// Represents the type of index change
    /// </summary>
    public enum IndexChangeType
    {
        /// <summary>
        /// Index was created
        /// </summary>
        Created = 1,

        /// <summary>
        /// Index was dropped
        /// </summary>
        Dropped = 2,

        /// <summary>
        /// Index was renamed
        /// </summary>
        Renamed = 3,

        /// <summary>
        /// Index was modified
        /// </summary>
        Modified = 4,

        /// <summary>
        /// Index was disabled
        /// </summary>
        Disabled = 5,

        /// <summary>
        /// Index was enabled
        /// </summary>
        Enabled = 6,

        /// <summary>
        /// Index was rebuilt
        /// </summary>
        Rebuilt = 7
    }

    /// <summary>
    /// Represents the type of constraint change
    /// </summary>
    public enum ConstraintChangeType
    {
        /// <summary>
        /// Constraint was created
        /// </summary>
        Created = 1,

        /// <summary>
        /// Constraint was dropped
        /// </summary>
        Dropped = 2,

        /// <summary>
        /// Constraint was renamed
        /// </summary>
        Renamed = 3,

        /// <summary>
        /// Constraint was modified
        /// </summary>
        Modified = 4,

        /// <summary>
        /// Constraint was disabled
        /// </summary>
        Disabled = 5,

        /// <summary>
        /// Constraint was enabled
        /// </summary>
        Enabled = 6
    }

    /// <summary>
    /// Represents the type of table change
    /// </summary>
    public enum TableChangeType
    {
        /// <summary>
        /// Table was created
        /// </summary>
        Created = 1,

        /// <summary>
        /// Table was dropped
        /// </summary>
        Dropped = 2,

        /// <summary>
        /// Table was renamed
        /// </summary>
        Renamed = 3,

        /// <summary>
        /// Table was truncated
        /// </summary>
        Truncated = 4,

        /// <summary>
        /// Table was modified
        /// </summary>
        Modified = 5,

        /// <summary>
        /// Table was moved to different schema
        /// </summary>
        SchemaChanged = 6
    }

    /// <summary>
    /// Represents the type of constraint
    /// </summary>
    public enum ConstraintType
    {
        /// <summary>
        /// Primary key constraint
        /// </summary>
        PrimaryKey = 1,

        /// <summary>
        /// Foreign key constraint
        /// </summary>
        ForeignKey = 2,

        /// <summary>
        /// Unique constraint
        /// </summary>
        Unique = 3,

        /// <summary>
        /// Check constraint
        /// </summary>
        Check = 4,

        /// <summary>
        /// Default constraint
        /// </summary>
        Default = 5,

        /// <summary>
        /// Not null constraint
        /// </summary>
        NotNull = 6,

        /// <summary>
        /// Other constraint type
        /// </summary>
        Other = 99
    }

    /// <summary>
    /// Represents an index definition
    /// </summary>
    public class IndexDefinition
    {
        /// <summary>
        /// Gets or sets the name of the index
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the index name (alias for Name)
        /// </summary>
        public string IndexName => Name;

        /// <summary>
        /// Gets or sets whether the index is unique
        /// </summary>
        public bool IsUnique { get; set; }

        /// <summary>
        /// Gets or sets whether the index is clustered
        /// </summary>
        public bool IsClustered { get; set; }

        /// <summary>
        /// Gets or sets the index type
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the index is a primary key
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// Gets or sets whether the index is a unique constraint
        /// </summary>
        public bool IsUniqueConstraint { get; set; }

        /// <summary>
        /// Gets or sets the columns in the index
        /// </summary>
        public List<IndexColumn> Columns { get; set; } = new();

        /// <summary>
        /// Gets or sets the filter expression for filtered indexes
        /// </summary>
        public string? FilterExpression { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the index
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Creates a shallow copy of this index definition
        /// </summary>
        public IndexDefinition Clone()
        {
            return new IndexDefinition
            {
                Name = this.Name,
                IsUnique = this.IsUnique,
                IsClustered = this.IsClustered,
                Type = this.Type,
                IsPrimaryKey = this.IsPrimaryKey,
                IsUniqueConstraint = this.IsUniqueConstraint,
                Columns = this.Columns.ConvertAll(c => c.Clone()),
                FilterExpression = this.FilterExpression,
                Metadata = this.Metadata != null ? new Dictionary<string, object>(this.Metadata) : null
            };
        }
    }

    /// <summary>
    /// Represents an index column
    /// </summary>
    public class IndexColumn
    {
        /// <summary>
        /// Gets or sets the name of the column
        /// </summary>
        public string ColumnName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ordinal position in the index
        /// </summary>
        public int OrdinalPosition { get; set; }

        /// <summary>
        /// Gets or sets whether the column is sorted in descending order
        /// </summary>
        public bool IsDescending { get; set; }

        /// <summary>
        /// Creates a shallow copy of this index column
        /// </summary>
        public IndexColumn Clone()
        {
            return new IndexColumn
            {
                ColumnName = this.ColumnName,
                OrdinalPosition = this.OrdinalPosition,
                IsDescending = this.IsDescending
            };
        }
    }

    /// <summary>
    /// Represents a constraint definition
    /// </summary>
    public class ConstraintDefinition
    {
        /// <summary>
        /// Gets or sets the name of the constraint
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the constraint name (alias for Name)
        /// </summary>
        public string ConstraintName => Name;

        /// <summary>
        /// Gets or sets the type of constraint
        /// </summary>
        public ConstraintType Type { get; set; }

        /// <summary>
        /// Gets or sets the constraint expression (for check constraints)
        /// </summary>
        public string? Expression { get; set; }

        /// <summary>
        /// Gets or sets the referenced table (for foreign key constraints)
        /// </summary>
        public string? ReferencedTable { get; set; }

        /// <summary>
        /// Gets or sets the referenced columns (for foreign key constraints)
        /// </summary>
        public List<string>? ReferencedColumns { get; set; }

        /// <summary>
        /// Gets or sets the columns in the constraint
        /// </summary>
        public List<string> Columns { get; set; } = new();

        /// <summary>
        /// Gets or sets additional metadata for the constraint
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Creates a shallow copy of this constraint definition
        /// </summary>
        public ConstraintDefinition Clone()
        {
            return new ConstraintDefinition
            {
                Name = this.Name,
                Type = this.Type,
                Expression = this.Expression,
                ReferencedTable = this.ReferencedTable,
                ReferencedColumns = this.ReferencedColumns != null ? new List<string>(this.ReferencedColumns) : null,
                Columns = new List<string>(this.Columns),
                Metadata = this.Metadata != null ? new Dictionary<string, object>(this.Metadata) : null
            };
        }
    }

    /// <summary>
    /// Represents a table definition
    /// </summary>
    public class TableDefinition
    {
        /// <summary>
        /// Gets or sets the name of the table
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the table name (alias for Name)
        /// </summary>
        public string TableName => Name;

        /// <summary>
        /// Gets or sets the schema name
        /// </summary>
        public string SchemaName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the columns in the table
        /// </summary>
        public List<ColumnDefinition> Columns { get; set; } = new();

        /// <summary>
        /// Gets or sets the primary key columns
        /// </summary>
        public List<string> PrimaryKeyColumns { get; set; } = new();

        /// <summary>
        /// Gets or sets the indexes on the table
        /// </summary>
        public List<IndexDefinition> Indexes { get; set; } = new();

        /// <summary>
        /// Gets or sets the constraints on the table
        /// </summary>
        public List<ConstraintDefinition> Constraints { get; set; } = new();

        /// <summary>
        /// Gets or sets the creation date of the table
        /// </summary>
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last modified date of the table
        /// </summary>
        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets additional metadata for the table
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Creates a shallow copy of this table definition
        /// </summary>
        public TableDefinition Clone()
        {
            return new TableDefinition
            {
                Name = this.Name,
                SchemaName = this.SchemaName,
                Columns = this.Columns.ConvertAll(c => c.Clone()),
                PrimaryKeyColumns = new List<string>(this.PrimaryKeyColumns),
                Indexes = this.Indexes.ConvertAll(i => i.Clone()),
                Constraints = this.Constraints.ConvertAll(c => c.Clone()),
                Metadata = this.Metadata != null ? new Dictionary<string, object>(this.Metadata) : null
            };
        }
    }
}
