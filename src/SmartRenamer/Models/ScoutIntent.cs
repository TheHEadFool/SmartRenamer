namespace SmartRenamer.Models
{
    public class ScoutIntent
    {
        public string OriginalText { get; set; } = "";
        public string Action { get; set; } = "";
        public string FindText { get; set; } = "";
        public string ReplaceWith { get; set; } = "";
        public bool Recognized { get; set; }
    }
}