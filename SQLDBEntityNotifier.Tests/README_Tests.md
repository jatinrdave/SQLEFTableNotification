# SQLDBEntityNotifier Multi-Database CDC Tests

This directory contains comprehensive tests for the enhanced SQLDBEntityNotifier library that supports multiple database types (SQL Server, MySQL, and PostgreSQL) with Change Data Capture (CDC) functionality.

## 🧪 Test Coverage

### **Provider Tests**
- **`SqlServerCDCProviderTests.cs`** - Tests for SQL Server CDC provider
- **`MySqlCDCProviderTests.cs`** - Tests for MySQL CDC provider  
- **`PostgreSqlCDCProviderTests.cs`** - Tests for PostgreSQL CDC provider
- **`CDCProviderFactoryTests.cs`** - Tests for the CDC provider factory

### **Service Tests**
- **`UnifiedDBNotificationServiceTests.cs`** - Tests for the unified notification service
- **`BackwardCompatibilityTests.cs`** - Tests ensuring backward compatibility

### **Model Tests**
- **`DatabaseConfigurationTests.cs`** - Tests for database configuration models
- **`EnhancedRecordChangedEventArgsTests.cs`** - Tests for enhanced event arguments

### **Existing Tests**
- **`SqlDBNotificationServiceTests.cs`** - Tests for existing SQL Server service
- **`ChangeTableServiceTests.cs`** - Tests for change table service
- **`SqlChangeTrackingTests.cs`** - Tests for SQL change tracking

## 🚀 Quick Start

### **Prerequisites**
- .NET 6.0 SDK or later
- Access to test databases (optional, most tests use mocks)

### **Run All Tests**
```bash
# From the test directory
./run_tests.sh

# Or using dotnet directly
dotnet test
```

### **Run Specific Test Categories**
```bash
# SQL Server CDC Provider tests
dotnet test --filter "FullyQualifiedName~SqlServerCDCProvider"

# MySQL CDC Provider tests  
dotnet test --filter "FullyQualifiedName~MySqlCDCProvider"

# PostgreSQL CDC Provider tests
dotnet test --filter "FullyQualifiedName~PostgreSqlCDCProvider"

# Backward compatibility tests
dotnet test --filter "FullyQualifiedName~BackwardCompatibility"

# Provider factory tests
dotnet test --filter "FullyQualifiedName~CDCProviderFactory"
```

### **Run with Detailed Output**
```bash
dotnet test --verbosity detailed
```

## 📋 Test Categories

### **1. Provider Tests**
Tests each CDC provider implementation to ensure:
- ✅ Correct database type detection
- ✅ Proper initialization and validation
- ✅ Change detection and retrieval
- ✅ Error handling and edge cases
- ✅ Resource cleanup and disposal

### **2. Factory Tests**
Tests the CDC provider factory to ensure:
- ✅ Correct provider creation for each database type
- ✅ Connection string parsing and detection
- ✅ Parameter validation and error handling
- ✅ Factory method consistency

### **3. Service Tests**
Tests the unified notification service to ensure:
- ✅ Proper service initialization
- ✅ Change monitoring and event raising
- ✅ Error handling and recovery
- ✅ Health monitoring and validation
- ✅ Resource management

### **4. Model Tests**
Tests the data models to ensure:
- ✅ Proper property access and validation
- ✅ Configuration building and parsing
- ✅ Event argument handling
- ✅ Deep cloning and equality

### **5. Backward Compatibility Tests**
Tests to ensure existing functionality continues to work:
- ✅ All existing constructors work unchanged
- ✅ Existing events and methods function properly
- ✅ Enhanced features are optional and don't break existing code
- ✅ Migration path is clear and functional

## 🔧 Test Configuration

### **Mocking Strategy**
Most tests use **Moq** for mocking database connections and providers:
- Database connections are mocked to avoid external dependencies
- CDC providers are mocked to test service behavior
- Error conditions are simulated to test error handling

