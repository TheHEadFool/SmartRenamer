using System.Collections.ObjectModel;

namespace SmartRenamer.Guide.Models
{
    /// <summary>
    /// Stores the conversation timeline between the user and Guide.
    /// </summary>
    public class GuideConversation
    {
        public ObservableCollection<GuideMessage> Messages { get; }
            = new();

        public void AddMessage(GuideMessage message)
        {
            Messages.Add(message);
        }

        public void AddGuideMessage(string text)
        {
            Messages.Add(new GuideMessage
            {
                IsGuide = true,
                Text = text
            });
        }

        public void AddUserMessage(string text)
        {
            Messages.Add(new GuideMessage
            {
                IsGuide = false,
                Text = text
            });
        }
    }
}