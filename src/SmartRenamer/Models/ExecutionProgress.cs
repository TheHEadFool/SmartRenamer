namespace SmartRenamer.Models
{
    public class ExecutionProgress
    {
        public int Completed { get; init; }

        public int Total { get; init; }

        public string CurrentFile { get; init; } = "";

        public string Status { get; init; } = "";
    }
}