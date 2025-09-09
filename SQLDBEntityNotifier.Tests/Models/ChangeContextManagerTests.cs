using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Interfaces;
using System.Threading;

namespace SQLDBEntityNotifier.Tests.Models
{
    public class ChangeContextManagerTests
    {
        private readonly ChangeContextManager _contextManager;

        public ChangeContextManagerTests()
        {
            _contextManager = new ChangeContextManager();
        }

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Assert
            Assert.NotNull(_contextManager);
            Assert.True(_contextManager.AutoEnrichContext);
            Assert.True(_contextManager.ValidateContext);
            Assert.True(_contextManager.PropagateContext);
            Assert.Equal(ChangeContext.Application, _contextManager.DefaultSource);
            Assert.Equal(ChangePriority.Normal, _contextManager.DefaultPriority);
            Assert.Equal(ChangeConfidence.High, _contextManager.DefaultConfidence);
        }

        [Fact]
        public void CreateContext_ShouldCreateValidContext()
        {
            // Act
            var context = _contextManager.CreateContext();

            // Assert
            Assert.NotNull(context);
            Assert.NotEmpty(context.ChangeId);
            Assert.Equal(ChangeContext.Application, context.Source);
            Assert.Equal(ChangePriority.Normal, context.Priority);
            Assert.Equal(ChangeConfidence.High, context.Confidence);
            Assert.Equal(DateTime.UtcNow.Date, context.DetectedAt.Date);
        }

        [Fact]
        public void CreateContext_WithCorrelationId_ShouldSetCorrelationId()
        {
            // Arrange
            var correlationId = "test-correlation-123";

            // Act
            var context = _contextManager.CreateContext(correlationId);

            // Assert
            Assert.Equal(correlationId, context.CorrelationId);
        }

        [Fact]
        public void SetContext_ShouldUpdateCurrentContext()
        {
            // Arrange
            var context = _contextManager.CreateContext();

            // Act
            _contextManager.SetContext(context);

            // Assert
            Assert.Same(context, _contextManager.CurrentContext);
        }

