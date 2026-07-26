using Microsoft.EntityFrameworkCore;
using Npgsql;
using RentalApp.Database.Data;
using RentalApp.Migrations;

namespace RentalApp.Test.Fixtures;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string TestDatabaseName = "rentalapp_test";
    private readonly string _serverConnection = Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=postgres;Username=app_user;Password=app_password";

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var builder = new NpgsqlConnectionStringBuilder(_serverConnection) { Database = "postgres" };
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using (var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS {TestDatabaseName} WITH (FORCE);", connection))
        {
            await drop.ExecuteNonQueryAsync();
        }

        await using (var create = new NpgsqlCommand($"CREATE DATABASE {TestDatabaseName};", connection))
        {
            await create.ExecuteNonQueryAsync();
        }

        builder.Database = TestDatabaseName;
        builder.Pooling = false;
        ConnectionString = builder.ConnectionString;

        // Install PostGIS before EF opens its spatial connection. If the extension is
        // created by the first migration after Npgsql has loaded the database types,
        // that connection cannot resolve geography parameters until its types reload.
        await using (var extensionConnection = new NpgsqlConnection(ConnectionString))
        {
            await extensionConnection.OpenAsync();
            await using var extension = new NpgsqlCommand(
                "CREATE EXTENSION IF NOT EXISTS postgis;",
                extensionConnection);
            await extension.ExecuteNonQueryAsync();
        }

        await using var context = CreateContext();
        if (context.Database.GetMigrations().Any())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }
    }

    public async Task DisposeAsync()
    {
        var builder = new NpgsqlConnectionStringBuilder(_serverConnection) { Database = "postgres" };
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS {TestDatabaseName} WITH (FORCE);", connection);
        await command.ExecuteNonQueryAsync();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString, postgres =>
            {
                postgres.UseNetTopologySuite();
                postgres.MigrationsAssembly(typeof(MigrationAssemblyMarker).Assembly.FullName);
            })
            .Options;
        return new AppDbContext(options);
    }
}

[CollectionDefinition("PostGIS database")]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "PostGIS database";
}
