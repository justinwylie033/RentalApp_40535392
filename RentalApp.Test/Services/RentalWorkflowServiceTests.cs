using NetTopologySuite.Geometries;
using RentalApp.Api.Services;
using RentalApp.Contracts;
using RentalApp.Database.Data;
using RentalApp.Database.Data.Repositories;
using RentalApp.Database.Models;
using RentalApp.Database.States;
using RentalApp.Test.Fixtures;

namespace RentalApp.Test.Services;

public sealed class RentalWorkflowServiceTests
{
    [Fact]
    public async Task RequestAsync_AvailableThreeDayRental_CalculatesInclusivePrice()
    {
        await using var context = TestContextFactory.Create();
        var data = await SeedUsersAndItemAsync(context);
        var service = CreateService(context);
        var start = DateTimeOffset.UtcNow.Date.AddDays(2);

        var result = await service.RequestAsync(
            data.Borrower.Id,
            new CreateRentalRequest(data.Item.Id, start, start.AddDays(2)));

        Assert.Equal(30m, result.TotalPrice);
        Assert.Equal(RentalStatus.Requested, result.Status);
        Assert.Equal(data.Borrower.Id, result.BorrowerId);
    }

    [Fact]
    public async Task RequestAsync_OwnerRequestsOwnItem_RejectsRequest()
    {
        await using var context = TestContextFactory.Create();
        var data = await SeedUsersAndItemAsync(context);
        var service = CreateService(context);
        var start = DateTimeOffset.UtcNow.Date.AddDays(2);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => service.RequestAsync(
            data.Owner.Id,
            new CreateRentalRequest(data.Item.Id, start, start.AddDays(1))));