        [Fact]
        public void SetContext_WithNullContext_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _contextManager.SetContext(null!));
        }

        [Fact]
        public void ClearContext_ShouldRemoveCurrentContext()
        {
            // Arrange
            var context = _contextManager.CreateContext();
            _contextManager.SetContext(context);

            // Act
            _contextManager.ClearContext();

            // Assert
            Assert.Null(_contextManager.CurrentContext);
        }

        [Fact]
        public void CurrentContext_ShouldReturnCurrentContext()
        {
            // Arrange
            var context = _contextManager.CreateContext();

            // Act
            _contextManager.SetContext(context);

            // Assert
            Assert.Same(context, _contextManager.CurrentContext);
        }

        [Fact]
        public void EnrichContext_ShouldCallEnrichers()
        {
            // Arrange
            var mockEnricher = new MockContextEnricher();
            _contextManager.AddEnricher(mockEnricher);
            var context = _contextManager.CreateContext();

            // Act
            _contextManager.EnrichContext(context);

            // Assert
            Assert.True(mockEnricher.WasCalled);
        }

        [Fact]
        public void ValidateContextAsync_ShouldReturnValidationResult()
        {
            // Arrange
            var mockValidator = new MockContextValidator();
            _contextManager.AddValidator(mockValidator);
            var context = _contextManager.CreateContext();

            // Act
            var result = _contextManager.ValidateContextAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateContextAsync_WithInvalidContext_ShouldReturnInvalidResult()
        {
            // Arrange
            var mockValidator = new MockContextValidator { ShouldReturnValid = false };
            _contextManager.AddValidator(mockValidator);
            var context = _contextManager.CreateContext();

            // Act
            var result = _contextManager.ValidateContextAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateContextAsync_WithNullContext_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _contextManager.ValidateContextAsync(null!));
        }

        [Fact]
        public void ProcessContextAsync_ShouldCallProcessors()
        {
            // Arrange
            var mockProcessor = new MockContextProcessor();
            _contextManager.AddProcessor(mockProcessor);
            var context = _contextManager.CreateContext();

            // Act
            _contextManager.ProcessContextAsync(context).Wait();

            // Assert
            Assert.True(mockProcessor.WasCalled);
        }

        [Fact]
        public void ProcessContextAsync_WithNullContext_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _contextManager.ProcessContextAsync(null!));
        }

        [Fact]
        public void AddEnricher_ShouldAddEnricher()
        {
            // Arrange
            var enricher = new MockContextEnricher();

            // Act
            _contextManager.AddEnricher(enricher);

            // Assert
            Assert.Single(_contextManager.ContextEnrichers);
            Assert.Contains(enricher, _contextManager.ContextEnrichers);
        }

        [Fact]
        public void AddValidator_ShouldAddValidator()
        {
            // Arrange
            var validator = new MockContextValidator();

            // Act
            _contextManager.AddValidator(validator);

            // Assert
            Assert.Single(_contextManager.ContextValidators);
            Assert.Contains(validator, _contextManager.ContextValidators);
        }

        [Fact]
        public void AddProcessor_ShouldAddProcessor()
        {
            // Arrange
            var processor = new MockContextProcessor();

            // Act
            _contextManager.AddProcessor(processor);

            // Assert
            Assert.Single(_contextManager.ContextProcessors);
            Assert.Contains(processor, _contextManager.ContextProcessors);
        }

        [Fact]
        public void AddEnricher_WithNullEnricher_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _contextManager.AddEnricher(null!));
        }

        [Fact]
        public void AddValidator_WithNullValidator_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _contextManager.AddValidator(null!));
        }

        [Fact]
        public void AddProcessor_WithNullProcessor_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _contextManager.AddProcessor(null!));
        }

        [Fact]
        public void RemoveEnricher_ShouldRemoveEnricher()
        {
            // Arrange
            var enricher = new MockContextEnricher();
            _contextManager.AddEnricher(enricher);

            // Act
            _contextManager.RemoveEnricher(enricher);

            // Assert
            Assert.Empty(_contextManager.ContextEnrichers);
        }

        [Fact]
        public void RemoveValidator_ShouldRemoveValidator()
        {
            // Arrange
            var validator = new MockContextValidator();
            _contextManager.AddValidator(validator);

            // Act
            _contextManager.RemoveValidator(validator);

            // Assert
            Assert.Empty(_contextManager.ContextValidators);
        }

        [Fact]
        public void RemoveProcessor_ShouldRemoveProcessor()
        {
            // Arrange
            var processor = new MockContextProcessor();
            _contextManager.AddProcessor(processor);

            // Act
            _contextManager.RemoveProcessor(processor);

            // Assert
            Assert.Empty(_contextManager.ContextProcessors);
        }

        [Fact]
        public void RemoveEnricher_WithNullEnricher_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _contextManager.RemoveEnricher(null!));
        }

        [Fact]
        public void RemoveValidator_WithNullValidator_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _contextManager.RemoveValidator(null!));
        }

        [Fact]
        public void RemoveProcessor_WithNullProcessor_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _contextManager.RemoveProcessor(null!));
        }

        [Fact]
        public void ContextEnrichers_ShouldBeReadOnly()
        {
            // Act
            var enrichers = _contextManager.ContextEnrichers;

            // Assert
            Assert.True(enrichers is IReadOnlyList<IContextEnricher>);
        }

        [Fact]
        public void ContextValidators_ShouldBeReadOnly()
        {
            // Act
            var validators = _contextManager.ContextValidators;

            // Assert
            Assert.True(validators is IReadOnlyList<IContextValidator>);
        }

        [Fact]
        public void ContextProcessors_ShouldBeReadOnly()
        {
            // Act
            var processors = _contextManager.ContextProcessors;

            // Assert
            Assert.True(processors is IReadOnlyList<IContextProcessor>);
        }

        [Fact]
        public void AutoEnrichContext_ShouldBeConfigurable()
        {
            // Act
            _contextManager.AutoEnrichContext = false;

            // Assert
            Assert.False(_contextManager.AutoEnrichContext);
        }

        [Fact]
        public void ValidateContext_ShouldBeConfigurable()
        {
            // Act
            _contextManager.ValidateContext = false;

            // Assert
            Assert.False(_contextManager.ValidateContext);
        }

        [Fact]
        public void PropagateContext_ShouldBeConfigurable()
        {
            // Act
            _contextManager.PropagateContext = false;

            // Assert
            Assert.False(_contextManager.PropagateContext);
        }

        [Fact]
        public void DefaultSource_ShouldBeConfigurable()
        {
            // Act
            _contextManager.DefaultSource = ChangeContext.Unknown;

            // Assert
            Assert.Equal(ChangeContext.Unknown, _contextManager.DefaultSource);
        }

        [Fact]
        public void DefaultPriority_ShouldBeConfigurable()
        {
            // Act
            _contextManager.DefaultPriority = ChangePriority.High;

            // Assert
            Assert.Equal(ChangePriority.High, _contextManager.DefaultPriority);
        }

        [Fact]
        public void DefaultConfidence_ShouldBeConfigurable()
        {
            // Act
            _contextManager.DefaultConfidence = ChangeConfidence.Medium;

            // Assert
            Assert.Equal(ChangeConfidence.Medium, _contextManager.DefaultConfidence);
        }

        [Fact]
        public void CreateContext_ShouldGenerateUniqueIds()
        {
            // Act
            var context1 = _contextManager.CreateContext();
            var context2 = _contextManager.CreateContext();

            // Assert
            Assert.NotEqual(context1.ChangeId, context2.ChangeId);
            Assert.NotEqual(context1.CorrelationId, context2.CorrelationId);
        }

        [Fact]
        public void CreateContext_ShouldSetEnvironmentInfo()
        {
            // Act
            var context = _contextManager.CreateContext();

            // Assert
            Assert.NotNull(context.Environment);
            Assert.NotNull(context.ApplicationName);
            Assert.NotNull(context.HostName);
            Assert.NotNull(context.HostIPAddress);
        }

        [Fact]
        public void CreateContext_ShouldSetProcessInfo()
        {
            // Act
            var context = _contextManager.CreateContext();

            // Assert
            Assert.NotNull(context.ProcessId);
            Assert.NotNull(context.ThreadId);
        }

        [Fact]
        public void Clone_ShouldCreateIndependentCopy()
        {
            // Arrange
            var context = _contextManager.CreateContext();
            context.Tags.Add("test-tag");
            context.CustomMetadata = new Dictionary<string, object> { { "key", "value" } };

            // Act
            var clone = context.Clone();

            // Assert
            Assert.NotSame(context, clone);
            Assert.Equal(context.ChangeId, clone.ChangeId);
            Assert.Equal(context.Tags.Count, clone.Tags.Count);
            Assert.Equal(context.CustomMetadata.Count, clone.CustomMetadata.Count);
        }

        [Fact]
        public void Clone_ShouldNotAffectOriginal()
        {
            // Arrange
            var context = _contextManager.CreateContext();
            var originalTagCount = context.Tags.Count;

            // Act
            var clone = context.Clone();
            clone.Tags.Add("new-tag");

            // Assert
            Assert.Equal(originalTagCount, context.Tags.Count);
            Assert.Equal(originalTagCount + 1, clone.Tags.Count);
        }

        [Fact]
        public void ToString_ShouldReturnFormattedString()
        {
            // Arrange
            var context = _contextManager.CreateContext();
            context.UserName = "testuser";
            context.HostName = "testhost";

            // Act
            var result = context.ToString();

            // Assert
            Assert.Contains("testuser", result);
            Assert.Contains("testhost", result);
            Assert.Contains(context.DetectedAt.ToString("yyyy-MM-dd HH:mm:ss"), result);
        }

        [Fact]
        public void ProcessingLatency_ShouldCalculateCorrectly()
        {
            // Arrange
            var context = _contextManager.CreateContext();
            var delay = TimeSpan.FromMilliseconds(100);

            // Act
            context.ProcessedAt = context.DetectedAt.Add(delay);

            // Assert
            Assert.Equal(delay, context.ProcessingLatency);
        }

        [Fact]
        public void ProcessingLatency_WhenNotProcessed_ShouldReturnNull()
        {
            // Arrange
            var context = _contextManager.CreateContext();

            // Act & Assert
            Assert.Null(context.ProcessingLatency);
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Arrange
            var context = _contextManager.CreateContext();
            _contextManager.SetContext(context);

            // Act
            _contextManager.Dispose();

            // Assert
            // After dispose, the context manager should still be accessible but cleared
            Assert.Null(_contextManager.CurrentContext);
        }

        [Fact]
        public void Dispose_ShouldBeCallableMultipleTimes()
        {
            // Act & Assert
            Assert.Null(Record.Exception(() =>
            {
                _contextManager.Dispose();
                _contextManager.Dispose();
            }));
        }

        #region Mock Classes

        private class MockContextEnricher : IContextEnricher
        {
            public bool WasCalled { get; private set; }

            public void Enrich(EnhancedChangeContext context)
            {
                WasCalled = true;
                context.Tags.Add("enriched");
            }
        }

        private class MockContextValidator : IContextValidator
        {
            public bool ShouldReturnValid { get; set; } = true;

            public ContextValidationResult Validate(EnhancedChangeContext context)
            {
                var result = new ContextValidationResult { IsValid = ShouldReturnValid };
                if (!ShouldReturnValid)
                {
                    result.Errors.Add("Validation failed");
                }
                return result;
            }
        }

        private class MockContextProcessor : IContextProcessor
        {
            public bool WasCalled { get; private set; }

            public Task ProcessAsync(EnhancedChangeContext context, CancellationToken cancellationToken = default)
            {
                WasCalled = true;
                context.Tags.Add("processed");
                return Task.CompletedTask;
            }
        }

        #endregion
    }
}
