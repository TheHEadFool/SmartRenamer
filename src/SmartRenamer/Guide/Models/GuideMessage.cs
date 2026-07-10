using SmartRenamer.Infrastructure;

namespace SmartRenamer.Guide.Models
{
    public enum GuideMessageType
    {
        Text,
        FolderPicker,
        WorkflowSuggestion,
        Warning,
        Success
    }

    public class GuideMessage
    {
        public bool IsGuide { get; set; }

        public string Text { get; set; } = "";

        public GuideMessageType MessageType { get; set; }
            = GuideMessageType.Text;

        public string? ActionText { get; set; }

        public RelayCommand? ActionCommand { get; set; }

        public object? Payload { get; set; }
    }
}