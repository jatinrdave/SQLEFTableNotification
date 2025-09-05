using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLDBEntityNotifier.Interfaces;

namespace SQLDBEntityNotifier.Models
{
    /// <summary>
    /// Advanced change routing engine that routes changes to different destinations based on rules
    /// </summary>
    public class ChangeRoutingEngine : IDisposable
    {
        private readonly List<IRoutingRule> _routingRules = new();
        private readonly List<IDestination> _destinations = new();
        private readonly Dictionary<string, IDestination> _destinationMap = new();
        private readonly ChangeRoutingMetrics _metrics = new();
        private bool _disposed = false;

        /// <summary>
        /// Event raised when a change is routed
        /// </summary>
        public event EventHandler<ChangeRoutedEventArgs>? OnChangeRouted;

        /// <summary>
        /// Event raised when routing fails
        /// </summary>
        public event EventHandler<RoutingFailedEventArgs>? OnRoutingFailed;

        /// <summary>
        /// Event raised when routing metrics are updated
        /// </summary>
        public event EventHandler<RoutingMetricsUpdatedEventArgs>? OnRoutingMetricsUpdated;

        /// <summary>
        /// Gets the routing rules
        /// </summary>
        public IReadOnlyList<IRoutingRule> RoutingRules => _routingRules.AsReadOnly();

        /// <summary>
        /// Gets the destinations
        /// </summary>
        public IReadOnlyList<IDestination> Destinations => _destinations.AsReadOnly();

        /// <summary>
        /// Gets the routing metrics
        /// </summary>
        public ChangeRoutingMetrics Metrics => _metrics;

