namespace SqlDbEntityNotifier.Serializers.Avro.Models;

/// <summary>
/// Configuration options for Avro schema registry integration.
/// </summary>
public sealed class AvroSchemaRegistryOptions
{
    /// <summary>
    /// Gets or sets the schema registry URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authentication configuration.
    /// </summary>
    public AuthenticationOptions Authentication { get; set; } = new();

    /// <summary>
    /// Gets or sets the subject name strategy.
    /// </summary>
    public SubjectNameStrategy SubjectNameStrategy { get; set; } = SubjectNameStrategy.TopicName;

    /// <summary>
    /// Gets or sets whether to auto-register schemas.
    /// </summary>
    public bool AutoRegisterSchemas { get; set; } = true;

    /// <summary>
    /// Gets or sets the schema compatibility level.
    /// </summary>
    public CompatibilityLevel CompatibilityLevel { get; set; } = CompatibilityLevel.Backward;

    /// <summary>
    /// Gets or sets the cache configuration.
    /// </summary>
    public CacheOptions Cache { get; set; } = new();

    /// <summary>
    /// Gets or sets the retry configuration.
    /// </summary>
    public RetryOptions Retry { get; set; } = new();
}

/// <summary>
/// Authentication options for schema registry.
/// </summary>
public sealed class AuthenticationOptions
{
    /// <summary>
    /// Gets or sets the authentication type.
    /// </summary>
    public AuthenticationType Type { get; set; } = AuthenticationType.None;

    /// <summary>
    /// Gets or sets the username for basic authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for basic authentication.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key for API key authentication.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API secret for API key authentication.
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;
}

/// <summary>
/// Authentication types for schema registry.
/// </summary>
public enum AuthenticationType
{
    /// <summary>
    /// No authentication.
    /// </summary>
    None,

    /// <summary>
    /// Basic authentication.
    /// </summary>
    Basic,

    /// <summary>
    /// API key authentication.
    /// </summary>
    ApiKey
}

/// <summary>
/// Subject name strategies for schema registry.
/// </summary>
public enum SubjectNameStrategy
{
    /// <summary>
    /// Use topic name as subject name.
    /// </summary>
    TopicName,

    /// <summary>
    /// Use record name as subject name.
    /// </summary>
    RecordName,

    /// <summary>
    /// Use topic and record name as subject name.
    /// </summary>
    TopicRecordName
}

/// <summary>
/// Schema compatibility levels.
/// </summary>
public enum CompatibilityLevel
{
    /// <summary>
    /// Backward compatibility.
    /// </summary>
    Backward,

    /// <summary>
    /// Forward compatibility.
    /// </summary>
    Forward,

    /// <summary>
    /// Full compatibility.
    /// </summary>
    Full,

    /// <summary>
    /// No compatibility checks.
    /// </summary>
    None
}

/// <summary>
/// Cache configuration for schema registry.
/// </summary>
public sealed class CacheOptions
{
    /// <summary>
    /// Gets or sets the maximum number of schemas to cache.
    /// </summary>
    public int MaxSchemas { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the cache TTL in seconds.
    /// </summary>
    public int TtlSeconds { get; set; } = 3600;

    /// <summary>
    /// Gets or sets whether to enable caching.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Retry configuration for schema registry.
/// </summary>
public sealed class RetryOptions
{
    /// <summary>
    /// Gets or sets the maximum number of retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the retry backoff in seconds.
    /// </summary>
    public int BackoffSeconds { get; set; } = 2;

    /// <summary>
    /// Gets or sets whether to enable retries.
    /// </summary>
    public bool Enabled { get; set; } = true;
}