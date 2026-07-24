using System.Collections.ObjectModel;

namespace SmartRenamer.Guide.Models
{
    /******************************************************************************
     * GuideConversation
     *
     * Scout Design Language (SDL-001)
     *
     * PURPOSE
     * -------
     * Represents an entire conversation between Scout and the user.
     *
     * A conversation is an ordered collection of GuideMessages that evolves as
     * Scout learns about the user's goals and makes recommendations.
     *
     * RESPONSIBILITIES
     * ----------------
     * • Maintain message order.
     * • Record conversation state.
     * • Provide helper methods for adding messages.
     *
     * NON-RESPONSIBILITIES
     * --------------------
     * • Rendering UI.
     * • Performing investigations.
     * • Executing recommendations.
     ******************************************************************************/

    public class GuideConversation
    {
        public ObservableCollection<GuideMessage> Messages { get; }
            = new();

        public ConversationState State { get; } = new();

        public void AddGuideMessage(string text)
        {
            Messages.Add(new GuideMessage
            {
                Speaker = GuideSpeaker.Guide,
                DisplayName = "Scout",
                Text = text
            });
        }

        public void AddUserMessage(string text)
        {
            Messages.Add(new GuideMessage
            {
                Speaker = GuideSpeaker.User,
                DisplayName = "You",
                Text = text
            });
        }

        public void Clear()
        {
            Messages.Clear();
        }
    }
}