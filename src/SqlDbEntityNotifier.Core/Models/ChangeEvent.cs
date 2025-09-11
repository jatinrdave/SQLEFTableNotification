using System.Text.Json;

namespace SqlDbEntityNotifier.Core.Models;

/// <summary>
/// Standard change event produced by database adapters.
/// Represents a single row change (INSERT, UPDATE, DELETE) from a database table.
/// </summary>
public sealed class ChangeEvent
{
    /// <summary>
    /// Gets the database source identifier (e.g., "postgres://orders-db").
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// Gets the database schema name (e.g., "public").
    /// </summary>
    public string Schema { get; init; } = string.Empty;

    /// <summary>
    /// Gets the table name that was changed.
    /// </summary>
    public string Table { get; init; } = string.Empty;

    /// <summary>
    /// Gets the operation type: INSERT, UPDATE, DELETE, BULK_INSERT, BULK_UPDATE, or BULK_DELETE.
    /// </summary>
    public string Operation { get; init; } = string.Empty;

    /// <summary>
    /// Gets the UTC timestamp when the change occurred.
    /// </summary>
    public DateTime TimestampUtc { get; init; }

    /// <summary>
    /// Gets the adapter-specific offset token for replay purposes.
    /// </summary>
    public string Offset { get; init; } = string.Empty;

    /// <summary>
    /// Gets the row data before the change (null for INSERT operations).
    /// </summary>
    public JsonElement? Before { get; init; }

    /// <summary>
    /// Gets the row data after the change (null for DELETE operations).
    /// </summary>
    public JsonElement? After { get; init; }

    /// <summary>
    /// Gets additional metadata about the change event.
    /// </summary>
    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Creates a new ChangeEvent with the specified properties.
    /// </summary>
    public static ChangeEvent Create(
        string source,
        string schema,
        string table,
        string operation,
        string offset,
        JsonElement? before = null,
        JsonElement? after = null,
        IDictionary<string, string>? metadata = null)
    {
        return new ChangeEvent
        {
            Source = source,
            Schema = schema,
            Table = table,
            Operation = operation,
            TimestampUtc = DateTime.UtcNow,
            Offset = offset,
            Before = before,
            After = after,
            Metadata = metadata ?? new Dictionary<string, string>()
        };
    }
}