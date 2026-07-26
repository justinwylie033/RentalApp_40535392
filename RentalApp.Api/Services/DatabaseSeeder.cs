using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RentalApp.Contracts;
using RentalApp.Database.Data;
using RentalApp.Database.Models;

namespace RentalApp.Api.Services;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var hasher = new PasswordHasher<User>();
        var owner = new User
        {
            DisplayName = "Sarah Owner",
            Email = "sarah@example.com",
            PasswordHash = string.Empty
        };
        owner.PasswordHash = hasher.HashPassword(owner, "Rental123!");
        var borrower = new User
        {
            DisplayName = "Mike Borrower",
            Email = "mike@example.com",
            PasswordHash = string.Empty
        };
        borrower.PasswordHash = hasher.HashPassword(borrower, "Rental123!");

        context.Users.AddRange(owner, borrower);
        context.Items.AddRange(
            new Item
            {
                Owner = owner,
                Title = "18V Cordless Drill",
                Description = "Reliable cordless drill supplied with two batteries and a charger.",
                DailyRate = 8.50m,
                Category = ItemCategory.Tools,
                Address = "1 Princes Street, Edinburgh, EH2 2EQ",
                Location = new Point(-3.1883, 55.9533) { SRID = 4326 }
            },
            new Item
            {
                Owner = owner,
                Title = "Four-person Camping Tent",
                Description = "Waterproof family tent with poles, pegs, groundsheet, and carry bag.",
                DailyRate = 14m,
                Category = ItemCategory.Camping,
                Address = "30 Castle Terrace, Edinburgh, EH1 2EL",
                Location = new Point(-3.2034, 55.9509) { SRID = 4326 }
            });
        await context.SaveChangesAsync(cancellationToken);
    }
}
