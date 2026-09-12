using System;

namespace SmartRenamer.Guide.Models
{
    /******************************************************************************
     * GuideInlineAction
     *
     * PURPOSE
     * -------
     * Represents a clickable portion of a conversational message.
     *
     * Scout may present a question as normal conversational text while making
     * a meaningful answer phrase clickable for users who prefer direct
     * interaction.
     *
     * Example:
     *
     *     Would you like me to [search inside nested folders]?
     *
     * The surrounding sentence remains ordinary conversation.
     * The bracketed phrase is an inline action.
     *
     * The action itself does not contain domain logic. It carries the
     * information necessary for the conversation layer to route the user's
     * selection through the existing action/discovery infrastructure.
     *
     * RESPONSIBILITIES
     * ----------------
     * • Identify the text that should be clickable.
     * • Identify the action/choice associated with that text.
     * • Provide the user-facing label for the resulting selection.
     *
     * NON-RESPONSIBILITIES
     * --------------------
     * • Executing an action.
     * • Understanding Ebook, Music, Photo, or other domain meaning.
     * • Rendering the action.
     *
     * The ConversationPanel is responsible for rendering.
     * The Conversation/Guide layer is responsible for routing the selection.
     ******************************************************************************/

    public sealed class GuideInlineAction
    {
        /// <summary>
        /// The exact text displayed as the clickable portion of the message.
        /// </summary>
        public string Text { get; init; } = string.Empty;

        /// <summary>
        /// Identifier of the action or discovery choice represented by this
        /// inline action.
        ///
        /// The meaning of this identifier belongs to the originating
        /// conversation/domain layer.
        /// </summary>
        public string ActionId { get; init; } = string.Empty;

        /// <summary>
        /// Optional domain context associated with the action.
        ///
        /// The Guide does not interpret this value.
        /// </summary>
        public string ContextId { get; init; } = string.Empty;

        /// <summary>
        /// Creates an inline conversation action.
        /// </summary>
        public GuideInlineAction(
            string text,
            string actionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            ArgumentException.ThrowIfNullOrWhiteSpace(actionId);

            Text = text;
            ActionId = actionId;
        }
    }
}