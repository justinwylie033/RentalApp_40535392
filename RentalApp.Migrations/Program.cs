using Microsoft.EntityFrameworkCore;
using RentalApp.Migrations;

await using var context = new AppDbContextFactory().CreateDbContext(args);
if (context.Database.GetMigrations().Any())
{
    await context.Database.MigrateAsync();
}
else
{
    await context.Database.EnsureCreatedAsync();
}

Console.WriteLine("RentalApp database is up to date.");
