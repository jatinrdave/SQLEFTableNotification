using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SQLDBEntityNotifier.Interfaces;

namespace SQLDBEntityNotifier.Models
{
    /// <summary>
    /// Manages change context information and provides context propagation across CDC operations
    /// </summary>
    public class ChangeContextManager : IDisposable
    {
        private readonly AsyncLocal<EnhancedChangeContext?> _currentContext = new();
        private readonly List<IContextEnricher> _contextEnrichers = new();
        private readonly List<IContextValidator> _contextValidators = new();
        private readonly List<IContextProcessor> _contextProcessors = new();
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        /// <summary>
        /// Gets the current change context for the current execution context
        /// </summary>
        public EnhancedChangeContext? CurrentContext => _currentContext.Value;

        /// <summary>
        /// Gets the context enrichers
        /// </summary>
        public IReadOnlyList<IContextEnricher> ContextEnrichers => _contextEnrichers.AsReadOnly();

        /// <summary>
        /// Gets the context validators
        /// </summary>
        public IReadOnlyList<IContextValidator> ContextValidators => _contextValidators.AsReadOnly();

        /// <summary>
        /// Gets the context processors
        /// </summary>
        public IReadOnlyList<IContextProcessor> ContextProcessors => _contextProcessors.AsReadOnly();

