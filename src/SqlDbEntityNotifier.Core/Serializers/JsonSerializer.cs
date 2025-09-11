using System.Text.Json;
using SqlDbEntityNotifier.Core.Interfaces;

namespace SqlDbEntityNotifier.Core.Serializers;

/// <summary>
/// JSON serializer implementation using System.Text.Json.
/// </summary>
public class JsonSerializer : ISerializer
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the JsonSerializer class.
    /// </summary>
    /// <param name="options">Optional JSON serializer options.</param>
    public JsonSerializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public string Serialize<T>(T obj)
    {
        return System.Text.Json.JsonSerializer.Serialize(obj, _options);
    }

    /// <inheritdoc />
    public T? Deserialize<T>(string data)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(data, _options);
    }

    /// <inheritdoc />
    public string ContentType => "application/json";
}