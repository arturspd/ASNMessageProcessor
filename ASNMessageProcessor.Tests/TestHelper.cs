using ASNMessageProcessor.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ASNMessageProcessor.Tests
{
    public class TestHelper
    {
        public static AppDbContext CreateTestDbContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var dbContext = new AppDbContext(options);

            dbContext.Database.EnsureCreated();

            return dbContext;
        }
    }
}
