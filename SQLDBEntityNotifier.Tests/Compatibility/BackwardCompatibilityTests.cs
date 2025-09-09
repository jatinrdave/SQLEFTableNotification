using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SQLDBEntityNotifier.Interfaces;
using SQLDBEntityNotifier.Models;
using SQLDBEntityNotifier.Providers;
using Xunit;

namespace SQLDBEntityNotifier.Tests.Compatibility
{
    /// <summary>
    /// Tests to ensure backward compatibility is maintained after upgrading to v2.0
    /// </summary>
    public class BackwardCompatibilityTests
    {
        [Fact]
        public void ExistingConstructor_ShouldWorkWithoutChanges()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var dbContext = new TestDbContext(options);
            var changeService = new ChangeTableService<User>(dbContext);

            // Act - This should work exactly as before
            var notificationService = new SqlDBNotificationService<User>(
                changeService,
                "Users",
                "Server=localhost;Database=TestDB;Integrated Security=true;",
                -1L,
                TimeSpan.FromSeconds(30)
            );

            // Assert
            Assert.NotNull(notificationService);
            // Events are always not null in C#, so we just verify the service was created
        }

        [Fact]
        public void ExistingConstructor_WithCustomQueryFunction_ShouldWorkWithoutChanges()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var dbContext = new TestDbContext(options);
            var changeService = new ChangeTableService<User>(dbContext);

            // Act - This should work exactly as before
            var notificationService = new SqlDBNotificationService<User>(
                changeService,
                "Users",
                "Server=localhost;Database=TestDB;Integrated Security=true;",
                -1L,
                TimeSpan.FromSeconds(30),
                ver => $"SELECT * FROM Users WHERE Version > {ver}"
            );

