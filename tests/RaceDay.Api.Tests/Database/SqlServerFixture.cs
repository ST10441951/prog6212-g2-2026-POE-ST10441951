using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using Xunit;

namespace RaceDay.Api.Tests.Database;

// Use the real provider for migrations and SQL constraints: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/testing/testing-with-the-database
// Shared fixture lifetime: https://xunit.net/docs/shared-context
// Accessed: 18 October 2026.
public sealed class SqlServerFixture : IAsyncLifetime
{
    public string ConnectionString { get; private set; } = string.Empty;

    public RaceDayDbContext CreateContext() => new(
        new DbContextOptionsBuilder<RaceDayDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        string? configured = Environment.GetEnvironmentVariable("RACEDAY_TEST_CONNECTION");
        var connection = new SqlConnectionStringBuilder(configured ??
            @"Server=(localdb)\MSSQLLocalDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True");

        // Always replace the database name, even when the caller supplies a connection.
        connection.AttachDBFilename = string.Empty;
        connection.InitialCatalog = $"RaceDayTests_{Guid.NewGuid():N}";
        ConnectionString = connection.ConnectionString;
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrEmpty(ConnectionString))
            return;

        var connection = new SqlConnectionStringBuilder(ConnectionString);
        if (!connection.InitialCatalog.StartsWith("RaceDayTests_", StringComparison.Ordinal))
            throw new InvalidOperationException("Only this fixture's temporary database may be removed.");

        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}
