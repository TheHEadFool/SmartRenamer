using System;

namespace SmartRenamer.Capabilities.TextReplacement
{
    /// <summary>
    /// Converts natural language into a TextReplacementRequest.
    /// </summary>
    public class TextReplacementInterpreter
    {
        public bool TryInterpret(
            string conversation,
            out TextReplacementRequest request)
        {
            request = new TextReplacementRequest();

            if (string.IsNullOrWhiteSpace(conversation))
                return false;

            string text = conversation.Trim().ToLowerInvariant();

            //-------------------------------------------------
            // Remove underscores
            //-------------------------------------------------

            if (text.Contains("remove underscores") ||
                text.Contains("replace underscores") ||
                text.Contains("get rid of underscores"))
            {
                request.FindText = "_";
                request.ReplaceWith = " ";
                return true;
            }

            //-------------------------------------------------
            // Replace x with y
            //-------------------------------------------------

            const string keyword = "replace ";

            if (text.StartsWith(keyword))
            {
                string remainder = conversation.Substring(keyword.Length);

                int withIndex =
                    remainder.IndexOf(" with ",
                    StringComparison.OrdinalIgnoreCase);

                if (withIndex > 0)
                {
                    request.FindText =
                        remainder.Substring(0, withIndex);

                    request.ReplaceWith =
                        remainder.Substring(withIndex + 6);

                    return true;
                }
            }

            return false;
        }
    }
}