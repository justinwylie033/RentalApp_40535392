using RentalApp.Contracts;

namespace RentalApp.Database.States;

public abstract class RentalState : IRentalState
{
    // Presentation point: each concrete state owns only its legal successors. Adding
    // a new workflow state is localised, demonstrating the Open/Closed Principle.
    private readonly HashSet<RentalStatus> _allowedTransitions;

    protected RentalState(params RentalStatus[] allowedTransitions)
    {
        _allowedTransitions = new HashSet<RentalStatus>(allowedTransitions);
    }

    public abstract RentalStatus Status { get; }

    public bool CanTransitionTo(RentalStatus nextStatus)
    {
        return _allowedTransitions.Contains(nextStatus);
    }
}

public sealed class RequestedState : RentalState
{
    public RequestedState()
        : base(RentalStatus.Approved, RentalStatus.Rejected)
    {
    }

    public override RentalStatus Status => RentalStatus.Requested;
}

public sealed class ApprovedState : RentalState
{
    public ApprovedState()
        : base(RentalStatus.OutForRent, RentalStatus.Rejected)
    {
    }

    public override RentalStatus Status => RentalStatus.Approved;
}

public sealed class RejectedState : RentalState
{
    public override RentalStatus Status => RentalStatus.Rejected;
}

public sealed class OutForRentState : RentalState
{
    public OutForRentState()
        : base(RentalStatus.Overdue, RentalStatus.Returned)
    {
    }

    public override RentalStatus Status => RentalStatus.OutForRent;
}

public sealed class OverdueState : RentalState
{
    public OverdueState()
        : base(RentalStatus.Returned)
    {
    }

    public override RentalStatus Status => RentalStatus.Overdue;
}

public sealed class ReturnedState : RentalState
{
    public ReturnedState()
        : base(RentalStatus.Completed)
    {
    }

    public override RentalStatus Status => RentalStatus.Returned;
}

public sealed class CompletedState : RentalState
{
    // Terminal states pass no successors to the base class.
    public override RentalStatus Status => RentalStatus.Completed;
}
