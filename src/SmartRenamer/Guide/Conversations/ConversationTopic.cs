using System.Collections.Generic;
using SmartRenamer.Guide.Models;

namespace SmartRenamer.Guide.Conversations
{
    /// <summary>
    /// Base class for every conversation Guide can have.
    /// A conversation produces GuideMessages, which may
    /// be text, cards, prompts, or other rich content.
    /// </summary>
    public abstract class ConversationTopic
    {
        /// <summary>
        /// Friendly name of this conversation.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Called when the conversation begins.
        /// </summary>
        public abstract IEnumerable<GuideMessage> Begin();

        /// <summary>
        /// Called each time the user responds.
        /// </summary>
        public abstract IEnumerable<GuideMessage> Continue(string userResponse);

        /// <summary>
        /// True once this conversation has completed.
        /// </summary>
        public abstract bool IsComplete { get; }
    }
}