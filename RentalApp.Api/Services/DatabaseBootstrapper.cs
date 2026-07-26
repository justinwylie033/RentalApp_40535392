using System.Data;
using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;

namespace RentalApp.Api.Services;

public static class DatabaseBootstrapper
{
    private const string InitialMigrationId = "202607160001_InitialCreate";

    public static async Task PrepareLegacySchemaAsync(
        AppDbContext database,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        // Presentation point: early coursework builds used EnsureCreated and therefore
        // had no history row. Baseline only when real tables exist and history is empty.
        if ((await database.Database.GetAppliedMigrationsAsync(cancellationToken)).Any() ||
            !await UsersTableExistsAsync(database, cancellationToken))
        {
            return;
        }

        logger.LogWarning(
            "Existing RentalApp tables have no EF migration history. Baseline migration {MigrationId} will be recorded before applying newer migrations.",
            InitialMigrationId);

        // The INSERT is idempotent, so repeated container starts remain safe.
        await database.Database.ExecuteSqlRawAsync(
            $$"""
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" character varying(150) NOT NULL,
                "ProductVersion" character varying(32) NOT NULL,
                CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
            );

            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('{{InitialMigrationId}}', '10.0.0')
            ON CONFLICT ("MigrationId") DO NOTHING;
            """,
            cancellationToken);
    }

    private static async Task<bool> UsersTableExistsAsync(
        AppDbContext database,
        CancellationToken cancellationToken)
    {
        var connection = database.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        try
        {
            if (shouldClose)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT to_regclass('public.\"Users\"') IS NOT NULL;";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is true;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}
