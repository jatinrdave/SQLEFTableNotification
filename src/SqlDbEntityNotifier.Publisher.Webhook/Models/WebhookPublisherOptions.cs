namespace SqlDbEntityNotifier.Publisher.Webhook.Models;

/// <summary>
/// Configuration options for the Webhook publisher.
/// </summary>
public sealed class WebhookPublisherOptions
{
    /// <summary>
    /// Gets or sets the webhook endpoint URL.
    /// </summary>
    public string EndpointUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP method to use (default: POST).
    /// </summary>
    public string HttpMethod { get; set; } = "POST";

    /// <summary>
    /// Gets or sets the default headers to include in requests.
    /// </summary>
    public IDictionary<string, string> DefaultHeaders { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the timeout in seconds for HTTP requests.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the retry configuration.
    /// </summary>
    public RetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Gets or sets the HMAC signing configuration.
    /// </summary>
    public HmacOptions Hmac { get; set; } = new();

    /// <summary>
    /// Gets or sets the OAuth configuration.
    /// </summary>
    public OAuthOptions OAuth { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to use HTTPS only.
    /// </summary>
    public bool RequireHttps { get; set; } = true;
}

/// <summary>
/// Retry configuration for Webhook publisher.
/// </summary>
public sealed class RetryOptions
{
    /// <summary>
    /// Gets or sets the maximum number of retries.
    /// </summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>
    /// Gets or sets the retry backoff in seconds.
    /// </summary>
    public int BackoffSeconds { get; set; } = 2;

    /// <summary>
    /// Gets or sets whether to enable retries.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// HMAC signing configuration for Webhook publisher.
/// </summary>
public sealed class HmacOptions
{
    /// <summary>
    /// Gets or sets the HMAC signing key (base64 encoded).
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the header name for the HMAC signature.
    /// </summary>
    public string SignatureHeader { get; set; } = "X-Signature";

    /// <summary>
    /// Gets or sets whether to enable HMAC signing.
    /// </summary>
    public bool Enabled { get; set; } = false;
}

/// <summary>
/// OAuth configuration for Webhook publisher.
/// </summary>
public sealed class OAuthOptions
{
    /// <summary>
    /// Gets or sets the OAuth token endpoint URL.
    /// </summary>
    public string TokenEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OAuth client ID.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OAuth client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OAuth scope.
    /// </summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to enable OAuth authentication.
    /// </summary>
    public bool Enabled { get; set; } = false;
}