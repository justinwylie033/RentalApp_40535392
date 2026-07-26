using NetTopologySuite.Geometries;
using RentalApp.Api.Services;
using RentalApp.Contracts;
using RentalApp.Database.Data;
using RentalApp.Database.Data.Repositories;
using RentalApp.Database.Models;
using RentalApp.Test.Fixtures;

namespace RentalApp.Test.Services;

public sealed class ReviewServiceTests
{
    [Fact]
    public async Task CreateAsync_CompletedRental_CreatesReview()
    {
        await using var context = TestContextFactory.Create();
        var data = await SeedCompletedRentalAsync(context);
        var service = new ReviewService(
            new ReviewRepository(context),
            new RentalRepository(context),
            new UnitOfWork(context));

        var result = await service.CreateAsync(
            data.Borrower.Id,
            new CreateReviewRequest(data.Rental.Id, 5, "Exactly as described."));

        Assert.Equal(5, result.Rating);
        Assert.Equal(data.Item.Id, result.ItemId);
        Assert.Single(context.Reviews);
    }

    [Fact]
    public async Task CreateAsync_IncompleteRental_RejectsReview()
    {
        await using var context = TestContextFactory.Create();
        var data = await SeedCompletedRentalAsync(context);
        data.Rental.Status = RentalStatus.Returned;
        await context.SaveChangesAsync();
        var service = new ReviewService(
            new ReviewRepository(context),
            new RentalRepository(context),
            new UnitOfWork(context));

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(
            data.Borrower.Id,
            new CreateReviewRequest(data.Rental.Id, 4, "Good condition.")));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task CreateAsync_InvalidRating_RejectsReview(int rating)
    {
        await using var context = TestContextFactory.Create();
        var service = new ReviewService(
            new ReviewRepository(context),
            new RentalRepository(context),
            new UnitOfWork(context));

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(
            Guid.NewGuid(),
            new CreateReviewRequest(Guid.NewGuid(), rating, "Comment")));
    }

    private static async Task<(User Owner, User Borrower, Item Item, Rental Rental)> SeedCompletedRentalAsync(
        AppDbContext context)
    {
        var owner = new User { DisplayName = "Owner", Email = "owner@test.local", PasswordHash = "hash" };
        var borrower = new User { DisplayName = "Borrower", Email = "borrower@test.local", PasswordHash = "hash" };
        var item = new Item
        {
            Owner = owner,
            Title = "Board game",
            Description = "Complete board game in good condition.",
            DailyRate = 3m,
            Category = ItemCategory.Games,
            Location = new Point(-3.18, 55.95) { SRID = 4326 }
        };
        var rental = new Rental
        {
            Item = item,
            Borrower = borrower,
            StartDateUtc = DateTimeOffset.UtcNow.AddDays(-4),
            EndDateUtc = DateTimeOffset.UtcNow.AddDays(-3),
            TotalPrice = 6m,
            Status = RentalStatus.Completed
        };
        context.AddRange(owner, borrower, item, rental);
        await context.SaveChangesAsync();
        return (owner, borrower, item, rental);
    }
}
