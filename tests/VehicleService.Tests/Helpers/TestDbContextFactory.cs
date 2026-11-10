using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VehicleService.Api.Data;

namespace VehicleService.Tests.Helpers;

public static class TestDbContextFactory
{
    public static async Task<(AppDbContext Context, SqliteConnection Connection)> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new AppDbContext(options);

            await context.Database.EnsureCreatedAsync();

            return (context, connection);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}