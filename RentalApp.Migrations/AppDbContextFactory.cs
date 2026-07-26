using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RentalApp.Database.Data;

namespace RentalApp.Migrations;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=rentalapp;Username=app_user;Password=app_password";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, postgres =>
            {
                postgres.UseNetTopologySuite();
                postgres.MigrationsAssembly(typeof(MigrationAssemblyMarker).Assembly.FullName);
            })
            .Options;
        return new AppDbContext(options);
    }
}
