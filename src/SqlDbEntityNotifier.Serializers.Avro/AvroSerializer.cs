using System.Text.Json;
using Apache.Avro;
using Apache.Avro.Generic;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.Serializers.Avro.Models;

namespace SqlDbEntityNotifier.Serializers.Avro;

/// <summary>
/// Avro serializer implementation for change events with schema registry integration.
/// </summary>
public class AvroSerializer : ISerializer
{
    private readonly ILogger<AvroSerializer> _logger;
    private readonly AvroSchemaRegistryOptions _options;
    private readonly ISchemaRegistryClient? _schemaRegistryClient;
    private readonly GenericRecordSerializer? _serializer;
    private readonly GenericRecordDeserializer? _deserializer;
    private readonly Schema _changeEventSchema;
    private readonly RecordSchema _recordSchema;

    /// <summary>
    /// Initializes a new instance of the AvroSerializer class.
    /// </summary>
    public AvroSerializer(
        ILogger<AvroSerializer> logger,
        IOptions<AvroSchemaRegistryOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        // Create Avro schema for ChangeEvent
        _changeEventSchema = CreateChangeEventSchema();
        _recordSchema = (RecordSchema)_changeEventSchema;

        // Initialize schema registry client if URL is provided
        if (!string.IsNullOrEmpty(_options.Url))
        {
            _schemaRegistryClient = CreateSchemaRegistryClient();
            _serializer = new GenericRecordSerializer(_schemaRegistryClient);
            _deserializer = new GenericRecordDeserializer(_schemaRegistryClient);
        }

        _logger.LogInformation("AvroSerializer initialized with schema registry: {HasRegistry}", _schemaRegistryClient != null);
    }

