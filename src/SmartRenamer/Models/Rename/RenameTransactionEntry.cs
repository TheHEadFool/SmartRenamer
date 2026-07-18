namespace SmartRenamer.Models.Rename
{
    /// <summary>
    /// Records a single successful rename operation.
    /// Used for Undo.
    /// </summary>
    public class RenameTransactionEntry
    {
        public string OriginalPath { get; init; } = string.Empty;

        public string NewPath { get; init; } = string.Empty;
    }
}