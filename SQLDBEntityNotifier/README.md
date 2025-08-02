# SQLDBEntityNotifier

A .NET library for SQL Server change tracking notifications, designed for use with Entity Framework Core and dependency injection. Supports event-driven notifications for insert, update, delete, and error scenarios.

## Features
- Works with any .NET version supporting EF Core 6+
- Event-driven notifications for table changes
- In-memory and real SQL Server support
- Comprehensive unit tests
- Easy DI registration

## Getting Started

### Installation
Add the NuGet package to your project:

```
dotnet add package SQLDBEntityNotifier
```

### Usage Example
```csharp
// Register services in DI
services.AddSQLDBNotifier();

// Create and use notification service
var changeService = new ChangeTableService<User>(dbContext);
var notificationService = new SqlDBNotificationService<User>(
    changeService,
    "Users",
    connectionString,
    -1L,
    null,
    ver => $"SELECT * FROM Users" // or your change tracking query
);

notificationService.OnChanged += (sender, args) =>
{
    foreach (var user in args.Entities ?? Enumerable.Empty<User>())
    {
        Console.WriteLine($"User changed: {user.Name}");
    }
};

notificationService.OnError += (sender, args) =>
{
    Console.WriteLine($"Error: {args.Message}");
};

await notificationService.PollForChangesAsync();
```

## Testing
Unit tests are provided in the `SQLDBEntityNotifier.Tests` project. Run with:

```
dotnet test SQLDBEntityNotifier.Tests/SQLDBEntityNotifier.Tests.csproj --collect:"Code Coverage"
```

## License
MIT
