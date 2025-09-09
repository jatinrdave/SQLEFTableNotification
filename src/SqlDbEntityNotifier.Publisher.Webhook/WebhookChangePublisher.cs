using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.Publisher.Webhook.Models;

namespace SqlDbEntityNotifier.Publisher.Webhook;

/// <summary>
/// Webhook publisher implementation for change events.
/// </summary>
public class WebhookChangePublisher : IChangePublisher
{
    private readonly WebhookPublisherOptions _options;
    private readonly ILogger<WebhookChangePublisher> _logger;
    private readonly ISerializer _serializer;
    private readonly HttpClient _httpClient;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
    private string? _oauthToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the WebhookChangePublisher class.
    /// </summary>
    public WebhookChangePublisher(
        IOptions<WebhookPublisherOptions> options,
        ILogger<WebhookChangePublisher> logger,
        ISerializer serializer,
        HttpClient? httpClient = null)
    {
        _options = options.Value;
        _logger = logger;
        _serializer = serializer;
        _httpClient = httpClient ?? new HttpClient();

        // Configure HTTP client
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        // Add default headers
        foreach (var header in _options.DefaultHeaders)
        {
            _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        // Create retry policy
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                _options.Retry.MaxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(_options.Retry.BackoffSeconds, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var statusCode = outcome.Result?.StatusCode.ToString() ?? "Unknown";
                    _logger.LogWarning("Webhook publish retry {RetryCount} after {Delay}ms. Status: {StatusCode}", 
                        retryCount, timespan.TotalMilliseconds, statusCode);
                });
    }

    /// <inheritdoc />
    public async Task PublishAsync(ChangeEvent changeEvent, CancellationToken cancellationToken = default)
    {
        if (!_options.Retry.Enabled)
        {
            await PublishInternalAsync(changeEvent, cancellationToken);
            return;
        }

        await _retryPolicy.ExecuteAsync(async (ct) =>
        {
            var response = await PublishInternalAsync(changeEvent, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"HTTP request failed with status {response.StatusCode}");
            }
            return response;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PublishBatchAsync(IEnumerable<ChangeEvent> changeEvents, CancellationToken cancellationToken = default)
    {
        var tasks = changeEvents.Select(ev => PublishAsync(ev, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task<HttpResponseMessage> PublishInternalAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        try
        {
            // Validate HTTPS requirement
            if (_options.RequireHttps && !_options.EndpointUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("HTTPS is required for webhook endpoints");
            }

            // Prepare the payload
            var payload = _serializer.Serialize(changeEvent);
            var content = new StringContent(payload, Encoding.UTF8, _serializer.ContentType);

            // Add HMAC signature if enabled
            if (_options.Hmac.Enabled && !string.IsNullOrEmpty(_options.Hmac.SigningKey))
            {
                var signature = CreateHmacSignature(payload, _options.Hmac.SigningKey);
                content.Headers.Add(_options.Hmac.SignatureHeader, signature);
            }

            // Add OAuth token if enabled
            if (_options.OAuth.Enabled)
            {
                await EnsureValidOAuthTokenAsync(cancellationToken);
                if (!string.IsNullOrEmpty(_oauthToken))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _oauthToken);
                }
            }

            // Add change event metadata as headers
            content.Headers.Add("X-Source", changeEvent.Source);
            content.Headers.Add("X-Schema", changeEvent.Schema);
            content.Headers.Add("X-Table", changeEvent.Table);
            content.Headers.Add("X-Operation", changeEvent.Operation);
            content.Headers.Add("X-Offset", changeEvent.Offset);
            content.Headers.Add("X-Timestamp", changeEvent.TimestampUtc.ToString("O"));

            // Send the request
            var request = new HttpRequestMessage(new HttpMethod(_options.HttpMethod), _options.EndpointUrl)
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Successfully published change event to webhook {Endpoint}. Status: {StatusCode}", 
                    _options.EndpointUrl, response.StatusCode);
            }
            else
            {
                _logger.LogWarning("Failed to publish change event to webhook {Endpoint}. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                    _options.EndpointUrl, response.StatusCode, response.ReasonPhrase);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error publishing change event to webhook {Endpoint}", _options.EndpointUrl);
            throw;
        }
    }

    private async Task EnsureValidOAuthTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_oauthToken) || DateTime.UtcNow >= _tokenExpiry)
        {
            await RefreshOAuthTokenAsync(cancellationToken);
        }
    }

    private async Task RefreshOAuthTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _options.OAuth.ClientId),
                new KeyValuePair<string, string>("client_secret", _options.OAuth.ClientSecret),
                new KeyValuePair<string, string>("scope", _options.OAuth.Scope)
            });

            var response = await _httpClient.PostAsync(_options.OAuth.TokenEndpoint, tokenRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenResponse);

            if (tokenData.TryGetProperty("access_token", out var accessTokenElement))
            {
                _oauthToken = accessTokenElement.GetString();
                
                // Set expiry time (default to 1 hour if not provided)
                var expiresIn = 3600; // 1 hour default
                if (tokenData.TryGetProperty("expires_in", out var expiresInElement))
                {
                    expiresIn = expiresInElement.GetInt32();
                }
                
                _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // Refresh 1 minute early
                
                _logger.LogDebug("OAuth token refreshed successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh OAuth token");
            throw;
        }
    }

    private static string CreateHmacSignature(string payload, string base64Key)
    {
        try
        {
            var keyBytes = Convert.FromBase64String(base64Key);
            using var hmac = new HMACSHA256(keyBytes);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            var signatureBytes = hmac.ComputeHash(payloadBytes);
            return Convert.ToBase64String(signatureBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to create HMAC signature", ex);
        }
    }

    /// <summary>
    /// Disposes the HTTP client.
    /// </summary>
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}