        /// <summary>
        /// Gets or sets whether to automatically enrich context
        /// </summary>
        public bool AutoEnrichContext { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to validate context before processing
        /// </summary>
        public bool ValidateContext { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to propagate context to child operations
        /// </summary>
        public bool PropagateContext { get; set; } = true;

        /// <summary>
        /// Gets or sets the default context source
        /// </summary>
        public ChangeContext DefaultSource { get; set; } = ChangeContext.Application;

        /// <summary>
        /// Gets or sets the default priority
        /// </summary>
        public ChangePriority DefaultPriority { get; set; } = ChangePriority.Normal;

        /// <summary>
        /// Gets or sets the default confidence level
        /// </summary>
        public ChangeConfidence DefaultConfidence { get; set; } = ChangeConfidence.High;

        /// <summary>
        /// Event raised when a context is created
        /// </summary>
        public event EventHandler<ContextCreatedEventArgs>? ContextCreated;

        /// <summary>
        /// Event raised when a context is modified
        /// </summary>
        public event EventHandler<ContextModifiedEventArgs>? ContextModified;

        /// <summary>
        /// Event raised when a context is validated
        /// </summary>
        public event EventHandler<ContextValidatedEventArgs>? ContextValidated;

        /// <summary>
        /// Event raised when a context is processed
        /// </summary>
        public event EventHandler<ContextProcessedEventArgs>? ContextProcessed;

        /// <summary>
        /// Creates a new change context for the current execution context
        /// </summary>
        public EnhancedChangeContext CreateContext(string? correlationId = null)
        {
            var context = new EnhancedChangeContext
            {
                ChangeId = GenerateChangeId(),
                Source = DefaultSource,
                Priority = DefaultPriority,
                Confidence = DefaultConfidence,
                DetectedAt = DateTime.UtcNow,
                CorrelationId = correlationId ?? GenerateCorrelationId(),
                Environment = GetEnvironment(),
                ApplicationName = GetApplicationName(),
                ApplicationVersion = GetApplicationVersion(),
                HostName = GetHostName(),
                HostIPAddress = GetHostIPAddress(),
                ProcessId = GetProcessId(),
                ThreadId = GetThreadId()
            };

            // Enrich context if auto-enrichment is enabled
            if (AutoEnrichContext)
            {
                EnrichContext(context);
            }

            // Validate context if validation is enabled
            if (ValidateContext)
            {
                ValidateContextAsync(context);
            }

            // Set as current context
            _currentContext.Value = context;

            // Raise context created event
            ContextCreated?.Invoke(this, new ContextCreatedEventArgs(context));

            return context;
        }

        /// <summary>
        /// Sets the current change context
        /// </summary>
        public void SetContext(EnhancedChangeContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var previousContext = _currentContext.Value;
            _currentContext.Value = context;

            // Raise context modified event
            ContextModified?.Invoke(this, new ContextModifiedEventArgs(previousContext, context));
        }

        /// <summary>
        /// Clears the current change context
        /// </summary>
        public void ClearContext()
        {
            var previousContext = _currentContext.Value;
            _currentContext.Value = null;

            if (previousContext != null)
            {
                ContextModified?.Invoke(this, new ContextModifiedEventArgs(previousContext, null));
            }
        }

        /// <summary>
        /// Enriches a change context with additional information
        /// </summary>
        public void EnrichContext(EnhancedChangeContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            foreach (var enricher in _contextEnrichers)
            {
                try
                {
                    enricher.Enrich(context);
                }
                catch (Exception ex)
                {
                    // Log enrichment error but don't fail the operation
                    // In a production environment, you might want to log this
                    System.Diagnostics.Debug.WriteLine($"Context enrichment failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Validates a change context
        /// </summary>
        public ContextValidationResult ValidateContextAsync(EnhancedChangeContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var result = new ContextValidationResult { IsValid = true };

            foreach (var validator in _contextValidators)
            {
                try
                {
                    var validationResult = validator.Validate(context);
                    if (!validationResult.IsValid)
                    {
                        result.IsValid = false;
                        result.Errors.AddRange(validationResult.Errors);
                        result.Warnings.AddRange(validationResult.Warnings);
                    }
                }
                catch (Exception ex)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Validation failed: {ex.Message}");
                }
            }

            // Raise context validated event
            ContextValidated?.Invoke(this, new ContextValidatedEventArgs(context, result));

            return result;
        }

        /// <summary>
        /// Processes a change context
        /// </summary>
        public async Task ProcessContextAsync(EnhancedChangeContext context, CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            foreach (var processor in _contextProcessors)
            {
                try
                {
                    await processor.ProcessAsync(context, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log processing error but don't fail the operation
                    System.Diagnostics.Debug.WriteLine($"Context processing failed: {ex.Message}");
                }
            }

            // Mark context as processed
            context.ProcessedAt = DateTime.UtcNow;

            // Raise context processed event
            ContextProcessed?.Invoke(this, new ContextProcessedEventArgs(context));
        }

        /// <summary>
        /// Adds a context enricher
        /// </summary>
        public ChangeContextManager AddEnricher(IContextEnricher enricher)
        {
            if (enricher == null)
                throw new ArgumentNullException(nameof(enricher));

            lock (_lockObject)
            {
                _contextEnrichers.Add(enricher);
            }
            return this;
        }

        /// <summary>
        /// Adds a context validator
        /// </summary>
        public ChangeContextManager AddValidator(IContextValidator validator)
        {
            if (validator == null)
                throw new ArgumentNullException(nameof(validator));

            lock (_lockObject)
            {
                _contextValidators.Add(validator);
            }
            return this;
        }

        /// <summary>
        /// Adds a context processor
        /// </summary>
        public ChangeContextManager AddProcessor(IContextProcessor processor)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor));

            lock (_lockObject)
            {
                _contextProcessors.Add(processor);
            }
            return this;
        }

        /// <summary>
        /// Removes a context enricher
        /// </summary>
        public ChangeContextManager RemoveEnricher(IContextEnricher enricher)
        {
            if (enricher == null)
                throw new ArgumentNullException(nameof(enricher));

            lock (_lockObject)
            {
                _contextEnrichers.Remove(enricher);
            }
            return this;
        }

        /// <summary>
        /// Removes a context validator
        /// </summary>
        public ChangeContextManager RemoveValidator(IContextValidator validator)
        {
            if (validator == null)
                throw new ArgumentNullException(nameof(validator));

            lock (_lockObject)
            {
                _contextValidators.Remove(validator);
            }
            return this;
        }

        /// <summary>
        /// Removes a context processor
        /// </summary>
        public ChangeContextManager RemoveProcessor(IContextProcessor processor)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor));

            lock (_lockObject)
            {
                _contextProcessors.Remove(processor);
            }
            return this;
        }

        /// <summary>
        /// Creates a child context from the current context
        /// </summary>
        public EnhancedChangeContext CreateChildContext(string? correlationId = null)
        {
            var currentContext = _currentContext.Value;
            if (currentContext == null)
                return CreateContext(correlationId);

            var childContext = currentContext.Clone();
            childContext.ParentChangeId = currentContext.ChangeId;
            childContext.CorrelationId = correlationId ?? currentContext.CorrelationId;
            childContext.SequenceNumber = GetNextSequenceNumber(currentContext.CorrelationId);

            return childContext;
        }

