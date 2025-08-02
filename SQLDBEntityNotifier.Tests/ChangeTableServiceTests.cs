using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using SQLDBEntityNotifier;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ChangeTableServiceTests
{
    [Fact]
    public async Task GetRecords_ReturnsEntities()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;
        var dbContext = new TestDbContext(options);
        dbContext.Users.Add(new UserChangeTable { Id = 1, Name = "Test" });
        dbContext.SaveChanges();

        var service = new ChangeTableService<UserChangeTable>(dbContext);
        var records = await service.GetRecords("SELECT * FROM Users");
        Assert.Single(records);
        Assert.Equal("Test", records[0].Name);
    }

    public class UserChangeTable
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<UserChangeTable> Users { get; set; }
    }
}