    /// <inheritdoc />
    public string Serialize<T>(T obj)
    {
        try
        {
            if (obj is ChangeEvent changeEvent)
            {
                var record = ConvertToAvroRecord(changeEvent);
                
                if (_serializer != null && _schemaRegistryClient != null)
                {
                    // Use schema registry
                    var bytes = _serializer.SerializeAsync(GetSubjectName(changeEvent), record).GetAwaiter().GetResult();
                    return Convert.ToBase64String(bytes);
                }
                else
                {
                    // Use direct Avro serialization
                    var bytes = SerializeDirect(record);
                    return Convert.ToBase64String(bytes);
                }
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
                GenericRecord record;

                if (_deserializer != null && _schemaRegistryClient != null)
                {
                    // Use schema registry
                    record = _deserializer.DeserializeAsync(bytes).GetAwaiter().GetResult();
                }
                else
                {
                    // Use direct Avro deserialization
                    record = DeserializeDirect(bytes);
                }

                return (T)(object)ConvertFromAvroRecord(record);
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

    private ISchemaRegistryClient CreateSchemaRegistryClient()
    {
        var config = new SchemaRegistryConfig
        {
            Url = _options.Url,
            BasicAuthCredentialsSource = AuthCredentialsSource.UserInfo
        };

        if (_options.Authentication.Type == AuthenticationType.Basic)
        {
            config.BasicAuthUserInfo = $"{_options.Authentication.Username}:{_options.Authentication.Password}";
        }
        else if (_options.Authentication.Type == AuthenticationType.ApiKey)
        {
            config.BasicAuthUserInfo = $"{_options.Authentication.ApiKey}:{_options.Authentication.ApiSecret}";
        }

        return new CachedSchemaRegistryClient(config);
    }

    private Schema CreateChangeEventSchema()
    {
        var schemaJson = @"
{
  ""type"": ""record"",
  ""name"": ""ChangeEvent"",
  ""namespace"": ""SqlDbEntityNotifier.Core.Models"",
  ""fields"": [
    { ""name"": ""source"", ""type"": ""string"" },
    { ""name"": ""schema"", ""type"": ""string"" },
    { ""name"": ""table"", ""type"": ""string"" },
    { ""name"": ""operation"", ""type"": ""string"" },
    { ""name"": ""timestampUtc"", ""type"": ""long"", ""logicalType"": ""timestamp-millis"" },
    { ""name"": ""offset"", ""type"": ""string"" },
    { ""name"": ""before"", ""type"": [""null"", ""string""], ""default"": null },
    { ""name"": ""after"", ""type"": [""null"", ""string""], ""default"": null },
    { ""name"": ""metadata"", ""type"": { ""type"": ""map"", ""values"": ""string"" } }
  ]
}";

        return Schema.Parse(schemaJson);
    }

    private GenericRecord ConvertToAvroRecord(ChangeEvent changeEvent)
    {
        var record = new GenericRecord(_recordSchema);
        
        record["source"] = changeEvent.Source;
        record["schema"] = changeEvent.Schema;
        record["table"] = changeEvent.Table;
        record["operation"] = changeEvent.Operation;
        record["timestampUtc"] = ((DateTimeOffset)changeEvent.TimestampUtc).ToUnixTimeMilliseconds();
        record["offset"] = changeEvent.Offset;
        record["before"] = changeEvent.Before?.GetRawText();
        record["after"] = changeEvent.After?.GetRawText();
        
        // Convert metadata dictionary
        var metadata = new Dictionary<string, string>();
        foreach (var kvp in changeEvent.Metadata)
        {
            metadata[kvp.Key] = kvp.Value;
        }
        record["metadata"] = metadata;

        return record;
    }

    private ChangeEvent ConvertFromAvroRecord(GenericRecord record)
    {
        JsonElement? before = null;
        JsonElement? after = null;

        // Convert Before data
        if (record["before"] is string beforeStr && !string.IsNullOrEmpty(beforeStr))
        {
            try
            {
                before = JsonDocument.Parse(beforeStr).RootElement;
            }
            catch (JsonException)
            {
                // If it's not valid JSON, treat as raw string
                before = JsonDocument.Parse($"\"{beforeStr}\"").RootElement;
            }
        }

        // Convert After data
        if (record["after"] is string afterStr && !string.IsNullOrEmpty(afterStr))
        {
            try
            {
                after = JsonDocument.Parse(afterStr).RootElement;
            }
            catch (JsonException)
            {
                // If it's not valid JSON, treat as raw string
                after = JsonDocument.Parse($"\"{afterStr}\"").RootElement;
            }
        }

        // Convert metadata
        var metadata = new Dictionary<string, string>();
        if (record["metadata"] is Dictionary<string, string> metadataDict)
        {
            metadata = metadataDict;
        }

        return new ChangeEvent
        {
            Source = record["source"]?.ToString() ?? string.Empty,
            Schema = record["schema"]?.ToString() ?? string.Empty,
            Table = record["table"]?.ToString() ?? string.Empty,
            Operation = record["operation"]?.ToString() ?? string.Empty,
            TimestampUtc = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(record["timestampUtc"])).DateTime,
            Offset = record["offset"]?.ToString() ?? string.Empty,
            Before = before,
            After = after,
            Metadata = metadata
        };
    }

    private byte[] SerializeDirect(GenericRecord record)
    {
        using var stream = new MemoryStream();
        using var encoder = new BinaryEncoder(stream);
        using var writer = new GenericDatumWriter<GenericRecord>(_recordSchema);
        
        writer.Write(record, encoder);
        return stream.ToArray();
    }

    private GenericRecord DeserializeDirect(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var decoder = new BinaryDecoder(stream);
        using var reader = new GenericDatumReader<GenericRecord>(_recordSchema, _recordSchema);
        
        return reader.Read(null, decoder);
    }

    private string GetSubjectName(ChangeEvent changeEvent)
    {
        return _options.SubjectNameStrategy switch
        {
            SubjectNameStrategy.TopicName => $"change-event-{changeEvent.Source}",
            SubjectNameStrategy.RecordName => "ChangeEvent",
            SubjectNameStrategy.TopicRecordName => $"change-event-{changeEvent.Source}-ChangeEvent",
            _ => "ChangeEvent"
        };
    }

    /// <summary>
    /// Disposes the Avro serializer resources.
    /// </summary>
    public void Dispose()
    {
        _serializer?.Dispose();
        _deserializer?.Dispose();
        _schemaRegistryClient?.Dispose();
    }
}