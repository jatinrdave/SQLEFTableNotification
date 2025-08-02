using Xunit;
using Microsoft.EntityFrameworkCore;
using SQLDBEntityNotifier;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Moq;

public class SqlDBNotificationServiceTests
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
    public async Task Notification_Raised_When_User_Added()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new TestDbContext(options);
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

        dbContext.Users.Add(new User { Id = 1, Name = "Alice" });
        dbContext.SaveChanges();

        await notificationService.PollForChangesAsync();
        Assert.True(notificationRaised);
    }

    [Fact]
    public async Task Notification_Raised_When_User_Updated()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new TestDbContext(options);
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
        Assert.True(notificationRaised);
    }

    [Fact]
    public async Task Notification_Raised_When_User_Deleted()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new TestDbContext(options);
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
            Assert.Empty(entities);
        };

        var user = await dbContext.Users.FirstAsync();
        dbContext.Users.Remove(user);
        dbContext.SaveChanges();

        await notificationService.PollForChangesAsync();
        Assert.True(notificationRaised);
    }

    [Fact]
    public async Task Error_Raised_On_Invalid_Query()
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