        /// <summary>
        /// Gets the next sequence number for a correlation ID
        /// </summary>
        private int GetNextSequenceNumber(string? correlationId)
        {
            // In a real implementation, you might want to use a more sophisticated
            // sequence number generation system, possibly with database backing
            return Environment.TickCount;
        }

        /// <summary>
        /// Generates a change ID
        /// </summary>
        private string GenerateChangeId()
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Generates a correlation ID
        /// </summary>
        private string GenerateCorrelationId()
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Gets the current environment
        /// </summary>
        private string GetEnvironment()
        {
            return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                   Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
                   Environment.GetEnvironmentVariable("ENVIRONMENT") ??
                   "Development";
        }

        /// <summary>
        /// Gets the application name
        /// </summary>
        private string GetApplicationName()
        {
            return Environment.GetEnvironmentVariable("APPLICATION_NAME") ??
                   System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ??
                   "Unknown";
        }

        /// <summary>
        /// Gets the application version
        /// </summary>
        private string GetApplicationVersion()
        {
            return Environment.GetEnvironmentVariable("APPLICATION_VERSION") ??
                   System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ??
                   "1.0.0";
        }

        /// <summary>
        /// Gets the host name
        /// </summary>
        private string GetHostName()
        {
            return Environment.GetEnvironmentVariable("HOSTNAME") ??
                   Environment.MachineName ??
                   "Unknown";
        }

        /// <summary>
        /// Gets the host IP address
        /// </summary>
        private string GetHostIPAddress()
        {
            // In a real implementation, you might want to get the actual IP address
            // This is a simplified version
            return Environment.GetEnvironmentVariable("HOST_IP_ADDRESS") ?? "127.0.0.1";
        }

        /// <summary>
        /// Gets the current process ID
        /// </summary>
        private int? GetProcessId()
        {
            try
            {
                return Environment.ProcessId;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the current thread ID
        /// </summary>
        private int? GetThreadId()
        {
            try
            {
                return Thread.CurrentThread.ManagedThreadId;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Disposes the context manager
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the context manager
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _currentContext.Value = null;
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Event arguments for context created events
    /// </summary>
    public class ContextCreatedEventArgs : EventArgs
    {
        public EnhancedChangeContext Context { get; }

        public ContextCreatedEventArgs(EnhancedChangeContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }
    }

    /// <summary>
    /// Event arguments for context modified events
    /// </summary>
    public class ContextModifiedEventArgs : EventArgs
    {
        public EnhancedChangeContext? PreviousContext { get; }
        public EnhancedChangeContext? CurrentContext { get; }

        public ContextModifiedEventArgs(EnhancedChangeContext? previousContext, EnhancedChangeContext? currentContext)
        {
            PreviousContext = previousContext;
            CurrentContext = currentContext;
        }
    }

    /// <summary>
    /// Event arguments for context validated events
    /// </summary>
    public class ContextValidatedEventArgs : EventArgs
    {
        public EnhancedChangeContext Context { get; }
        public ContextValidationResult ValidationResult { get; }

        public ContextValidatedEventArgs(EnhancedChangeContext context, ContextValidationResult validationResult)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            ValidationResult = validationResult ?? throw new ArgumentNullException(nameof(validationResult));
        }
    }

    /// <summary>
    /// Event arguments for context processed events
    /// </summary>
    public class ContextProcessedEventArgs : EventArgs
    {
        public EnhancedChangeContext Context { get; }

        public ContextProcessedEventArgs(EnhancedChangeContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }
    }

    /// <summary>
    /// Result of context validation
    /// </summary>
    public class ContextValidationResult
    {
        /// <summary>
        /// Gets or sets whether the context is valid
        /// </summary>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// Gets or sets the validation errors
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Gets or sets the validation warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Gets or sets additional validation metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Interface for context enrichers
    /// </summary>
    public interface IContextEnricher
    {
        /// <summary>
        /// Enriches a change context with additional information
        /// </summary>
        void Enrich(EnhancedChangeContext context);
    }

    /// <summary>
    /// Interface for context validators
    /// </summary>
    public interface IContextValidator
    {
        /// <summary>
        /// Validates a change context
        /// </summary>
        ContextValidationResult Validate(EnhancedChangeContext context);
    }

    /// <summary>
    /// Interface for context processors
    /// </summary>
    public interface IContextProcessor
    {
        /// <summary>
        /// Processes a change context
        /// </summary>
        Task ProcessAsync(EnhancedChangeContext context, CancellationToken cancellationToken = default);
    }
}
