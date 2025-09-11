using System.Text.Json;

namespace SqlDbEntityNotifier.Serializers.Protobuf.Models;

/// <summary>
/// Simplified protobuf representation of a change event.
/// This is a simplified implementation for demonstration purposes.
/// In a real implementation, you would use protobuf code generation tools.
/// </summary>
public sealed class ChangeEventProto
{
    /// <summary>
    /// Gets or sets the source.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema.
    /// </summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the table.
    /// </summary>
    public string Table { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation.
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp in Unix milliseconds.
    /// </summary>
    public long TimestampUtc { get; set; }

    /// <summary>
    /// Gets or sets the offset.
    /// </summary>
    public string Offset { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the before data as JSON string.
    /// </summary>
    public string? Before { get; set; }

    /// <summary>
    /// Gets or sets the after data as JSON string.
    /// </summary>
    public string? After { get; set; }

    /// <summary>
    /// Gets or sets the metadata.
    /// </summary>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Converts to byte array for protobuf serialization.
    /// </summary>
    public byte[] ToByteArray()
    {
        var json = JsonSerializer.Serialize(this);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Creates from byte array.
    /// </summary>
    public static ChangeEventProto FromByteArray(byte[] data)
    {
        var json = System.Text.Encoding.UTF8.GetString(data);
        return JsonSerializer.Deserialize<ChangeEventProto>(json) ?? new ChangeEventProto();
    }
}