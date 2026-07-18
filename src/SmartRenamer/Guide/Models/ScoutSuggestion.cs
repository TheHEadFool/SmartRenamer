using SmartRenamer.Infrastructure;

namespace SmartRenamer.Guide.Models
{
    /// <summary>
    /// A recommendation Scout presents to the user.
    /// These become the buttons in the Suggestions panel.
    /// </summary>
    public class ScoutSuggestion
    {
        public string Id { get; set; } = "";

        public string Title { get; set; } = "";

        public string Description { get; set; } = "";

        public RelayCommand? Command { get; set; }

        public bool IsPrimary { get; set; }

        public bool IsEnabled { get; set; } = true;
    }
}