using System;
namespace Scout.Expeditions.Safari.Animation;

/// <summary>
/// Directs the current expedition.
/// The director doesn't animate anything itself.
/// It simply changes the expedition state.
/// </summary>
public class ExpeditionDirector
{
    public ExpeditionState CurrentState { get; private set; } = ExpeditionState.Idle;

    public event Action<ExpeditionState>? StateChanged;

    public void BeginExpedition()
    {
        SetState(ExpeditionState.Working);
    }

    public void BeginReturnToCamp()
    {
        SetState(ExpeditionState.Returning);
    }

    public void CompleteExpedition()
    {
        SetState(ExpeditionState.Complete);
    }

    public void ReturnToIdle()
    {
        SetState(ExpeditionState.Idle);
    }

    private void SetState(ExpeditionState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        StateChanged?.Invoke(CurrentState);
    }
}