        Assert.Contains("own item", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestAsync_OverlappingDates_RejectsSecondRequest()
    {
        await using var context = TestContextFactory.Create();
        var data = await SeedUsersAndItemAsync(context);
        var service = CreateService(context);
        var start = DateTimeOffset.UtcNow.Date.AddDays(4);
        await service.RequestAsync(data.Borrower.Id, new CreateRentalRequest(data.Item.Id, start, start.AddDays(2)));

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => service.RequestAsync(
            data.SecondBorrower.Id,
            new CreateRentalRequest(data.Item.Id, start.AddDays(1), start.AddDays(3))));

        Assert.Contains("booked", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TransitionAsync_OwnerApprovesRequest_ChangesStatus()
    {
        await using var context = TestContextFactory.Create();
        var data = await SeedUsersAndItemAsync(context);
        var service = CreateService(context);
        var start = DateTimeOffset.UtcNow.Date.AddDays(2);
        var rental = await service.RequestAsync(
            data.Borrower.Id,
            new CreateRentalRequest(data.Item.Id, start, start.AddDays(1)));

        var result = await service.TransitionAsync(data.Owner.Id, rental.Id, RentalStatus.Approved);

        Assert.Equal(RentalStatus.Approved, result.Status);
    }

    [Fact]
    public async Task TransitionAsync_BorrowerAttemptsApproval_RejectsTransition()
    {
        await using var context = TestContextFactory.Create();
        var data = await SeedUsersAndItemAsync(context);
        var service = CreateService(context);
        var start = DateTimeOffset.UtcNow.Date.AddDays(2);
        var rental = await service.RequestAsync(
            data.Borrower.Id,
            new CreateRentalRequest(data.Item.Id, start, start));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.TransitionAsync(data.Borrower.Id, rental.Id, RentalStatus.Approved));
    }

    [Fact]
    public async Task TransitionAsync_BorrowerCancelsApprovedRental_ReleasesDates()
    {
        await using var context = TestContextFactory.Create();
        var data = await SeedUsersAndItemAsync(context);
        var service = CreateService(context);
        var start = DateTimeOffset.UtcNow.Date.AddDays(5);
        var rental = await service.RequestAsync(
            data.Borrower.Id,
            new CreateRentalRequest(data.Item.Id, start, start.AddDays(1)));
        await service.TransitionAsync(data.Owner.Id, rental.Id, RentalStatus.Approved);

        var cancelled = await service.TransitionAsync(
            data.Borrower.Id,
            rental.Id,
            RentalStatus.Cancelled);
        var replacement = await service.RequestAsync(
            data.SecondBorrower.Id,
            new CreateRentalRequest(data.Item.Id, start, start.AddDays(1)));

        Assert.Equal(RentalStatus.Cancelled, cancelled.Status);
        Assert.Equal(RentalStatus.Requested, replacement.Status);
    }

    [Fact]
    public async Task MarkOverdueAsync_ExpiredOutForRentRental_TransitionsAutomatically()
    {
        await using var context = TestContextFactory.Create();
        var data = await SeedUsersAndItemAsync(context);
        var rental = new Rental
        {
            Item = data.Item,
            Borrower = data.Borrower,
            StartDateUtc = DateTimeOffset.UtcNow.AddDays(-3),
            EndDateUtc = DateTimeOffset.UtcNow.AddDays(-1),
            TotalPrice = 20m,
            Status = RentalStatus.OutForRent
        };
        context.Rentals.Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var changed = await service.MarkOverdueAsync(DateTimeOffset.UtcNow);

        Assert.Equal(1, changed);
        Assert.Equal(RentalStatus.Overdue, rental.Status);
    }

    [Fact]
    public async Task GetUnavailableDatesAsync_RejectedAndCancelledRentals_AreExcluded()
    {
        await using var context = TestContextFactory.Create();
        var data = await SeedUsersAndItemAsync(context);
        var start = DateTimeOffset.UtcNow.Date.AddDays(10);
        context.Rentals.AddRange(
            CreateRental(data, start, RentalStatus.Approved),
            CreateRental(data, start.AddDays(3), RentalStatus.Rejected),
            CreateRental(data, start.AddDays(6), RentalStatus.Cancelled));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var results = await service.GetUnavailableDatesAsync(
            data.Item.Id,
            start.AddDays(-1),
            start.AddDays(10));

        var range = Assert.Single(results);
        Assert.Equal(start, range.StartDateUtc);
    }

    private static RentalWorkflowService CreateService(AppDbContext context)
    {
        var itemRepository = new ItemRepository(context);
        var rentalRepository = new RentalRepository(context);
        var machine = new RentalStateMachine(
        [
            new RequestedState(), new ApprovedState(), new RejectedState(),
            new CancelledState(),
            new OutForRentState(), new OverdueState(), new ReturnedState(), new CompletedState()
        ]);
        return new RentalWorkflowService(rentalRepository, itemRepository, new UnitOfWork(context), machine);
    }

    private static async Task<(User Owner, User Borrower, User SecondBorrower, Item Item)> SeedUsersAndItemAsync(
        AppDbContext context)
    {
        var owner = new User { DisplayName = "Owner", Email = "owner@test.local", PasswordHash = "hash" };
        var borrower = new User { DisplayName = "Borrower", Email = "borrower@test.local", PasswordHash = "hash" };
        var secondBorrower = new User { DisplayName = "Second", Email = "second@test.local", PasswordHash = "hash" };
        var item = new Item
        {
            Owner = owner,
            Title = "Test drill",
            Description = "A drill used by the test suite.",
            DailyRate = 10m,
            Category = ItemCategory.Tools,
            Location = new Point(-3.1883, 55.9533) { SRID = 4326 }
        };
        context.AddRange(owner, borrower, secondBorrower, item);
        await context.SaveChangesAsync();
        return (owner, borrower, secondBorrower, item);
    }

    private static Rental CreateRental(
        (User Owner, User Borrower, User SecondBorrower, Item Item) data,
        DateTimeOffset start,
        RentalStatus status) => new()
    {
        Item = data.Item,
        Borrower = data.Borrower,
        StartDateUtc = start,
        EndDateUtc = start.AddDays(1),
        TotalPrice = 20m,
        Status = status
    };
}
