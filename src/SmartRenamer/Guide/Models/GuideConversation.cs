using System.Collections.ObjectModel;

namespace SmartRenamer.Guide.Models
{
    public class GuideConversation
    {
        public ObservableCollection<GuideMessage> Messages { get; }
            = new();

        public void Add(GuideMessage message)
        {
            Messages.Add(message);
        }

        public void AddGuideMessage(string text)
        {
            Add(new GuideMessage
            {
                IsGuide = true,
                Text = text
            });
        }

        public void AddFriendMessage(string text)
        {
            Add(new GuideMessage
            {
                IsGuide = false,
                Text = text
            });
        }

        public void AddUserMessage(string text)
        {
            AddFriendMessage(text);
        }
    }
}