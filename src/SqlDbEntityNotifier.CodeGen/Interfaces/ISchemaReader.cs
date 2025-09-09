using SqlDbEntityNotifier.CodeGen.Models;

namespace SqlDbEntityNotifier.CodeGen;

/// <summary>
/// Interface for reading database schema information.
/// </summary>
public interface ISchemaReader
{
    /// <summary>
    /// Gets all tables from the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of table schemas.</returns>
    Task<IList<TableSchema>> GetTablesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific table schema.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The table schema.</returns>
    Task<TableSchema> GetTableAsync(string tableName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets columns for a specific table.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of column schemas.</returns>
    Task<IList<ColumnSchema>> GetColumnsAsync(string tableName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a database table schema.
/// </summary>
public sealed class TableSchema
{
    /// <summary>
    /// Gets or sets the table name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the table schema name.
    /// </summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the table comment or description.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Gets or sets the list of columns.
    /// </summary>
    public IList<ColumnSchema> Columns { get; set; } = new List<ColumnSchema>();

    /// <summary>
    /// Gets or sets the list of primary key columns.
    /// </summary>
    public IList<string> PrimaryKeys { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the list of foreign key constraints.
    /// </summary>
    public IList<ForeignKeySchema> ForeignKeys { get; set; } = new List<ForeignKeySchema>();
}

/// <summary>
/// Represents a database column schema.
/// </summary>
public sealed class ColumnSchema
{
    /// <summary>
    /// Gets or sets the column name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the column data type.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the column is nullable.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Gets or sets whether the column is a primary key.
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// Gets or sets whether the column is auto-increment.
    /// </summary>
    public bool IsAutoIncrement { get; set; }

    /// <summary>
    /// Gets or sets the column default value.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the column comment or description.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Gets or sets the maximum length for string columns.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the precision for numeric columns.
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// Gets or sets the scale for numeric columns.
    /// </summary>
    public int? Scale { get; set; }
}

/// <summary>
/// Represents a foreign key constraint.
/// </summary>
public sealed class ForeignKeySchema
{
    /// <summary>
    /// Gets or sets the foreign key name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the column name.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the referenced table name.
    /// </summary>
    public string ReferencedTableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the referenced column name.
    /// </summary>
    public string ReferencedColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delete action.
    /// </summary>
    public string DeleteAction { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the update action.
    /// </summary>
    public string UpdateAction { get; set; } = string.Empty;
}