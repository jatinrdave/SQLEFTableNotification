using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqlDbEntityNotifier.Core.Extensions;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using SqlDbEntityNotifier.Core.BulkOperations;
using SqlDbEntityNotifier.Core.Filters;
using SqlDbEntityNotifier.Adapters.Sqlite;
using SqlDbEntityNotifier.Publisher.Kafka;

namespace BulkOperationsSample;

/// <summary>
/// Sample application demonstrating bulk operation detection and filtering.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        using var scope = host.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var bulkOperationDetector = scope.ServiceProvider.GetRequiredService<BulkOperationDetector>();
        var bulkFilterEngine = scope.ServiceProvider.GetRequiredService<BulkOperationFilterEngine>();
        var entityNotifier = scope.ServiceProvider.GetRequiredService<IEntityNotifier>();

        logger.LogInformation("Starting Bulk Operations Sample");

        try
        {
            // Start the entity notifier
            await entityNotifier.StartAsync();

            // Subscribe to bulk operations with filtering
            var subscription = await entityNotifier.SubscribeAsync<BulkOperationEvent>(
                new SubscriptionOptions
                {
                    TableName = "__schema_changes__", // Special table for bulk operations
                    IncludeMetadata = true
                },
                async (changeEvent, bulkEvent, cancellationToken) =>
                {
                    logger.LogInformation("Received bulk operation: {Operation} on {Table} affecting {Count} rows", 
                        bulkEvent.Operation, bulkEvent.Table, bulkEvent.AffectedRowCount);

                    // Apply additional filtering
                    var highImpactFilter = bulkFilterEngine.CreateHighImpactFilter(minRowCount: 100, minDurationMs: 1000);
                    if (highImpactFilter(bulkEvent))
                    {
                        logger.LogWarning("High-impact bulk operation detected: {Operation} on {Table} affecting {Count} rows in {Duration}ms", 
                            bulkEvent.Operation, bulkEvent.Table, bulkEvent.AffectedRowCount, bulkEvent.ExecutionDurationMs);
                    }

                    // Get statistics
                    var stats = bulkFilterEngine.GetStatistics(new[] { bulkEvent });
                    logger.LogInformation("Bulk operation statistics: Total={Total}, AvgRows={AvgRows}, MaxDuration={MaxDuration}ms", 
                        stats.TotalOperations, stats.AverageAffectedRows, stats.MaxExecutionDuration);
                });

            logger.LogInformation("Bulk operations monitoring started. Press any key to stop...");
            Console.ReadKey();

            // Stop the entity notifier
            await entityNotifier.StopAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in bulk operations sample");
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Add SQLDBEntityNotifier with bulk operation detection
                services.AddSqlDbEntityNotifier(options =>
                {
                    options.EnableBulkOperationDetection = true;
                    options.BulkOperationMinRowCount = 2;
                    options.BulkOperationBatchTimeoutSeconds = 5;
                });

                // Configure bulk operation detection
                services.Configure<BulkOperationDetectorOptions>(options =>
                {
                    options.Enabled = true;
                    options.MinRowCount = 2;
                    options.MaxBatchSize = 1000;
                    options.BatchTimeoutSeconds = 5;
                    options.MaxSampleSize = 10;
                    options.IncludeSampleData = true;
                    options.GroupByTransaction = true;
                    
                    // Filter configuration
                    options.ExcludedTables.Add("temp_table");
                    options.ExcludedOperations.Add(BulkOperationType.BULK_DELETE); // Exclude bulk deletes
                    
                    // Performance monitoring
                    options.PerformanceMonitoring.Enabled = true;
                    options.PerformanceMonitoring.SlowOperationThresholdMs = 1000;
                    options.PerformanceMonitoring.LargeOperationThreshold = 10000;
                    options.PerformanceMonitoring.AlertOnSlowOperations = true;
                    options.PerformanceMonitoring.AlertOnLargeOperations = true;
                });

                // Add SQLite adapter with bulk operation detection
                services.AddDbAdapter<SqliteAdapter>();
                services.Configure<SqliteAdapterOptions>(options =>
                {
                    options.Source = "sqlite-sample";
                    options.FilePath = "sample.db";
                    options.ChangeTable = "change_log";
                    options.IncludeBefore = true;
                    options.IncludeAfter = true;
                    options.PollingIntervalSeconds = 1;
                });

                // Add Kafka publisher
                services.AddChangePublisher<KafkaChangePublisher>();
                services.Configure<KafkaPublisherOptions>(options =>
                {
                    options.BootstrapServers = "localhost:9092";
                    options.Topic = "bulk-operations";
                    options.EnableIdempotence = true;
                    options.Acks = "all";
                    options.RetryBackoffMs = 1000;
                    options.MaxRetries = 3;
                });

                // Add offset store (in-memory for demo)
                services.AddSingleton<IOffsetStore, InMemoryOffsetStore>();
            });

    /// <summary>
    /// In-memory offset store for demonstration purposes.
    /// </summary>
    public class InMemoryOffsetStore : IOffsetStore
    {
        private readonly Dictionary<string, string> _offsets = new();

        public Task<string?> GetOffsetAsync(string source, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_offsets.TryGetValue(source, out var offset) ? offset : null);
        }

        public Task SetOffsetAsync(string source, string offset, CancellationToken cancellationToken = default)
        {
            _offsets[source] = offset;
            return Task.CompletedTask;
        }
    }
}