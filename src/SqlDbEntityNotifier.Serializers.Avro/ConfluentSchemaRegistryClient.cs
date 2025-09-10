using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlDbEntityNotifier.Serializers.Avro.Models;

namespace SqlDbEntityNotifier.Serializers.Avro;

/// <summary>
/// Confluent Schema Registry client implementation.
/// </summary>
public class ConfluentSchemaRegistryClient : IAvroSchemaRegistryClient
{
    private readonly ILogger<ConfluentSchemaRegistryClient> _logger;
    private readonly AvroSchemaRegistryOptions _options;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the ConfluentSchemaRegistryClient class.
    /// </summary>
    public ConfluentSchemaRegistryClient(
        ILogger<ConfluentSchemaRegistryClient> logger,
        IOptions<AvroSchemaRegistryOptions> options,
        HttpClient httpClient)
    {
        _logger = logger;
        _options = options.Value;
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Configure HTTP client
        _httpClient.BaseAddress = new Uri(_options.Url);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.schemaregistry.v1+json");

        if (!string.IsNullOrEmpty(_options.Username) && !string.IsNullOrEmpty(_options.Password))
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.Username}:{_options.Password}"));
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");
        }
    }

    /// <inheritdoc />
    public async Task<AvroSchema> RegisterSchemaAsync(string subject, AvroSchema schema, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Registering schema for subject: {Subject}", subject);

            var request = new
            {
                schema = JsonSerializer.Serialize(schema, _jsonOptions)
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/subjects/{Uri.EscapeDataString(subject)}/versions", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var registrationResponse = JsonSerializer.Deserialize<SchemaRegistrationResponse>(responseJson, _jsonOptions);

            if (registrationResponse == null)
            {
                throw new InvalidOperationException("Failed to deserialize schema registration response");
            }

            schema.Id = registrationResponse.Id;
            schema.Version = registrationResponse.Version;
            schema.Subject = registrationResponse.Subject;

            _logger.LogInformation("Schema registered successfully: {Subject}, ID: {SchemaId}, Version: {Version}", 
                subject, schema.Id, schema.Version);

            return schema;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering schema for subject: {Subject}", subject);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<AvroSchema?> GetSchemaAsync(int schemaId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting schema by ID: {SchemaId}", schemaId);

            var response = await _httpClient.GetAsync($"/schemas/ids/{schemaId}", cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var schemaResponse = JsonSerializer.Deserialize<SchemaResponse>(responseJson, _jsonOptions);

            if (schemaResponse == null)
            {
                return null;
            }

            var schema = JsonSerializer.Deserialize<AvroSchema>(schemaResponse.Schema, _jsonOptions);
            if (schema != null)
            {
                schema.Id = schemaResponse.Id;
                schema.Version = schemaResponse.Version;
                schema.Subject = schemaResponse.Subject;
            }

            return schema;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schema by ID: {SchemaId}", schemaId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<AvroSchema?> GetSchemaBySubjectAsync(string subject, int version = -1, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting schema for subject: {Subject}, version: {Version}", subject, version);

            var url = version == -1 
                ? $"/subjects/{Uri.EscapeDataString(subject)}/versions/latest"
                : $"/subjects/{Uri.EscapeDataString(subject)}/versions/{version}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var schemaResponse = JsonSerializer.Deserialize<SchemaResponse>(responseJson, _jsonOptions);

            if (schemaResponse == null)
            {
                return null;
            }

            var schema = JsonSerializer.Deserialize<AvroSchema>(schemaResponse.Schema, _jsonOptions);
            if (schema != null)
            {
                schema.Id = schemaResponse.Id;
                schema.Version = schemaResponse.Version;
                schema.Subject = schemaResponse.Subject;
            }

            return schema;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schema for subject: {Subject}, version: {Version}", subject, version);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<AvroSchema?> GetLatestSchemaAsync(string subject, CancellationToken cancellationToken = default)
    {
        return await GetSchemaBySubjectAsync(subject, -1, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IList<string>> GetSubjectsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting all subjects");

            var response = await _httpClient.GetAsync("/subjects", cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var subjectsResponse = JsonSerializer.Deserialize<SubjectsResponse>(responseJson, _jsonOptions);

            return subjectsResponse?.Subjects ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subjects");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IList<int>> GetSchemaVersionsAsync(string subject, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting schema versions for subject: {Subject}", subject);

            var response = await _httpClient.GetAsync($"/subjects/{Uri.EscapeDataString(subject)}/versions", cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new List<int>();
            }

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var versionsResponse = JsonSerializer.Deserialize<VersionsResponse>(responseJson, _jsonOptions);

            return versionsResponse?.Versions ?? new List<int>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schema versions for subject: {Subject}", subject);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteSchemaVersionAsync(string subject, int version, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting schema version: {Subject}, version: {Version}", subject, version);

            var response = await _httpClient.DeleteAsync($"/subjects/{Uri.EscapeDataString(subject)}/versions/{version}", cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Schema version deleted: {Subject}, version: {Version}", subject, version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting schema version: {Subject}, version: {Version}", subject, version);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteSubjectAsync(string subject, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting subject: {Subject}", subject);

            var response = await _httpClient.DeleteAsync($"/subjects/{Uri.EscapeDataString(subject)}", cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Subject deleted: {Subject}", subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subject: {Subject}", subject);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsCompatibleAsync(string subject, AvroSchema schema, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking schema compatibility for subject: {Subject}", subject);

            var request = new
            {
                schema = JsonSerializer.Serialize(schema, _jsonOptions)
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/compatibility/subjects/{Uri.EscapeDataString(subject)}/versions/latest", content, cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return true; // No existing schema, so it's compatible
            }

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var compatibilityResponse = JsonSerializer.Deserialize<CompatibilityResponse>(responseJson, _jsonOptions);

            return compatibilityResponse?.IsCompatible ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking schema compatibility for subject: {Subject}", subject);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<SchemaRegistryConfig> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting schema registry configuration");

            var response = await _httpClient.GetAsync("/config", cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var config = JsonSerializer.Deserialize<SchemaRegistryConfig>(responseJson, _jsonOptions);

            return config ?? new SchemaRegistryConfig();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schema registry configuration");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task UpdateConfigAsync(SchemaRegistryConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating schema registry configuration");

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync("/config", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Schema registry configuration updated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating schema registry configuration");
            throw;
        }
    }

    /// <summary>
    /// Disposes the schema registry client.
    /// </summary>
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}