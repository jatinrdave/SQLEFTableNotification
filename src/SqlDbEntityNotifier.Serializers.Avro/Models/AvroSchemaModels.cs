namespace SqlDbEntityNotifier.Serializers.Avro.Models;

/// <summary>
/// Avro schema definition.
/// </summary>
public sealed class AvroSchema
{
    /// <summary>
    /// Gets or sets the schema ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the schema type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema namespace.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema fields.
    /// </summary>
    public IList<AvroField> Fields { get; set; } = new List<AvroField>();

    /// <summary>
    /// Gets or sets the schema version.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the schema subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema definition as JSON.
    /// </summary>
    public string Definition { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the schema last updated timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Avro field definition.
/// </summary>
public sealed class AvroField
{
    /// <summary>
    /// Gets or sets the field name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field type.
    /// </summary>
    public object Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field default value.
    /// </summary>
    public object? Default { get; set; }

    /// <summary>
    /// Gets or sets the field documentation.
    /// </summary>
    public string? Documentation { get; set; }

    /// <summary>
    /// Gets or sets whether the field is optional.
    /// </summary>
    public bool Optional { get; set; }
}

/// <summary>
/// Avro map type definition.
/// </summary>
public sealed class AvroMapType
{
    /// <summary>
    /// Gets or sets the value type.
    /// </summary>
    public string Values { get; set; } = string.Empty;
}

/// <summary>
/// Avro array type definition.
/// </summary>
public sealed class AvroArrayType
{
    /// <summary>
    /// Gets or sets the item type.
    /// </summary>
    public string Items { get; set; } = string.Empty;
}

/// <summary>
/// Avro union type definition.
/// </summary>
public sealed class AvroUnionType
{
    /// <summary>
    /// Gets or sets the union types.
    /// </summary>
    public IList<string> Types { get; set; } = new List<string>();
}

/// <summary>
/// Avro message format for schema registry integration.
/// </summary>
public sealed class AvroMessage
{
    /// <summary>
    /// Gets or sets the schema ID.
    /// </summary>
    public int SchemaId { get; set; }

    /// <summary>
    /// Gets or sets the serialized data.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the message timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the message metadata.
    /// </summary>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Schema registry configuration.
/// </summary>
public sealed class SchemaRegistryConfig
{
    /// <summary>
    /// Gets or sets the compatibility level.
    /// </summary>
    public CompatibilityLevel CompatibilityLevel { get; set; } = CompatibilityLevel.Backward;

    /// <summary>
    /// Gets or sets whether schema validation is enabled.
    /// </summary>
    public bool ValidationEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the schema retention policy.
    /// </summary>
    public RetentionPolicy RetentionPolicy { get; set; } = RetentionPolicy.Delete;

    /// <summary>
    /// Gets or sets the schema retention period in days.
    /// </summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of schemas per subject.
    /// </summary>
    public int MaxSchemasPerSubject { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether to allow schema evolution.
    /// </summary>
    public bool AllowSchemaEvolution { get; set; } = true;
}

/// <summary>
/// Schema registry response for schema registration.
/// </summary>
public sealed class SchemaRegistrationResponse
{
    /// <summary>
    /// Gets or sets the schema ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the schema version.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the schema subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;
}

/// <summary>
/// Schema registry response for schema retrieval.
/// </summary>
public sealed class SchemaResponse
{
    /// <summary>
    /// Gets or sets the schema ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the schema version.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the schema subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema definition.
    /// </summary>
    public string Schema { get; set; } = string.Empty;
}

/// <summary>
/// Schema registry response for subject listing.
/// </summary>
public sealed class SubjectsResponse
{
    /// <summary>
    /// Gets or sets the list of subjects.
    /// </summary>
    public IList<string> Subjects { get; set; } = new List<string>();
}

/// <summary>
/// Schema registry response for version listing.
/// </summary>
public sealed class VersionsResponse
{
    /// <summary>
    /// Gets or sets the list of versions.
    /// </summary>
    public IList<int> Versions { get; set; } = new List<int>();
}

/// <summary>
/// Schema registry response for compatibility check.
/// </summary>
public sealed class CompatibilityResponse
{
    /// <summary>
    /// Gets or sets whether the schema is compatible.
    /// </summary>
    public bool IsCompatible { get; set; }
}

/// <summary>
/// Schema registry error response.
/// </summary>
public sealed class SchemaRegistryError
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public int ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Compatibility levels for schema evolution.
/// </summary>
public enum CompatibilityLevel
{
    /// <summary>
    /// No compatibility checks.
    /// </summary>
    None,

    /// <summary>
    /// Backward compatibility (new schema can read data written with old schema).
    /// </summary>
    Backward,

    /// <summary>
    /// Forward compatibility (old schema can read data written with new schema).
    /// </summary>
    Forward,

    /// <summary>
    /// Full compatibility (both backward and forward).
    /// </summary>
    Full,

    /// <summary>
    /// Backward transitive compatibility.
    /// </summary>
    BackwardTransitive,

    /// <summary>
    /// Forward transitive compatibility.
    /// </summary>
    ForwardTransitive,

    /// <summary>
    /// Full transitive compatibility.
    /// </summary>
    FullTransitive
}

/// <summary>
/// Schema retention policies.
/// </summary>
public enum RetentionPolicy
{
    /// <summary>
    /// Delete old schemas.
    /// </summary>
    Delete,

    /// <summary>
    /// Compact old schemas.
    /// </summary>
    Compact,

    /// <summary>
    /// Keep all schemas.
    /// </summary>
    Keep
}