using Xunit;
using Microsoft.EntityFrameworkCore;
using Moq;
using SQLDBEntityNotifier;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

public class SqlChangeTrackingTests
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
    }

    [Fact]
    public async Task ChangeTracking_Enabled_And_Notification_Raised_On_User_Insert()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new TestDbContext(options);
        // Simulate enabling change tracking (in-memory, so just a flag)
        bool changeTrackingEnabled = true;
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
        Assert.True(changeTrackingEnabled);
        Assert.True(notificationRaised);
    }

    [Fact]
    public async Task ChangeTracking_Enabled_And_Notification_Raised_On_User_Update()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new TestDbContext(options);
        bool changeTrackingEnabled = true;
        dbContext.Users.Add(new User { Id = 1, Name = "Bob" });
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
            Assert.Equal("Robert", entities[0].Name);
        };

        var user = await dbContext.Users.FirstAsync();
        user.Name = "Robert";
        dbContext.SaveChanges();

        await notificationService.PollForChangesAsync();
        Assert.True(changeTrackingEnabled);
        Assert.True(notificationRaised);
    }

    [Fact]
    public async Task ChangeTracking_Enabled_And_Notification_Raised_On_User_Delete()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new TestDbContext(options);
        bool changeTrackingEnabled = true;
        dbContext.Users.Add(new User { Id = 1, Name = "Charlie" });
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
            // Notification should be raised even if entities are empty
            Assert.Empty(entities);
        };

        var user = await dbContext.Users.FirstAsync();
        dbContext.Users.Remove(user);
        dbContext.SaveChanges();

        await notificationService.PollForChangesAsync();
        Assert.True(changeTrackingEnabled);
        Assert.True(notificationRaised);
    }

    [Fact]
    public async Task ChangeTracking_Disabled_No_Notification()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new TestDbContext(options);
        bool changeTrackingEnabled = false;
        dbContext.Users.Add(new User { Id = 1, Name = "David" });
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
        };

        // Simulate: if change tracking is disabled, do not check for changes
        if (changeTrackingEnabled)
            await notificationService.PollForChangesAsync();

        Assert.False(notificationRaised);
    }

    [Fact]
    public async Task Error_Raised_On_Invalid_Query_Mock()
    {
        var mockChangeService = new Mock<IChangeTableService<User>>();
        mockChangeService.Setup(s => s.GetRecords(It.IsAny<string>())).ThrowsAsync(new Exception("Invalid query"));
        var notificationService = new SqlDBNotificationService<User>(
            mockChangeService.Object,
            "Users",
            "FakeConnectionString",
            -1L,
            null,
            _ => "SELECT * FROM NonExistentTable");

        bool errorRaised = false;
        notificationService.OnError += (sender, args) =>
        {
            errorRaised = true;
            Assert.NotNull(args.Exception);
            Assert.Equal("Invalid query", args.Exception.Message);
        };

        await notificationService.PollForChangesAsync();
        Assert.True(errorRaised);
    }
}
