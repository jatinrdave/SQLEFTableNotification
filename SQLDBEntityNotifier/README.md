# SQLDBEntityNotifier

[![NuGet Version](https://img.shields.io/nuget/v/SQLDBEntityNotifier.svg)](https://www.nuget.org/packages/SQLDBEntityNotifier/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SQLDBEntityNotifier.svg)](https://www.nuget.org/packages/SQLDBEntityNotifier/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 6.0](https://img.shields.io/badge/.NET-6.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/6.0)

A comprehensive .NET library for database change tracking notifications across SQL Server, MySQL, and PostgreSQL with advanced Change Data Capture (CDC) features.

## 🚀 Features

- **Multi-Database Support**: SQL Server, MySQL, and PostgreSQL
- **Advanced CDC Features**: Change Data Capture with real-time notifications
- **Column-Level Filtering**: Monitor or exclude specific columns
- **Change Analytics**: Performance metrics and change pattern analysis
- **Schema Change Detection**: Real-time schema modification monitoring
- **Change Correlation**: Identify relationships between changes across tables
- **Advanced Routing**: Intelligent change routing to multiple destinations
- **Change Replay**: Replay historical changes for testing and recovery
- **Real-time Monitoring**: Comprehensive system health and performance monitoring
- **High Performance**: Optimized for high-throughput scenarios
- **Comprehensive Testing**: 379+ unit tests with 100% pass rate

## 📦 Installation

```bash
dotnet add package SQLDBEntityNotifier
```

Or via Package Manager Console:

```powershell
Install-Package SQLDBEntityNotifier
```

## 🎯 Quick Start

### Basic Usage

```csharp
using SQLDBEntityNotifier;

// Configure the service
var service = new UnifiedDBNotificationService();

// Set up event handlers
service.OnInsert += (change) => Console.WriteLine($"Insert: {change.TableName}");
service.OnUpdate += (change) => Console.WriteLine($"Update: {change.TableName}");
service.OnDelete += (change) => Console.WriteLine($"Delete: {change.TableName}");

// Start monitoring
await service.StartAsync("Server=localhost;Database=MyDB;Trusted_Connection=true;");
```

### Advanced Configuration

```csharp
var config = new NotificationConfig
{
    ConnectionString = "Server=localhost;Database=MyDB;Trusted_Connection=true;",
    DatabaseType = DatabaseType.SqlServer,
    MonitorColumns = new[] { "Name", "Email" },
    ExcludeColumns = new[] { "Password", "Token" },
    EnableCDC = true,
    EnableAnalytics = true
};

await service.StartAsync(config);
```

## 🏗️ Advanced Features

### Change Analytics

```csharp
var analytics = new ChangeAnalytics();
analytics.RecordChange(changeRecord);

var metrics = analytics.GetAggregatedMetrics();
Console.WriteLine($"Total Changes: {metrics.TotalChanges}");
Console.WriteLine($"Average Processing Time: {metrics.AverageProcessingTime}");
```

### Schema Change Detection

```csharp
var schemaDetector = new SchemaChangeDetection();
var changes = await schemaDetector.DetectSchemaChangesAsync(cdcProvider, "Users");
foreach (var change in changes)
{
    Console.WriteLine($"Schema Change: {change.ChangeType} - {change.ColumnName}");
}
```

### Advanced Filtering

```csharp
var filters = new AdvancedChangeFilters();
filters.AddColumnFilter("Status", "Active");
filters.AddValueFilter("Amount", ">", 1000);

var filteredChanges = filters.ApplyFilters(changes);
```

## 🔧 Configuration

### Database Types

- **SQL Server**: Native Change Data Capture support
- **MySQL**: Binary log replication
- **PostgreSQL**: WAL replication

### Connection Strings

```csharp
// SQL Server
"Server=localhost;Database=MyDB;Trusted_Connection=true;"

// MySQL
"Server=localhost;Database=MyDB;Uid=root;Pwd=password;"

// PostgreSQL
"Host=localhost;Database=MyDB;Username=postgres;Password=password;"
```

## 📊 Performance

- **Change Detection Latency**: <100ms
- **Throughput**: 1000+ changes/second
- **Memory Usage**: <500MB
- **CPU Utilization**: <20%

## 🧪 Testing

The library includes comprehensive test coverage:

```bash
dotnet test SQLDBEntityNotifier.Tests
```

**Test Results**: 379 tests passing (100% pass rate)

## 📚 Documentation

- [API Reference](API_REFERENCE.md)
- [Advanced Features](README_AdvancedCDCFeatures.md)
- [Examples](EXAMPLES.md)
- [Development Roadmap](TESTSPRITE_DEVELOPMENT_ROADMAP.md)

## 🤝 Contributing

Contributions are welcome! Please read our contributing guidelines and submit pull requests to our [GitHub repository](https://github.com/jatinrdave/SQLEFTableNotification).

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Entity Framework Core team for the excellent ORM framework
- Microsoft for .NET platform
- Open source community for inspiration and feedback

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/jatinrdave/SQLEFTableNotification/issues)
- **Discussions**: [GitHub Discussions](https://github.com/jatinrdave/SQLEFTableNotification/discussions)
- **Documentation**: [Wiki](https://github.com/jatinrdave/SQLEFTableNotification/wiki)

---

**Made with ❤️ by [Jatin Dave](https://github.com/jatinrdave)**