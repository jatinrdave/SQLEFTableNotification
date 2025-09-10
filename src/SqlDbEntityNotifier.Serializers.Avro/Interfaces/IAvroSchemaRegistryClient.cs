using SqlDbEntityNotifier.Serializers.Avro.Models;

namespace SqlDbEntityNotifier.Serializers.Avro;

/// <summary>
/// Interface for Avro schema registry client.
/// </summary>
public interface IAvroSchemaRegistryClient
{
    /// <summary>
    /// Registers a schema in the schema registry.
    /// </summary>
    /// <param name="subject">The schema subject.</param>
    /// <param name="schema">The schema to register.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registered schema with ID.</returns>
    Task<AvroSchema> RegisterSchemaAsync(string subject, AvroSchema schema, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a schema by ID from the schema registry.
    /// </summary>
    /// <param name="schemaId">The schema ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The schema or null if not found.</returns>
    Task<AvroSchema?> GetSchemaAsync(int schemaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a schema by subject and version from the schema registry.
    /// </summary>
    /// <param name="subject">The schema subject.</param>
    /// <param name="version">The schema version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The schema or null if not found.</returns>
    Task<AvroSchema?> GetSchemaBySubjectAsync(string subject, int version = -1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest version of a schema by subject.
    /// </summary>
    /// <param name="subject">The schema subject.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest schema or null if not found.</returns>
    Task<AvroSchema?> GetLatestSchemaAsync(string subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all subjects from the schema registry.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of subject names.</returns>
    Task<IList<string>> GetSubjectsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all versions of a schema subject.
    /// </summary>
    /// <param name="subject">The schema subject.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of schema versions.</returns>
    Task<IList<int>> GetSchemaVersionsAsync(string subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a schema version.
    /// </summary>
    /// <param name="subject">The schema subject.</param>
    /// <param name="version">The schema version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task DeleteSchemaVersionAsync(string subject, int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a schema subject.
    /// </summary>
    /// <param name="subject">The schema subject.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task DeleteSubjectAsync(string subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a schema is compatible with an existing schema.
    /// </summary>
    /// <param name="subject">The schema subject.</param>
    /// <param name="schema">The schema to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if compatible, false otherwise.</returns>
    Task<bool> IsCompatibleAsync(string subject, AvroSchema schema, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the schema registry configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The schema registry configuration.</returns>
    Task<SchemaRegistryConfig> GetConfigAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the schema registry configuration.
    /// </summary>
    /// <param name="config">The new configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task UpdateConfigAsync(SchemaRegistryConfig config, CancellationToken cancellationToken = default);
}