# SQLDBEntityNotifier Documentation

## Overview
SQLDBEntityNotifier is a .NET library for SQL Server change tracking notifications, designed for use with Entity Framework Core and dependency injection. It provides event-driven notifications for insert, update, delete, and error scenarios, supporting both in-memory and real SQL Server databases.

## Features
- Works with .NET 6.0 and above
- Supports Entity Framework Core 6+
- Event-driven notifications for table changes
- Comprehensive unit tests
- Easy DI registration

## Installation
Add the NuGet package to your project:

```
dotnet add package SQLDBEntityNotifier
```

## Usage
### Registering Services
```csharp
services.AddSQLDBNotifier();
```

### Creating and Using Notification Service
```csharp
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

## API Reference
### ChangeTableService<T>
- `GetRecords(string commandText)`: Returns a list of changed entities.
- `GetRecordsSync(string commandText)`: Synchronous version.

### SqlDBNotificationService<T>
- `OnChanged`: Event triggered when records change.
- `OnError`: Event triggered on error.
- `PollForChangesAsync()`: Polls for changes and raises events.

### Event Args
- `RecordChangedEventArgs<T>`: Contains `Entities` property (changed records).
- `ErrorEventArgs`: Contains `Message` and `Exception` properties.

## Testing
Unit tests are provided in the `SQLDBEntityNotifier.Tests` project. Run with:

```
dotnet test SQLDBEntityNotifier.Tests/SQLDBEntityNotifier.Tests.csproj --collect:"Code Coverage"
```

## Example Test Case
```csharp
[Fact]
public async Task ChangeTracking_Enabled_And_Notification_Raised_On_User_Insert()
{
    var options = new DbContextOptionsBuilder<TestDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    var dbContext = new TestDbContext(options);
    dbContext.Users.Add(new User { Id = 1, Name = "Alice" });
    dbContext.SaveChanges();

    var changeService = new ChangeTableService<User>(dbContext);
    var notificationService = new SqlDBNotificationService<User>(
        changeService,
        "Users",
        "FakeConnectionString",
        -1L,
        null,
        _ => "SELECT * FROM Users");

    bool notificationRaised = false;
    notificationService.OnChanged += (sender, args) =>
    {
        notificationRaised = true;
        var entities = args.Entities is List<User> list ? list : new List<User>(args.Entities);
        Assert.Single(entities);
        Assert.Equal("Alice", entities[0].Name);
    };

    await notificationService.PollForChangesAsync();
    Assert.True(notificationRaised);
}
```

## Usage Sample

```csharp
// 1. Register services in DI (Startup.cs or Program.cs)
services.AddSQLDBNotifier();

// 2. Create ChangeTableService and NotificationService
var changeService = new ChangeTableService<User>(dbContext);
var notificationService = new SqlDBNotificationService<User>(
    changeService,
    "Users",
    connectionString,
    -1L,
    null,
    ver => $"SELECT * FROM Users" // or your change tracking query
);

// 3. Subscribe to events
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

// 4. Poll for changes (can be called on a timer or manually)
await notificationService.PollForChangesAsync();
```

## License
MIT
