namespace SmartRenamer.Models
{
    /// <summary>
    /// Represents the current state of a Scout operation.
    /// </summary>
    public enum ScoutOperationState
    {
        Idle,
        Running,
        Paused,
        Cancelling,
        Cancelled,
        Completed,
        Failed
    }
}