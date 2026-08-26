using System;

namespace Scout.Observations.Conversation
{
    /// <summary>
    /// =========================================================================
    /// CV_UserIntent
    /// =========================================================================
    ///
    /// Motto
    /// -------------------------------------------------------------------------
    /// "Understand what the user meant."
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Converts different forms of user input into a common conversation
    /// intent.
    ///
    /// The source of the input does not matter.
    ///
    ///     Typed text
    ///     Button / hotlink
    ///     Voice
    ///     Future UI controls
    ///
    /// All of them should eventually produce the same intent.
    ///
    /// Current Responsibilities
    /// -------------------------------------------------------------------------
    /// • Recognize approval.
    /// • Recognize research requests.
    /// • Recognize Review All.
    ///
    /// Future Responsibilities
    /// -------------------------------------------------------------------------
    /// • Interpret richer natural-language responses.
    /// • Interpret voice commands.
    /// • Preserve conversational context.
    /// • Resolve ambiguous responses using the current topic.
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Decide what Scout should do next.
    /// • Perform research.
    /// • Modify files.
    /// • Render the user interface.
    ///
    /// Those responsibilities belong to the Conversation Engine,
    /// Ebook Expert, Resources, and User Interface.
    /// =========================================================================
    /// </summary>
    public sealed class CV_UserIntent
    {
        /// <summary>
        /// The recognized kind of user intent.
        /// </summary>
        public CV_UserIntentType Type { get; private set; }
            = CV_UserIntentType.Unknown;

        /// <summary>
        /// The original user input that produced the intent.
        /// </summary>
        public string RawInput { get; private set; } = string.Empty;

        /// <summary>
        /// Interprets a user's typed response.
        /// </summary>
        public CV_UserIntent Interpret(
            string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            RawInput = input.Trim();

            if (string.IsNullOrWhiteSpace(RawInput))
            {
                Type = CV_UserIntentType.Unknown;
                return this;
            }

            string normalized =
                RawInput.ToLowerInvariant();

            if (IsApproval(normalized))
            {
                Type = CV_UserIntentType.Approve;
                return this;
            }

            if (IsResearchRequest(normalized))
            {
                Type = CV_UserIntentType.Research;
                return this;
            }

            if (IsReviewAllRequest(normalized))
            {
                Type = CV_UserIntentType.ReviewAll;
                return this;
            }

            Type = CV_UserIntentType.Unknown;
            return this;
        }

        /// <summary>
        /// Recognizes common affirmative responses.
        /// </summary>
        private static bool IsApproval(
            string input)
        {
            return
                input == "yes" ||
                input == "y" ||
                input == "yeah" ||
                input == "yep" ||
                input == "sure" ||
                input == "okay" ||
                input == "ok" ||
                input == "go ahead" ||
                input == "do it" ||
                input == "please do";
        }

        /// <summary>
        /// Recognizes requests to research information.
        /// </summary>
        private static bool IsResearchRequest(
            string input)
        {
            return
                input.Contains("research") ||
                input.Contains("look up") ||
                input.Contains("find") ||
                input.Contains("search");
        }

        /// <summary>
        /// Recognizes a request to review all recommendations.
        /// </summary>
        private static bool IsReviewAllRequest(
            string input)
        {
            return
                input == "review all" ||
                input.Contains("review everything") ||
                input.Contains("review all recommendations") ||
                input.Contains("show me everything");
        }
    }

    /// <summary>
    /// =========================================================================
    /// CV_UserIntentType
    /// =========================================================================
    /// </summary>
    public enum CV_UserIntentType
    {
        Unknown,

        Approve,

        Research,

        ReviewAll
    }
}