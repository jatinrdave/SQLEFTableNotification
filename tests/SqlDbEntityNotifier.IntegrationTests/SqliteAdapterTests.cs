using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlDbEntityNotifier.Adapters.Sqlite;
using SqlDbEntityNotifier.Adapters.Sqlite.Models;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Core.Models;
using Xunit;

namespace SqlDbEntityNotifier.IntegrationTests;

/// <summary>
/// Integration tests for the SQLite adapter.
/// </summary>
public class SqliteAdapterTests : IAsyncLifetime
{
    private readonly string _dbPath;
    private readonly SqliteAdapter _adapter;
    private readonly MockOffsetStore _offsetStore;
    private readonly List<ChangeEvent> _receivedEvents;

    public SqliteAdapterTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        _offsetStore = new MockOffsetStore();
        _receivedEvents = new List<ChangeEvent>();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IOffsetStore>(_offsetStore);
        services.Configure<SqliteAdapterOptions>(options =>
        {
            options.Source = "test-sqlite";
            options.FilePath = _dbPath;
            options.ChangeTable = "change_log";
            options.PollIntervalMs = 100; // Fast polling for tests
            options.AutoCreateChangeLog = true;
        });

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<SqliteAdapterOptions>>();
        var logger = serviceProvider.GetRequiredService<ILogger<SqliteAdapter>>();

        _adapter = new SqliteAdapter(options, logger, _offsetStore);
    }

    public async Task InitializeAsync()
    {
        // Create a test table
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();
        
        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS test_orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                order_number TEXT NOT NULL,
                status TEXT NOT NULL,
                amount DECIMAL(10,2) NOT NULL,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )";
        
        using var command = new Microsoft.Data.Sqlite.SqliteCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        await _adapter.StopAsync();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public async Task StartAsync_ShouldStartSuccessfully()
    {
        // Act
        await _adapter.StartAsync(OnChangeEvent);

        // Assert
        // The adapter should start without throwing an exception
        await Task.Delay(100); // Give it time to initialize
    }

    [Fact]
    public async Task DatabaseChanges_ShouldTriggerChangeEvents()
    {
        // Arrange
        await _adapter.StartAsync(OnChangeEvent);
        await Task.Delay(200); // Wait for polling to start

        // Act - Insert a record
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();
        
        var insertSql = @"
            INSERT INTO test_orders (order_number, status, amount)
            VALUES ('ORD-001', 'Pending', 99.99)";
        
        using var command = new Microsoft.Data.Sqlite.SqliteCommand(insertSql, connection);
        await command.ExecuteNonQueryAsync();

        // Wait for the change to be detected
        await Task.Delay(500);

        // Assert
        Assert.Single(_receivedEvents);
        var changeEvent = _receivedEvents[0];
        Assert.Equal("test-sqlite", changeEvent.Source);
        Assert.Equal("main", changeEvent.Schema);
        Assert.Equal("test_orders", changeEvent.Table);
        Assert.Equal("INSERT", changeEvent.Operation);
    }

    [Fact]
    public async Task GetCurrentOffsetAsync_ShouldReturnStoredOffset()
    {
        // Arrange
        var expectedOffset = "123";
        await _offsetStore.SetOffsetAsync("test-sqlite", expectedOffset);

        // Act
        var actualOffset = await _adapter.GetCurrentOffsetAsync();

        // Assert
        Assert.Equal(expectedOffset, actualOffset);
    }

    [Fact]
    public async Task SetOffsetAsync_ShouldStoreOffset()
    {
        // Arrange
        var offset = "456";

        // Act
        await _adapter.SetOffsetAsync(offset);

        // Assert
        var storedOffset = await _offsetStore.GetOffsetAsync("test-sqlite");
        Assert.Equal(offset, storedOffset);
    }

    [Fact]
    public async Task ReplayFromOffsetAsync_ShouldReplayEvents()
    {
        // Arrange
        var replayedEvents = new List<ChangeEvent>();
        
        // Insert some test data first
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();
        
        var insertSql = @"
            INSERT INTO test_orders (order_number, status, amount)
            VALUES ('ORD-REPLAY-001', 'Pending', 199.99)";
        
        using var command = new Microsoft.Data.Sqlite.SqliteCommand(insertSql, connection);
        await command.ExecuteNonQueryAsync();

        // Act
        await _adapter.ReplayFromOffsetAsync("0", (ev, ct) =>
        {
            replayedEvents.Add(ev);
            return Task.CompletedTask;
        });

        // Assert
        Assert.NotEmpty(replayedEvents);
        var replayedEvent = replayedEvents.First();
        Assert.Equal("ORD-REPLAY-001", replayedEvent.After?.GetProperty("order_number").GetString());
    }

    private Task OnChangeEvent(ChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        _receivedEvents.Add(changeEvent);
        return Task.CompletedTask;
    }
}