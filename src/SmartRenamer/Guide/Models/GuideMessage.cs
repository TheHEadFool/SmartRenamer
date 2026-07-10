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
        /// <summary>
        /// True if this message came from the Guide.
        /// False if it came from the Friend.
        /// </summary>
        public bool IsGuide { get; set; }

        /// <summary>
        /// Convenience property for the UI.
        /// </summary>
        public string Speaker => IsGuide ? "Guide" : "Friend";

        /// <summary>
        /// The text shown in the conversation.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Determines what kind of Guide Card (if any) accompanies this message.
        /// </summary>
        public GuideMessageType MessageType { get; set; }
            = GuideMessageType.Text;

        /// <summary>
        /// Optional text shown on a Guide Card button.
        /// </summary>
        public string? ActionText { get; set; }

        /// <summary>
        /// Optional command executed by a Guide Card.
        /// </summary>
        public RelayCommand? ActionCommand { get; set; }

        /// <summary>
        /// Optional data associated with the message.
        /// </summary>
        public object? Payload { get; set; }
    }
}