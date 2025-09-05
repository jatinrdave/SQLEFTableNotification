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
        // Arrange
        var mockChangeService = new Mock<IChangeTableService<User>>();
        mockChangeService.Setup(s => s.GetRecordCount(It.IsAny<string>())).ReturnsAsync(1L);
        mockChangeService.Setup(s => s.GetRecords(It.IsAny<string>())).ReturnsAsync(new List<User> { new User { Id = 1, Name = "Alice" } });
        
        var notificationService = new SqlDBNotificationService<User>(
            mockChangeService.Object,
            "Users",
            "FakeConnectionString",
            -1L,
            null,
            (fromVer) => "SELECT * FROM Users");

        bool notificationRaised = false;
        notificationService.OnChanged += (sender, args) =>
        {
            notificationRaised = true;
            var entities = args.Entities is List<User> list ? list : new List<User>(args.Entities);
            Assert.Single(entities);
            Assert.Equal("Alice", entities[0].Name);
        };

        // Act
        await notificationService.PollForChangesAsync();
        
        // Assert
        Assert.True(notificationRaised);
    }

    [Fact]
    public async Task Notification_Raised_When_User_Updated()
    {
        // Arrange
        var mockChangeService = new Mock<IChangeTableService<User>>();
        mockChangeService.Setup(s => s.GetRecordCount(It.IsAny<string>())).ReturnsAsync(2L);
        mockChangeService.Setup(s => s.GetRecords(It.IsAny<string>())).ReturnsAsync(new List<User> { new User { Id = 1, Name = "Robert" } });
        
        var notificationService = new SqlDBNotificationService<User>(
            mockChangeService.Object,
            "Users",
            "FakeConnectionString",
            1L,
            null,
            (fromVer) => "SELECT * FROM Users");

        bool notificationRaised = false;
        notificationService.OnChanged += (sender, args) =>
        {
            notificationRaised = true;
            var entities = args.Entities is List<User> list ? list : new List<User>(args.Entities);
            Assert.Single(entities);
            Assert.Equal("Robert", entities[0].Name);
        };

        // Act
        await notificationService.PollForChangesAsync();
        
        // Assert
        Assert.True(notificationRaised);
    }

    [Fact]
    public async Task Notification_Raised_When_User_Deleted()
    {
        // Arrange
        var mockChangeService = new Mock<IChangeTableService<User>>();
        mockChangeService.Setup(s => s.GetRecordCount(It.IsAny<string>())).ReturnsAsync(3L);
        mockChangeService.Setup(s => s.GetRecords(It.IsAny<string>())).ReturnsAsync(new List<User>());
        
        var notificationService = new SqlDBNotificationService<User>(
            mockChangeService.Object,
            "Users",
            "FakeConnectionString",
            2L,
            null,
            (fromVer) => "SELECT * FROM Users");

        bool notificationRaised = false;
        notificationService.OnChanged += (sender, args) =>
        {
            notificationRaised = true;
            var entities = args.Entities is List<User> list ? list : new List<User>(args.Entities);
            Assert.Empty(entities);
        };

        // Act
        await notificationService.PollForChangesAsync();
        
        // Assert
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
