using RentalApp.Contracts;
using RentalApp.Database.States;

namespace RentalApp.Test.Services;

public sealed class RentalStateMachineTests
{
    // Presentation point: the same eight concrete states registered by DI are used
    // here, making this a focused unit test of the State Pattern itself.
    private readonly RentalStateMachine _machine = new(
    [
        new RequestedState(),
        new ApprovedState(),
        new RejectedState(),
        new CancelledState(),
        new OutForRentState(),
        new OverdueState(),
        new ReturnedState(),
        new CompletedState()
    ]);

    [Theory]
    // Each InlineData row is reported as a separate executed xUnit test case.
    [InlineData(RentalStatus.Requested, RentalStatus.Approved)]
    [InlineData(RentalStatus.Requested, RentalStatus.Rejected)]
    [InlineData(RentalStatus.Requested, RentalStatus.Cancelled)]
    [InlineData(RentalStatus.Approved, RentalStatus.Cancelled)]
    [InlineData(RentalStatus.Approved, RentalStatus.OutForRent)]
    [InlineData(RentalStatus.OutForRent, RentalStatus.Returned)]
    [InlineData(RentalStatus.OutForRent, RentalStatus.Overdue)]
    [InlineData(RentalStatus.Overdue, RentalStatus.Returned)]
    [InlineData(RentalStatus.Returned, RentalStatus.Completed)]
    public void EnsureValidTransition_AllowedTransition_DoesNotThrow(
        RentalStatus current,
        RentalStatus next)
    {
        var exception = Record.Exception(() => _machine.EnsureValidTransition(current, next));

        Assert.Null(exception);
    }

    [Theory]
    // Forbidden transitions are as important as the happy path because they prove
    // terminal and role-driven workflow states cannot be skipped.
    [InlineData(RentalStatus.Requested, RentalStatus.Completed)]
    [InlineData(RentalStatus.Rejected, RentalStatus.Approved)]
    [InlineData(RentalStatus.Cancelled, RentalStatus.Approved)]
    [InlineData(RentalStatus.Completed, RentalStatus.Requested)]
    [InlineData(RentalStatus.Returned, RentalStatus.Approved)]
    public void EnsureValidTransition_InvalidTransition_Throws(
        RentalStatus current,
        RentalStatus next)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => _machine.EnsureValidTransition(current, next));

        Assert.Contains(current.ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Contains(next.ToString(), exception.Message, StringComparison.Ordinal);
    }
}
