using System.Text.Json;
using Microsoft.Extensions.Logging;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.Serializers.Protobuf.Models;

namespace SqlDbEntityNotifier.Serializers.Protobuf;

/// <summary>
/// Protobuf serializer implementation for change events.
/// </summary>
public class ProtobufSerializer : ISerializer
{
    private readonly ILogger<ProtobufSerializer> _logger;

    /// <summary>
    /// Initializes a new instance of the ProtobufSerializer class.
    /// </summary>
    public ProtobufSerializer(ILogger<ProtobufSerializer> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string Serialize<T>(T obj)
    {
        try
        {
            if (obj is ChangeEvent changeEvent)
            {
                var proto = ConvertToProto(changeEvent);
                return Convert.ToBase64String(proto.ToByteArray());
            }

            // Fallback to JSON for other types
            return JsonSerializer.Serialize(obj);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serializing object to Protobuf");
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
                var proto = ChangeEventProto.FromByteArray(bytes);
                return (T)(object)ConvertFromProto(proto);
            }

            // Fallback to JSON for other types
            return JsonSerializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing Protobuf data");
            throw;
        }
    }

    /// <inheritdoc />
    public string ContentType => "application/x-protobuf";

    private static ChangeEventProto ConvertToProto(ChangeEvent changeEvent)
    {
        var proto = new ChangeEventProto
        {
            Source = changeEvent.Source,
            Schema = changeEvent.Schema,
            Table = changeEvent.Table,
            Operation = changeEvent.Operation,
            Offset = changeEvent.Offset,
            TimestampUtc = ((DateTimeOffset)changeEvent.TimestampUtc).ToUnixTimeMilliseconds()
        };

        // Convert Before data
        if (changeEvent.Before.HasValue)
        {
            proto.Before = changeEvent.Before.Value.GetRawText();
        }

        // Convert After data
        if (changeEvent.After.HasValue)
        {
            proto.After = changeEvent.After.Value.GetRawText();
        }

        // Convert metadata
        foreach (var metadata in changeEvent.Metadata)
        {
            proto.Metadata[metadata.Key] = metadata.Value;
        }

        return proto;
    }

    private static ChangeEvent ConvertFromProto(ChangeEventProto proto)
    {
        JsonElement? before = null;
        JsonElement? after = null;

        // Convert Before data
        if (!string.IsNullOrEmpty(proto.Before))
        {
            try
            {
                before = JsonDocument.Parse(proto.Before).RootElement;
            }
            catch (JsonException)
            {
                // If it's not valid JSON, treat as raw string
                before = JsonDocument.Parse($"\"{proto.Before}\"").RootElement;
            }
        }

        // Convert After data
        if (!string.IsNullOrEmpty(proto.After))
        {
            try
            {
                after = JsonDocument.Parse(proto.After).RootElement;
            }
            catch (JsonException)
            {
                // If it's not valid JSON, treat as raw string
                after = JsonDocument.Parse($"\"{proto.After}\"").RootElement;
            }
        }

        return new ChangeEvent
        {
            Source = proto.Source,
            Schema = proto.Schema,
            Table = proto.Table,
            Operation = proto.Operation,
            TimestampUtc = DateTimeOffset.FromUnixTimeMilliseconds(proto.TimestampUtc).DateTime,
            Offset = proto.Offset,
            Before = before,
            After = after,
            Metadata = proto.Metadata
        };
    }
}