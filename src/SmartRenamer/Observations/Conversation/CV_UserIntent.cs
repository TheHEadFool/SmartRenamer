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
    /// • Recognize user delegation of appropriate actions to Scout.
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
    /// • Decide whether an action is safe.
    /// • Perform research.
    /// • Modify files.
    /// • Render the user interface.
    ///
    /// Those responsibilities belong to the Conversation Engine,
    /// domain Experts, Resources, and User Interface.
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

            if (IsAutomaticActionAuthorizationRequest(normalized))
            {
                Type = CV_UserIntentType.AuthorizeAutomaticAction;
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

        /// <summary>
        /// Recognizes natural-language requests that delegate appropriate
        /// actions to Scout.
        ///
        /// This recognizes the meaning of the request rather than requiring
        /// one specific phrase.
        ///
        /// The intent only establishes that the user is delegating authority.
        /// It does NOT determine whether any particular action is safe or
        /// appropriate. That decision belongs to the applicable Expert.
        /// </summary>
        private static bool IsAutomaticActionAuthorizationRequest(
            string input)
        {
            bool containsDelegation =
                input.Contains("trust you") ||
                input.Contains("trust scout") ||
                input.Contains("you can") ||
                input.Contains("go ahead and") ||
                input.Contains("take care of") ||
                input.Contains("handle") ||
                input.Contains("do them all") ||
                input.Contains("fix them all") ||
                input.Contains("fix everything") ||
                input.Contains("do everything");

            bool containsAction =
                input.Contains("fix") ||
                input.Contains("repair") ||
                input.Contains("handle") ||
                input.Contains("take care of") ||
                input.Contains("do them") ||
                input.Contains("do everything") ||
                input.Contains("do all");

            return
                containsDelegation &&
                containsAction;
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

        ReviewAll,

        AuthorizeAutomaticAction
    }
}