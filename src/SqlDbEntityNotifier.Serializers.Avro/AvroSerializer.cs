using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.Serializers.Avro.Models;

namespace SqlDbEntityNotifier.Serializers.Avro;

/// <summary>
/// Avro serializer implementation for change events with schema registry integration.
/// This is a simplified implementation for demonstration purposes.
/// In a real implementation, you would use Apache Avro libraries.
/// </summary>
public class AvroSerializer : ISerializer
{
    private readonly ILogger<AvroSerializer> _logger;
    private readonly AvroSchemaRegistryOptions _options;

    /// <summary>
    /// Initializes a new instance of the AvroSerializer class.
    /// </summary>
    public AvroSerializer(
        ILogger<AvroSerializer> logger,
        IOptions<AvroSchemaRegistryOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        _logger.LogInformation("AvroSerializer initialized with schema registry: {HasRegistry}", !string.IsNullOrEmpty(_options.Url));
    }

    /// <inheritdoc />
    public string Serialize<T>(T obj)
    {
        try
        {
            if (obj is ChangeEvent changeEvent)
            {
                var avroData = ConvertToAvroData(changeEvent);
                return Convert.ToBase64String(avroData);
            }

            // Fallback to JSON for other types
            return JsonSerializer.Serialize(obj);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serializing object to Avro");
            throw;
        }
    }

    /// <inheritdoc />
    public T? Deserialize<T>(string data)
    {
        try
        {
            if (typeof(T) == typeof(ChangeEvent))
            {
                var bytes = Convert.FromBase64String(data);
                var changeEvent = ConvertFromAvroData(bytes);
                return (T)(object)changeEvent;
            }

            // Fallback to JSON for other types
            return JsonSerializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing Avro data");
            throw;
        }
    }

    /// <inheritdoc />
    public string ContentType => "application/avro";

    private byte[] ConvertToAvroData(ChangeEvent changeEvent)
    {
        // Simplified Avro serialization - in real implementation, use Apache Avro
        var avroObject = new
        {
            source = changeEvent.Source,
            schema = changeEvent.Schema,
            table = changeEvent.Table,
            operation = changeEvent.Operation,
            timestampUtc = ((DateTimeOffset)changeEvent.TimestampUtc).ToUnixTimeMilliseconds(),
            offset = changeEvent.Offset,
            before = changeEvent.Before?.GetRawText(),
            after = changeEvent.After?.GetRawText(),
            metadata = changeEvent.Metadata
        };

        var json = JsonSerializer.Serialize(avroObject);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    private ChangeEvent ConvertFromAvroData(byte[] data)
    {
        var json = System.Text.Encoding.UTF8.GetString(data);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        JsonElement? before = null;
        JsonElement? after = null;

        // Convert Before data
        if (root.TryGetProperty("before", out var beforeElement) && beforeElement.ValueKind != JsonValueKind.Null)
        {
            var beforeStr = beforeElement.GetString();
            if (!string.IsNullOrEmpty(beforeStr))
            {
                try
                {
                    before = JsonDocument.Parse(beforeStr).RootElement;
                }
                catch (JsonException)
                {
                    before = JsonDocument.Parse($"\"{beforeStr}\"").RootElement;
                }
            }
        }

        // Convert After data
        if (root.TryGetProperty("after", out var afterElement) && afterElement.ValueKind != JsonValueKind.Null)
        {
            var afterStr = afterElement.GetString();
            if (!string.IsNullOrEmpty(afterStr))
            {
                try
                {
                    after = JsonDocument.Parse(afterStr).RootElement;
                }
                catch (JsonException)
                {
                    after = JsonDocument.Parse($"\"{afterStr}\"").RootElement;
                }
            }
        }

        // Convert metadata
        var metadata = new Dictionary<string, string>();
        if (root.TryGetProperty("metadata", out var metadataElement))
        {
            foreach (var property in metadataElement.EnumerateObject())
            {
                metadata[property.Name] = property.Value.GetString() ?? string.Empty;
            }
        }

        return new ChangeEvent
        {
            Source = root.GetProperty("source").GetString() ?? string.Empty,
            Schema = root.GetProperty("schema").GetString() ?? string.Empty,
            Table = root.GetProperty("table").GetString() ?? string.Empty,
            Operation = root.GetProperty("operation").GetString() ?? string.Empty,
            TimestampUtc = DateTimeOffset.FromUnixTimeMilliseconds(root.GetProperty("timestampUtc").GetInt64()).DateTime,
            Offset = root.GetProperty("offset").GetString() ?? string.Empty,
            Before = before,
            After = after,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Disposes the Avro serializer resources.
    /// </summary>
    public void Dispose()
    {
        // No resources to dispose in simplified implementation
    }
}