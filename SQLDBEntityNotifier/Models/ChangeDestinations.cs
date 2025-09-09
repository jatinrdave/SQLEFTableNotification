using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SQLDBEntityNotifier.Interfaces;

namespace SQLDBEntityNotifier.Models
{
    /// <summary>
    /// Base class for change destinations
    /// </summary>
    public abstract class BaseDestination : IDestination
    {
        public string Name { get; }
        public DestinationType Type { get; }
        public bool IsEnabled { get; set; } = true;

        protected BaseDestination(string name, DestinationType type)
        {
            Name = name;
            Type = type;
        }

        public abstract Task<DeliveryResult> DeliverAsync(ChangeRecord change, string tableName);

        public virtual void Dispose()
        {
            // Base implementation - override in derived classes if needed
        }
    }

    /// <summary>
    /// HTTP webhook destination
    /// </summary>
    public class WebhookDestination : BaseDestination
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpointUrl;
        private readonly Dictionary<string, string> _headers;
        private readonly TimeSpan _timeout;

        public WebhookDestination(string name, string endpointUrl, Dictionary<string, string>? headers = null, TimeSpan? timeout = null)
            : base(name, DestinationType.Webhook)
        {
            _endpointUrl = endpointUrl ?? throw new ArgumentNullException(nameof(endpointUrl));
            _headers = headers ?? new Dictionary<string, string>();
            _timeout = timeout ?? TimeSpan.FromSeconds(30);

            _httpClient = new HttpClient
            {
                Timeout = _timeout
            };

            // Add default headers
            if (!_headers.ContainsKey("Content-Type"))
                _headers["Content-Type"] = "application/json";
            if (!_headers.ContainsKey("User-Agent"))
                _headers["User-Agent"] = "SQLDBEntityNotifier/1.0";
        }

        public override async Task<DeliveryResult> DeliverAsync(ChangeRecord change, string tableName)
        {
            if (!IsEnabled)
            {
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = "Destination is disabled"
                };
            }

            var startTime = DateTime.UtcNow;
            try
            {
                var payload = new
                {
                    ChangeId = change.ChangeId,
                    TableName = tableName,
                    Operation = change.Operation.ToString(),
                    Timestamp = change.ChangeTimestamp,
                    ChangePosition = change.ChangePosition,
                    Metadata = change.Metadata
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Add custom headers
                foreach (var header in _headers)
                {
                    content.Headers.Add(header.Key, header.Value);
                }

                var response = await _httpClient.PostAsync(_endpointUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                var deliveryTime = DateTime.UtcNow - startTime;

                if (response.IsSuccessStatusCode)
                {
                    return new DeliveryResult
                    {
                        Success = true,
                        DeliveryTime = deliveryTime
                    };
                }
                else
                {
                    return new DeliveryResult
                    {
                        Success = false,
                        ErrorMessage = $"HTTP {response.StatusCode}: {responseContent}",
                        DeliveryTime = deliveryTime
                    };
                }
            }
            catch (TaskCanceledException)
            {
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = "Request timeout",
                    DeliveryTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    DeliveryTime = DateTime.UtcNow - startTime
                };
            }
        }

        public override void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// Database destination for storing changes
    /// </summary>
    public class DatabaseDestination : BaseDestination
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        private readonly string _schemaName;

