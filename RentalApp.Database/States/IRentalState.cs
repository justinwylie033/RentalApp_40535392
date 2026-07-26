using RentalApp.Contracts;

namespace RentalApp.Database.States;

public interface IRentalState
{
    RentalStatus Status { get; }
    bool CanTransitionTo(RentalStatus nextStatus);
}
