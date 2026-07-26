using RentalApp.Contracts;

namespace RentalApp.Database.States;

public sealed class RentalStateMachine
{
    // Presentation point: dependency injection supplies every state implementation.
    // The dictionary gives one state object per RentalStatus without a large if/else.
    private readonly IReadOnlyDictionary<RentalStatus, IRentalState> _states;

    public RentalStateMachine(IEnumerable<IRentalState> states)
    {
        _states = states.ToDictionary(state => state.Status);
    }

    public void EnsureValidTransition(RentalStatus currentStatus, RentalStatus nextStatus)
    {
        // The State Pattern validates domain progression independently of UI buttons.
        var stateExists = _states.TryGetValue(currentStatus, out var currentState);
        if (!stateExists || currentState is null)
        {
            throw new InvalidOperationException($"No state implementation exists for {currentStatus}.");
        }

        var transitionIsAllowed = currentState.CanTransitionTo(nextStatus);
        if (!transitionIsAllowed)
        {
            throw new InvalidOperationException(
                $"A rental cannot transition from {currentStatus} to {nextStatus}.");
        }
    }
}
