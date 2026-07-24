using System.Windows;
using SmartRenamer.Infrastructure;

namespace SmartRenamer.Guide.Models
{
    /// <summary>
    /// Represents one item in the conversation timeline.
    /// A message may be plain text, or it may contain a rich UI card.
    /// </summary>
    public class GuideMessage
    {
        /// <summary>
        /// True if the message came from Guide.
        /// False if it came from the user.
        /// </summary>
        public bool IsGuide { get; set; }

        /// <summary>
        /// Display name of the speaker.
        /// Eventually this will come from the user's chosen Guide name.
        /// </summary>
        public string Speaker =>    IsGuide ? "Scout" : "You";

        /// <summary>
        /// Optional text shown in the conversation.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Optional rich content such as a WelcomeCard,
        /// FolderPickerCard, or ProjectSummaryCard.
        /// </summary>
        public FrameworkElement? Card { get; set; }

        /// <summary>
        /// Optional action text.
        /// </summary>
        public string? ActionText { get; set; }

        /// <summary>
        /// Optional action command.
        /// </summary>
        public RelayCommand? ActionCommand { get; set; }

        /// <summary>
        /// Optional payload associated with this message.
        /// </summary>
        public object? Payload { get; set; }
    }
}