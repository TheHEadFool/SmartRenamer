using System;

namespace SmartRenamer.Guide.Models
{
    /******************************************************************************
     * GuideMessage
     *
     * Scout Design Language (SDL-001)
     *
     * PURPOSE
     * -------
     * Represents a single conversational item exchanged between Scout and the
     * user.
     *
     * GuideMessage is the fundamental unit of communication within the Guide
     * subsystem. Every conversation is composed of one or more GuideMessages
     * arranged in chronological order.
     *
     * A GuideMessage intentionally contains no business logic and no knowledge
     * of how it will be rendered. Expeditions decide how messages are displayed.
     *
     * RESPONSIBILITIES
     * ----------------
     * • Represent one conversational exchange.
     * • Identify the speaker.
     * • Store the message text.
     * • Carry optional contextual information.
     * • Record when the message was created.
     *
     * NON-RESPONSIBILITIES
     * --------------------
     * • Performing analysis.
     * • Rendering UI.
     * • Executing commands.
     * • Accessing the file system.
     *
     * FUTURE EVOLUTION
     * ----------------
     * Future message types such as Welcome Cards, Folder Pickers, Project
     * Summaries, Progress Cards, and Rich Guide Cards should extend the Guide
     * conversation without changing its underlying philosophy.
     *
     * RELATED DOCUMENTS
     * -----------------
     * SDL-001  Scout Design Language
     * ADR-011  Expedition Architecture
     *
     * HISTORY
     * -------
     * P003 - Initial implementation.
     ******************************************************************************/

    public class GuideMessage
    {
        /// <summary>
        /// Backward compatibility with the original conversation model.
        /// This property will eventually be removed.
        /// </summary>
        public bool IsGuide
        {
            get => Speaker == GuideSpeaker.Guide;
            set => Speaker = value
                ? GuideSpeaker.Guide
                : GuideSpeaker.User;
        }
        /// <summary>
        /// Identifies who produced this message.
        /// </summary>
        public GuideSpeaker Speaker { get; set; } = GuideSpeaker.Guide;

        /// <summary>
        /// Name displayed for the speaker.
        /// Expeditions may customize this value.
        /// </summary>
        public string DisplayName { get; set; } = "Scout";

        /// <summary>
        /// The primary conversational text.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Temporary compatibility property.
        /// Eventually this will become a strongly typed content model.
        /// </summary>
        public object? Card { get; set; }

        /// <summary>
        /// Time the message was created.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// Optional contextual data associated with the message.
        /// This may contain domain objects, observations, suggestions,
        /// or other information understood by the Guide.
        /// </summary>
        public object? Payload { get; set; }

        /// <summary>
        /// Indicates whether this message contains additional contextual data.
        /// </summary>
        public bool HasPayload => Payload != null;

        /// <summary>
        /// Returns a readable representation for debugging.
        /// </summary>
        public override string ToString()
        {
            return $"{DisplayName}: {Text}";
        }
    }
}