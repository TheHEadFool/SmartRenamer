using SmartRenamer.Models;

namespace SmartRenamer.Services
{
    public class IntentInterpreter
    {
        public ScoutIntent Interpret(string text)
        {
            var intent = new ScoutIntent { OriginalText = text };

            if (text.ToLowerInvariant().Contains("remove underscores"))
            {
                intent.Recognized = true;
                intent.Action = "ReplaceText";
                intent.FindText = "_";
                intent.ReplaceWith = " ";
            }

            return intent;
        }
    }
}