        /// <summary>
        /// Adds a routing rule
        /// </summary>
        public ChangeRoutingEngine AddRoutingRule(IRoutingRule rule)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeRoutingEngine));
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            _routingRules.Add(rule);
            return this;
        }

        /// <summary>
        /// Adds a destination
        /// </summary>
        public ChangeRoutingEngine AddDestination(IDestination destination)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeRoutingEngine));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            _destinations.Add(destination);
            _destinationMap[destination.Name] = destination;
            return this;
        }

        /// <summary>
        /// Routes a change to appropriate destinations based on routing rules
        /// </summary>
        public async Task<RoutingResult> RouteChangeAsync(ChangeRecord change, string tableName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeRoutingEngine));

            var startTime = DateTime.UtcNow;
            var result = new RoutingResult
            {
                ChangeId = change.ChangeId,
                TableName = tableName,
                Timestamp = startTime,
                Success = true
            };

            try
            {
                var applicableRules = _routingRules.Where(r => r.ShouldApply(change, tableName)).ToList();
                var routedDestinations = new List<string>();

                foreach (var rule in applicableRules)
                {
                    var destinations = rule.GetDestinations(change, tableName);
                    foreach (var destinationName in destinations)
                    {
                        if (_destinationMap.TryGetValue(destinationName, out var destination))
                        {
                            try
                            {
                                var deliveryResult = await destination.DeliverAsync(change, tableName);
                                if (deliveryResult.Success)
                                {
                                    routedDestinations.Add(destinationName);
                                    _metrics.RecordSuccessfulRouting(destinationName, startTime);
                                }
                                else
                                {
                                    result.Errors.Add($"Failed to deliver to {destinationName}: {deliveryResult.ErrorMessage}");
                                    _metrics.RecordFailedRouting(destinationName, startTime);
                                }
                            }
                            catch (Exception ex)
                            {
                                result.Errors.Add($"Exception delivering to {destinationName}: {ex.Message}");
                                _metrics.RecordFailedRouting(destinationName, startTime);
                            }
                        }
                        else
                        {
                            result.Errors.Add($"Destination not found: {destinationName}");
                        }
                    }
                }

                result.RoutedDestinations = routedDestinations;
                result.ProcessingTime = DateTime.UtcNow - startTime;

                if (routedDestinations.Any())
                {
                    OnChangeRouted?.Invoke(this, new ChangeRoutedEventArgs
                    {
                        Change = change,
                        TableName = tableName,
                        RoutedDestinations = routedDestinations,
                        ProcessingTime = result.ProcessingTime
                    });
                }

                if (result.Errors.Any())
                {
                    OnRoutingFailed?.Invoke(this, new RoutingFailedEventArgs
                    {
                        Change = change,
                        TableName = tableName,
                        Errors = result.Errors
                    });
                }

                _metrics.RecordRoutingCompleted(result);
                OnRoutingMetricsUpdated?.Invoke(this, new RoutingMetricsUpdatedEventArgs { Metrics = _metrics });

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Routing failed: {ex.Message}");
                _metrics.RecordRoutingError(ex.Message, startTime);
                return result;
            }
        }

        /// <summary>
        /// Routes multiple changes in batch
        /// </summary>
        public async Task<List<RoutingResult>> RouteChangesAsync(List<ChangeRecord> changes, string tableName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeRoutingEngine));
            if (changes == null) return new List<RoutingResult>();
            if (string.IsNullOrEmpty(tableName)) return new List<RoutingResult>();
            
            var results = new List<RoutingResult>();
            var tasks = changes.Select(change => RouteChangeAsync(change, tableName));
            var completedResults = await Task.WhenAll(tasks);
            results.AddRange(completedResults);
            return results;
        }

        /// <summary>
        /// Gets routing statistics for a specific destination
        /// </summary>
        public DestinationRoutingStats GetDestinationStats(string destinationName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeRoutingEngine));
            return _metrics.GetDestinationStats(destinationName);
        }

        /// <summary>
        /// Gets overall routing statistics
        /// </summary>
        public OverallRoutingStats GetOverallStats()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeRoutingEngine));
            return _metrics.GetOverallStats();
        }

        /// <summary>
        /// Clears routing metrics
        /// </summary>
        public void ClearMetrics()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChangeRoutingEngine));
            _metrics.Clear();
        }

        /// <summary>
        /// Disposes the routing engine
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the routing engine
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                foreach (var destination in _destinations)
                {
                    destination?.Dispose();
                }
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Interface for routing rules
    /// </summary>
    public interface IRoutingRule
    {
        /// <summary>
        /// Gets the rule name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the rule priority (higher numbers = higher priority)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Determines if this rule should apply to the given change
        /// </summary>
        bool ShouldApply(ChangeRecord change, string tableName);

        /// <summary>
        /// Gets the destinations for this rule
        /// </summary>
        List<string> GetDestinations(ChangeRecord change, string tableName);
    }

    /// <summary>
    /// Interface for change destinations
    /// </summary>
    public interface IDestination : IDisposable
    {
        /// <summary>
        /// Gets the destination name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the destination type
        /// </summary>
        DestinationType Type { get; }

        /// <summary>
        /// Gets whether the destination is enabled
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Delivers a change to this destination
        /// </summary>
        Task<DeliveryResult> DeliverAsync(ChangeRecord change, string tableName);
    }

    /// <summary>
    /// Result of routing a change
    /// </summary>
    public class RoutingResult
    {
        public string ChangeId { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
        public List<string> RoutedDestinations { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public TimeSpan ProcessingTime { get; set; }
    }

    /// <summary>
    /// Result of delivering a change to a destination
    /// </summary>
    public class DeliveryResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan DeliveryTime { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event arguments for change routed events
    /// </summary>
    public class ChangeRoutedEventArgs : EventArgs
    {
        public ChangeRecord Change { get; set; } = new();
        public string TableName { get; set; } = string.Empty;
        public List<string> RoutedDestinations { get; set; } = new();
        public TimeSpan ProcessingTime { get; set; }
    }

    /// <summary>
    /// Event arguments for routing failed events
    /// </summary>
    public class RoutingFailedEventArgs : EventArgs
    {
        public ChangeRecord Change { get; set; } = new();
        public string TableName { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Event arguments for routing metrics updated events
    /// </summary>
    public class RoutingMetricsUpdatedEventArgs : EventArgs
    {
        public ChangeRoutingMetrics Metrics { get; set; } = new();
    }

    /// <summary>
    /// Destination types
    /// </summary>
    public enum DestinationType
    {
        /// <summary>
        /// HTTP webhook endpoint
        /// </summary>
        Webhook,

        /// <summary>
        /// Message queue (RabbitMQ, Azure Service Bus, etc.)
        /// </summary>
        MessageQueue,

        /// <summary>
        /// Event stream (Kafka, etc.)
        /// </summary>
        EventStream,

        /// <summary>
        /// Database table
        /// </summary>
        Database,

        /// <summary>
        /// File system
        /// </summary>
        FileSystem,

        /// <summary>
        /// Email notification
        /// </summary>
        Email,

        /// <summary>
        /// SMS notification
        /// </summary>
        Sms,

        /// <summary>
        /// Custom destination
        /// </summary>
        Custom
    }

    /// <summary>
    /// Change routing metrics
    /// </summary>
    public class ChangeRoutingMetrics
    {
        private readonly Dictionary<string, DestinationRoutingStats> _destinationStats = new();
        private readonly object _lock = new object();
        private int _totalChangesRouted;
        private int _totalRoutingErrors;
        private TimeSpan _totalProcessingTime;

        /// <summary>
        /// Gets the total changes routed
        /// </summary>
        public int TotalChangesRouted => _totalChangesRouted;

        /// <summary>
        /// Gets the total routing errors
        /// </summary>
        public int TotalRoutingErrors => _totalRoutingErrors;

        /// <summary>
        /// Gets the total processing time
        /// </summary>
        public TimeSpan TotalProcessingTime => _totalProcessingTime;

        /// <summary>
        /// Gets the average processing time
        /// </summary>
        public TimeSpan AverageProcessingTime => _totalChangesRouted > 0 
            ? TimeSpan.FromTicks(_totalProcessingTime.Ticks / _totalChangesRouted) 
            : TimeSpan.Zero;

        /// <summary>
        /// Records a successful routing
        /// </summary>
        public void RecordSuccessfulRouting(string destinationName, DateTime timestamp)
        {
            lock (_lock)
            {
                if (!_destinationStats.ContainsKey(destinationName))
                {
                    _destinationStats[destinationName] = new DestinationRoutingStats(destinationName);
                }
                _destinationStats[destinationName].RecordSuccess(timestamp);
                _totalChangesRouted++;
            }
        }

        /// <summary>
        /// Records a failed routing
        /// </summary>
        public void RecordFailedRouting(string destinationName, DateTime timestamp)
        {
            lock (_lock)
            {
                if (!_destinationStats.ContainsKey(destinationName))
                {
                    _destinationStats[destinationName] = new DestinationRoutingStats(destinationName);
                }
                _destinationStats[destinationName].RecordFailure(timestamp);
                _totalChangesRouted++;
            }
        }

        /// <summary>
        /// Records a routing error
        /// </summary>
        public void RecordRoutingError(string errorMessage, DateTime timestamp)
        {
            lock (_lock)
            {
                _totalRoutingErrors++;
            }
        }

        /// <summary>
        /// Records routing completion
        /// </summary>
        public void RecordRoutingCompleted(RoutingResult result)
        {
            lock (_lock)
            {
                _totalProcessingTime += result.ProcessingTime;
            }
        }

        /// <summary>
        /// Gets destination statistics
        /// </summary>
        public DestinationRoutingStats GetDestinationStats(string destinationName)
        {
            lock (_lock)
            {
                return _destinationStats.TryGetValue(destinationName, out var stats) ? stats : new DestinationRoutingStats(destinationName);
            }
        }

        /// <summary>
        /// Gets overall routing statistics
        /// </summary>
        public OverallRoutingStats GetOverallStats()
        {
            lock (_lock)
            {
                return new OverallRoutingStats
                {
                    TotalChangesRouted = _totalChangesRouted,
                    TotalRoutingErrors = _totalRoutingErrors,
                    TotalProcessingTime = _totalProcessingTime,
                    AverageProcessingTime = AverageProcessingTime,
                    DestinationCount = _destinationStats.Count,
                    SuccessRate = _totalChangesRouted > 0 ? (double)(_totalChangesRouted - _totalRoutingErrors) / _totalChangesRouted : 0
                };
            }
        }

        /// <summary>
        /// Clears all metrics
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _destinationStats.Clear();
                _totalChangesRouted = 0;
                _totalRoutingErrors = 0;
                _totalProcessingTime = TimeSpan.Zero;
            }
        }
    }

    /// <summary>
    /// Destination routing statistics
    /// </summary>
    public class DestinationRoutingStats
    {
        public string DestinationName { get; }
        public int TotalDeliveries { get; private set; }
        public int SuccessfulDeliveries { get; private set; }
        public int FailedDeliveries { get; private set; }
        public TimeSpan TotalDeliveryTime { get; private set; }
        public DateTime LastSuccessfulDelivery { get; private set; }
        public DateTime LastFailedDelivery { get; private set; }
        public DateTime LastDeliveryAttempt { get; private set; }

        public DestinationRoutingStats(string destinationName)
        {
            DestinationName = destinationName;
        }

        public void RecordSuccess(DateTime timestamp)
        {
            TotalDeliveries++;
            SuccessfulDeliveries++;
            LastSuccessfulDelivery = timestamp;
            LastDeliveryAttempt = timestamp;
        }

        public void RecordFailure(DateTime timestamp)
        {
            TotalDeliveries++;
            FailedDeliveries++;
            LastFailedDelivery = timestamp;
            LastDeliveryAttempt = timestamp;
        }

        public void RecordDeliveryTime(TimeSpan deliveryTime)
        {
            TotalDeliveryTime += deliveryTime;
        }

        public double SuccessRate => TotalDeliveries > 0 ? (double)SuccessfulDeliveries / TotalDeliveries : 0;
        public TimeSpan AverageDeliveryTime => TotalDeliveries > 0 ? TimeSpan.FromTicks(TotalDeliveryTime.Ticks / TotalDeliveries) : TimeSpan.Zero;
    }

    /// <summary>
    /// Overall routing statistics
    /// </summary>
    public class OverallRoutingStats
    {
        public int TotalChangesRouted { get; set; }
        public int TotalRoutingErrors { get; set; }
        public TimeSpan TotalProcessingTime { get; set; }
        public TimeSpan AverageProcessingTime { get; set; }
        public int DestinationCount { get; set; }
        public double SuccessRate { get; set; }
    }
}