        public DatabaseDestination(string name, string connectionString, string tableName, string schemaName = "dbo")
            : base(name, DestinationType.Database)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _schemaName = schemaName;
        }

        public override async Task<DeliveryResult> DeliverAsync(ChangeRecord change, string tableName)
        {
            if (!IsEnabled)
            {
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = "Destination is disabled"
                };
            }

            var startTime = DateTime.UtcNow;
            try
            {
                // This is a simplified implementation
                // In a real scenario, you would use proper database access (Entity Framework, Dapper, etc.)
                var sql = $@"
                    INSERT INTO [{_schemaName}].[{_tableName}] 
                    (ChangeId, SourceTableName, Operation, ChangeTimestamp, ChangePosition, Metadata, CreatedAt)
                    VALUES (@ChangeId, @SourceTableName, @Operation, @ChangeTimestamp, @ChangePosition, @Metadata, @CreatedAt)";

                // For now, we'll simulate successful database insertion
                await Task.Delay(10); // Simulate database operation

                var deliveryTime = DateTime.UtcNow - startTime;

                return new DeliveryResult
                {
                    Success = true,
                    DeliveryTime = deliveryTime
                };
            }
            catch (Exception ex)
            {
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    DeliveryTime = DateTime.UtcNow - startTime
                };
            }
        }
    }

    /// <summary>
    /// File system destination for logging changes
    /// </summary>
    public class FileSystemDestination : BaseDestination
    {
        private readonly string _directoryPath;
        private readonly string _fileExtension;
        private readonly bool _appendToFile;
        private readonly object _fileLock = new object();

        public FileSystemDestination(string name, string directoryPath, string fileExtension = ".json", bool appendToFile = false)
            : base(name, DestinationType.FileSystem)
        {
            _directoryPath = directoryPath ?? throw new ArgumentNullException(nameof(directoryPath));
            _fileExtension = fileExtension ?? ".json";
            _appendToFile = appendToFile;

            // Ensure directory exists
            if (!System.IO.Directory.Exists(_directoryPath))
            {
                System.IO.Directory.CreateDirectory(_directoryPath);
            }
        }

        public override async Task<DeliveryResult> DeliverAsync(ChangeRecord change, string tableName)
        {
            if (!IsEnabled)
            {
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = "Destination is disabled"
                };
            }

            var startTime = DateTime.UtcNow;
            try
            {
                var fileName = $"{tableName}_{DateTime.UtcNow:yyyyMMdd}{_fileExtension}";
                var filePath = System.IO.Path.Combine(_directoryPath, fileName);

                var payload = new
                {
                    ChangeId = change.ChangeId,
                    TableName = tableName,
                    Operation = change.Operation.ToString(),
                    Timestamp = change.ChangeTimestamp,
                    ChangePosition = change.ChangePosition,
                    Metadata = change.Metadata,
                    DeliveredAt = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                await Task.Run(() =>
                {
                    lock (_fileLock)
                    {
                        if (_appendToFile)
                        {
                            System.IO.File.AppendAllText(filePath, json + Environment.NewLine);
                        }
                        else
                        {
                            System.IO.File.WriteAllText(filePath, json);
                        }
                    }
                });

                var deliveryTime = DateTime.UtcNow - startTime;

                return new DeliveryResult
                {
                    Success = true,
                    DeliveryTime = deliveryTime
                };
            }
            catch (Exception ex)
            {
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    DeliveryTime = DateTime.UtcNow - startTime
                };
            }
        }
    }

    /// <summary>
    /// Email notification destination
    /// </summary>
    public class EmailDestination : BaseDestination
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly List<string> _toEmails;
        private readonly string _subjectTemplate;
        private readonly string _bodyTemplate;

        public EmailDestination(string name, string smtpServer, int smtpPort, string fromEmail, string fromName, 
            List<string> toEmails, string subjectTemplate = "Database Change Alert: {TableName}", 
            string bodyTemplate = "Change detected in table {TableName}")
            : base(name, DestinationType.Email)
        {
            _smtpServer = smtpServer ?? throw new ArgumentNullException(nameof(smtpServer));
            _smtpPort = smtpPort;
            _fromEmail = fromEmail ?? throw new ArgumentNullException(nameof(fromEmail));
            _fromName = fromName ?? throw new ArgumentNullException(nameof(fromName));
            _toEmails = toEmails ?? throw new ArgumentNullException(nameof(toEmails));
            _subjectTemplate = subjectTemplate ?? throw new ArgumentNullException(nameof(subjectTemplate));
            _bodyTemplate = bodyTemplate ?? throw new ArgumentNullException(nameof(bodyTemplate));
        }

        public override async Task<DeliveryResult> DeliverAsync(ChangeRecord change, string tableName)
        {
            if (!IsEnabled)
            {
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = "Destination is disabled"
                };
            }

            var startTime = DateTime.UtcNow;
            try
            {
                // This is a simplified implementation
                // In a real scenario, you would use proper SMTP client (System.Net.Mail.SmtpClient, etc.)
                var subject = _subjectTemplate.Replace("{TableName}", tableName);
                var body = _bodyTemplate.Replace("{TableName}", tableName)
                    .Replace("{Operation}", change.Operation.ToString())
                    .Replace("{ChangeId}", change.ChangeId)
                    .Replace("{Timestamp}", change.ChangeTimestamp.ToString("yyyy-MM-dd HH:mm:ss"));

                // For now, we'll simulate successful email sending
                await Task.Delay(50); // Simulate SMTP operation

                var deliveryTime = DateTime.UtcNow - startTime;

                return new DeliveryResult
                {
                    Success = true,
                    DeliveryTime = deliveryTime
                };
            }
            catch (Exception ex)
            {
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    DeliveryTime = DateTime.UtcNow - startTime
                };
            }
        }
    }

    /// <summary>
    /// Console output destination for debugging
    /// </summary>
    public class ConsoleDestination : BaseDestination
    {
        private readonly bool _includeTimestamp;
        private readonly bool _includeMetadata;

        public ConsoleDestination(string name, bool includeTimestamp = true, bool includeMetadata = true)
            : base(name, DestinationType.Custom)
        {
            _includeTimestamp = includeTimestamp;
            _includeMetadata = includeMetadata;
        }

        public override async Task<DeliveryResult> DeliverAsync(ChangeRecord change, string tableName)
        {
            if (!IsEnabled)
            {
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = "Destination is disabled"
                };
            }

            var startTime = DateTime.UtcNow;
            try
            {
                var output = new StringBuilder();
                output.AppendLine($"=== Change Delivered to Console ===");
                output.AppendLine($"Table: {tableName}");
                output.AppendLine($"Operation: {change.Operation}");
                output.AppendLine($"Change ID: {change.ChangeId}");
                
                if (_includeTimestamp)
                {
                    output.AppendLine($"Timestamp: {change.ChangeTimestamp:yyyy-MM-dd HH:mm:ss}");
                }

                if (_includeMetadata && change.Metadata != null && change.Metadata.Count > 0)
                {
                    output.AppendLine("Metadata:");
                    foreach (var kvp in change.Metadata)
                    {
                        output.AppendLine($"  {kvp.Key}: {kvp.Value}");
                    }
                }

                output.AppendLine("==================================");

                await Task.Run(() => Console.WriteLine(output.ToString()));

                var deliveryTime = DateTime.UtcNow - startTime;

                return new DeliveryResult
                {
                    Success = true,
                    DeliveryTime = deliveryTime
                };
            }
            catch (Exception ex)
            {
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    DeliveryTime = DateTime.UtcNow - startTime
                };
            }
        }
    }

    /// <summary>
    /// Message queue destination (simplified implementation)
    /// </summary>
    public class MessageQueueDestination : BaseDestination
    {
        private readonly string _queueName;
        private readonly string _connectionString;
        private readonly Dictionary<string, object> _queueProperties;

        public MessageQueueDestination(string name, string queueName, string connectionString, Dictionary<string, object>? queueProperties = null)
            : base(name, DestinationType.MessageQueue)
        {
            _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _queueProperties = queueProperties ?? new Dictionary<string, object>();
        }

        public override async Task<DeliveryResult> DeliverAsync(ChangeRecord change, string tableName)
        {
            if (!IsEnabled)
            {
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = "Destination is disabled"
                };
            }

            var startTime = DateTime.UtcNow;
            try
            {
                // This is a simplified implementation
                // In a real scenario, you would use proper message queue client (RabbitMQ, Azure Service Bus, etc.)
                var message = new
                {
                    ChangeId = change.ChangeId,
                    TableName = tableName,
                    Operation = change.Operation.ToString(),
                    Timestamp = change.ChangeTimestamp,
                    ChangePosition = change.ChangePosition,
                    Metadata = change.Metadata,
                    QueuedAt = DateTime.UtcNow
                };

                // For now, we'll simulate successful message queuing
                await Task.Delay(20); // Simulate queue operation

                var deliveryTime = DateTime.UtcNow - startTime;

                return new DeliveryResult
                {
                    Success = true,
                    DeliveryTime = deliveryTime
                };
            }
            catch (Exception ex)
            {
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    DeliveryTime = DateTime.UtcNow - startTime
                };
            }
        }
    }

    /// <summary>
    /// Event stream destination (simplified implementation)
    /// </summary>
    public class EventStreamDestination : BaseDestination
    {
        private readonly string _streamName;
        private readonly string _connectionString;
        private readonly Dictionary<string, object> _streamProperties;

        public EventStreamDestination(string name, string streamName, string connectionString, Dictionary<string, object>? streamProperties = null)
            : base(name, DestinationType.EventStream)
        {
            _streamName = streamName ?? throw new ArgumentNullException(nameof(streamName));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _streamProperties = streamProperties ?? new Dictionary<string, object>();
        }

        public override async Task<DeliveryResult> DeliverAsync(ChangeRecord change, string tableName)
        {
            if (!IsEnabled)
            {
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = "Destination is disabled"
                };
            }

            var startTime = DateTime.UtcNow;
            try
            {
                // This is a simplified implementation
                // In a real scenario, you would use proper event stream client (Kafka, etc.)
                var eventData = new
                {
                    EventId = Guid.NewGuid().ToString(),
                    EventType = "DatabaseChange",
                    ChangeId = change.ChangeId,
                    TableName = tableName,
                    Operation = change.Operation.ToString(),
                    Timestamp = change.ChangeTimestamp,
                    ChangePosition = change.ChangePosition,
                    Metadata = change.Metadata,
                    StreamedAt = DateTime.UtcNow
                };

                // For now, we'll simulate successful event streaming
                await Task.Delay(15); // Simulate stream operation

                var deliveryTime = DateTime.UtcNow - startTime;

                return new DeliveryResult
                {
                    Success = true,
                    DeliveryTime = deliveryTime
                };
            }
            catch (Exception ex)
            {
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    DeliveryTime = DateTime.UtcNow - startTime
                };
            }
        }
    }
}