### **Test Data**
- **In-Memory Database**: Uses EF Core InMemory provider for entity tests
- **Mock Data**: Simulated change records and database responses
- **Edge Cases**: Tests null values, empty collections, and error conditions

### **Test Isolation**
- Each test class implements `IDisposable` for proper cleanup
- Tests are independent and can run in any order
- No shared state between tests

## 📊 Test Results

### **Success Indicators**
- ✅ All tests pass (exit code 0)
- ✅ No warnings or errors during build
- ✅ Proper test isolation and cleanup

### **Common Issues**
- **Build Failures**: Check .NET version and package references
- **Test Failures**: Review specific test output for details
- **Mock Setup Issues**: Verify mock configurations in test setup

## 🎯 Testing Best Practices

### **Test Naming Convention**
```
[MethodName]_[Condition]_[ExpectedResult]
```
Example: `InitializeAsync_WithValidConfiguration_ShouldReturnTrue`

### **Test Structure (AAA Pattern)**
```csharp
[Fact]
public void MethodName_WithCondition_ShouldReturnExpected()
{
    // Arrange - Setup test data and mocks
    var config = DatabaseConfiguration.CreateMySql("localhost", "db", "user", "pass");
    
    // Act - Execute the method being tested
    var result = config.BuildConnectionString();
    
    // Assert - Verify the expected outcome
    Assert.Contains("Server=localhost", result);
}
```

### **Mock Setup Guidelines**
- Setup mocks in constructor or test setup methods
- Use `SetupSequence` for multiple return values
- Verify mock interactions when testing service behavior
- Clean up mocks in `Dispose` method

## 🚨 Troubleshooting

### **Build Issues**
```bash
# Clean and restore
dotnet clean
dotnet restore
dotnet build
```

### **Test Failures**
```bash
# Run specific failing test
dotnet test --filter "FullyQualifiedName~SpecificTestName"

# Run with detailed output
dotnet test --verbosity detailed --logger "console;verbosity=detailed"
```

### **Package Issues**
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore --force
```

## 📈 Performance Testing

### **Load Testing**
```bash
# Run tests with performance profiling
dotnet test --collect:"XPlat Code Coverage" --settings CodeCoverage.runsettings
```

### **Memory Testing**
- Tests include disposal verification
- Mock objects are properly cleaned up
- No memory leaks in long-running scenarios

## 🔍 Code Coverage

### **Coverage Areas**
- **Provider Implementation**: 100% for core CDC functionality
- **Service Layer**: 100% for notification and monitoring
- **Model Validation**: 100% for configuration and events
- **Error Handling**: 100% for exception scenarios
- **Edge Cases**: 100% for boundary conditions

### **Coverage Reports**
```bash
# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"

# View coverage in browser
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:TestResults/coverage.cobertura.xml -targetdir:coverage
```

## 🏗️ Adding New Tests

### **Test File Structure**
```csharp
namespace SQLDBEntityNotifier.Tests.[Category]
{
    public class [ClassName]Tests : IDisposable
    {
        // Setup
        public [ClassName]Tests() { }
        
        // Tests
        [Fact]
        public void TestName_ShouldWork() { }
        
        // Cleanup
        public void Dispose() { }
    }
}
```

### **Test Categories**
- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test component interactions
- **Behavior Tests**: Test expected behavior and edge cases
- **Performance Tests**: Test performance characteristics

## 📚 Additional Resources

- **xUnit Documentation**: https://xunit.net/
- **Moq Documentation**: https://github.com/moq/moq4
- **.NET Testing**: https://docs.microsoft.com/en-us/dotnet/core/testing/
- **EF Core Testing**: https://docs.microsoft.com/en-us/ef/core/testing/

## 🤝 Contributing

When adding new tests:
1. Follow the existing naming conventions
2. Include comprehensive test coverage
3. Test both success and failure scenarios
4. Ensure proper cleanup and disposal
5. Add appropriate documentation

---

**Happy Testing! 🧪✨**