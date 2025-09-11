namespace SqlDbEntityNotifier.Core.Interfaces;

/// <summary>
/// Interface for serializing and deserializing objects.
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// Serializes an object to a string.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>The serialized string representation.</returns>
    string Serialize<T>(T obj);

    /// <summary>
    /// Deserializes a string to an object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize to.</typeparam>
    /// <param name="data">The string to deserialize.</param>
    /// <returns>The deserialized object.</returns>
    T? Deserialize<T>(string data);

    /// <summary>
    /// Gets the content type for this serializer.
    /// </summary>
    string ContentType { get; }
}