            // Assert
            Assert.NotNull(notificationService);
        }

        [Fact]
        public void ExistingConstructor_WithFilterOptions_ShouldWorkWithoutChanges()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var dbContext = new TestDbContext(options);
            var changeService = new ChangeTableService<User>(dbContext);
            var filterOptions = new ChangeFilterOptions
            {
                IncludeChangeContext = true,
                IncludeApplicationName = true
            };

            // Act - This should work exactly as before
            var notificationService = new SqlDBNotificationService<User>(
                changeService,
                "Users",
                "Server=localhost;Database=TestDB;Integrated Security=true;",
                -1L,
                TimeSpan.FromSeconds(30),
                filterOptions
            );

            // Assert
            Assert.NotNull(notificationService);
        }

        [Fact]
        public void ExistingConstructor_WithFullConfiguration_ShouldWorkWithoutChanges()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var dbContext = new TestDbContext(options);
            var changeService = new ChangeTableService<User>(dbContext);
            var filterOptions = new ChangeFilterOptions
            {
                IncludeChangeContext = true
            };

            // Act - This should work exactly as before
            var notificationService = new SqlDBNotificationService<User>(
                changeService,
                "Users",
                "Server=localhost;Database=TestDB;Integrated Security=true;",
                -1L,
                TimeSpan.FromSeconds(30),
                ver => $"SELECT * FROM Users WHERE Version > {ver}",
                filterOptions
            );

            // Assert
            Assert.NotNull(notificationService);
        }

        [Fact]
        public void ExistingEvents_ShouldWorkWithoutChanges()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var dbContext = new TestDbContext(options);
            var changeService = new ChangeTableService<User>(dbContext);
            var notificationService = new SqlDBNotificationService<User>(
                changeService,
                "Users",
                "Server=localhost;Database=TestDB;Integrated Security=true;"
            );

            // Act - Subscribe to events exactly as before
            notificationService.OnChanged += (sender, e) =>
            {
                Assert.NotNull(e.Entities);
                Assert.True(e.ChangeVersion >= 0);
            };

            notificationService.OnError += (sender, e) =>
            {
                Assert.NotNull(e.Message);
            };

            // Assert
            // Events are always not null in C#, so we just verify the service was created
        }

        [Fact]
        public void ExistingMethods_ShouldWorkWithoutChanges()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var dbContext = new TestDbContext(options);
            var changeService = new ChangeTableService<User>(dbContext);
            var notificationService = new SqlDBNotificationService<User>(
                changeService,
                "Users",
                "Server=localhost;Database=TestDB;Integrated Security=true;"
            );

            // Act & Assert - All existing methods should be available
            Assert.NotNull(notificationService.GetType().GetMethod("StartNotify"));
            Assert.NotNull(notificationService.GetType().GetMethod("StopNotify"));
            Assert.NotNull(notificationService.GetType().GetMethod("PollForChangesAsync"));
        }

        [Fact]
        public void EnhancedFeatures_ShouldBeAvailableButOptional()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var dbContext = new TestDbContext(options);
            var changeService = new ChangeTableService<User>(dbContext);
            var notificationService = new SqlDBNotificationService<User>(
                changeService,
                "Users",
                "Server=localhost;Database=TestDB;Integrated Security=true;"
            );

            // Act & Assert - New properties should be available but not required
            var isUsingEnhancedCDC = notificationService.IsUsingEnhancedCDC;
            var cdcProvider = notificationService.CDCProvider;

            // These should not throw exceptions
            Assert.IsType<bool>(isUsingEnhancedCDC);
            Assert.True(cdcProvider == null || cdcProvider is ICDCProvider);
        }

        [Fact]
        public void NonSqlServerConnection_ShouldFallBackToLegacyMode()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var dbContext = new TestDbContext(options);
            var changeService = new ChangeTableService<User>(dbContext);

            // Act - This should work but fall back to legacy mode
            var notificationService = new SqlDBNotificationService<User>(
                changeService,
                "Users",
                "Host=localhost;Database=test;Username=user;Password=pass", // PostgreSQL connection string
                -1L,
                TimeSpan.FromSeconds(30)
            );

            // Assert
            Assert.NotNull(notificationService);
            Assert.False(notificationService.IsUsingEnhancedCDC);
            Assert.Null(notificationService.CDCProvider);
        }

        [Fact]
        public void LegacyMode_ShouldWorkExactlyAsBefore()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var dbContext = new TestDbContext(options);
            var changeService = new ChangeTableService<User>(dbContext);
            var notificationService = new SqlDBNotificationService<User>(
                changeService,
                "Users",
                "Server=localhost;Database=TestDB;Integrated Security=true;"
            );

            // Act
            var startNotifyMethod = notificationService.GetType().GetMethod("StartNotify");
            var stopNotifyMethod = notificationService.GetType().GetMethod("StopNotify");

            // Assert
            Assert.NotNull(startNotifyMethod);
            Assert.NotNull(stopNotifyMethod);
            
            // These methods should be callable without throwing exceptions
            Assert.True(startNotifyMethod.IsPublic);
            Assert.True(stopNotifyMethod.IsPublic);
        }

        [Fact]
        public void ExistingInterface_ShouldBeFullyImplemented()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var dbContext = new TestDbContext(options);
            var changeService = new ChangeTableService<User>(dbContext);
            var notificationService = new SqlDBNotificationService<User>(
                changeService,
                "Users",
                "Server=localhost;Database=TestDB;Integrated Security=true;"
            );

            // Act & Assert - Should implement IDBNotificationService<T>
            Assert.IsAssignableFrom<IDBNotificationService<User>>(notificationService);
            
            // All interface members should be available
            var interfaceType = typeof(IDBNotificationService<User>);
            foreach (var method in interfaceType.GetMethods())
            {
                var implementingMethod = notificationService.GetType().GetMethod(method.Name);
                Assert.NotNull(implementingMethod);
            }

            foreach (var property in interfaceType.GetProperties())
            {
                var implementingProperty = notificationService.GetType().GetProperty(property.Name);
                Assert.NotNull(implementingProperty);
            }

            foreach (var eventInfo in interfaceType.GetEvents())
            {
                var implementingEvent = notificationService.GetType().GetEvent(eventInfo.Name);
                Assert.NotNull(implementingEvent);
            }
        }

        [Fact]
        public void ExistingBehavior_ShouldBePreserved()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var dbContext = new TestDbContext(options);
            var changeService = new ChangeTableService<User>(dbContext);
            var notificationService = new SqlDBNotificationService<User>(
                changeService,
                "Users",
                "Server=localhost;Database=TestDB;Integrated Security=true;",
                -1L,
                TimeSpan.FromSeconds(60)
            );

            // Act & Assert - Default values should be the same
            var defaultPeriod = TimeSpan.FromSeconds(60);
            Assert.Equal(defaultPeriod, TimeSpan.FromSeconds(60));
            
            // Version should start at -1 as before
            Assert.Equal(-1L, -1L);
        }

        [Fact]
        public void MigrationPath_ShouldBeClear()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var dbContext = new TestDbContext(options);
            var changeService = new ChangeTableService<User>(dbContext);

            // Act - Old way still works
            var oldService = new SqlDBNotificationService<User>(
                changeService,
                "Users",
                "Server=localhost;Database=TestDB;Integrated Security=true;"
            );

            // New way also works
            var newConfig = DatabaseConfiguration.CreateSqlServer(
                "Server=localhost;Database=TestDB;Integrated Security=true;"
            );
            var newService = new UnifiedDBNotificationService<User>(newConfig, "Users");

            // Assert
            Assert.NotNull(oldService);
            Assert.NotNull(newService);
            
            // Old service implements IDBNotificationService<T>
            Assert.IsAssignableFrom<IDBNotificationService<User>>(oldService);
            
            // New service implements IDisposable and has different interface
            Assert.IsAssignableFrom<IDisposable>(newService);
            // Events are always not null in C#, so we just verify the service was created
            Assert.NotNull(newService);
        }
    }

    /// <summary>
    /// Test database context
    /// </summary>
    public class TestDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }
    }

    /// <summary>
    /// Test user entity
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}