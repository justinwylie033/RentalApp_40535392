using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;

namespace RentalApp.Test.Fixtures;

internal static class TestContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
