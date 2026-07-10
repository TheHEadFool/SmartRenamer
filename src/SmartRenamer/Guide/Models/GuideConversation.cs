using System.Collections.ObjectModel;

namespace SmartRenamer.Guide.Models
{
    public class GuideConversation
    {
        public ObservableCollection<GuideChatMessage> Messages { get; }
            = new();

        public void AddGuideMessage(string text)
        {
            Messages.Add(new GuideChatMessage
            {
                IsGuide = true,
                Text = text
            });
        }

        public void AddUserMessage(string text)
        {
            Messages.Add(new GuideChatMessage
            {
                IsGuide = false,
                Text = text
            });
        }
    }
}