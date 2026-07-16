using System.Collections.Generic;
using SmartRenamer.Guide.Models;
using SmartRenamer.Models.Settings;

namespace SmartRenamer.Guide.Conversations
{
    /// <summary>
    /// First-time onboarding conversation.
    /// </summary>
    public class WelcomeTopic : ConversationTopic
    {
        private readonly UserProfile profile;

        private bool complete;

        public WelcomeTopic(UserProfile profile)
        {
            this.profile = profile;
        }

        public override string Name => "Welcome";

        public override bool IsComplete => complete;

        public override IEnumerable<GuideMessage> Begin()
        {
            yield return new GuideMessage
            {
                IsGuide = true,
                Text =
@"👋 Welcome to Smart File Organizer.

I'm here to help organize your photos, movies, books, music, documents, downloads, and much more.

Before we begin...

What would you like me to call you?"
            };
        }

        public override IEnumerable<GuideMessage> Continue(string userResponse)
        {
            profile.UserName = userResponse.Trim();
            profile.HasCompletedWelcome = true;

            complete = true;

            yield return new GuideMessage
            {
                IsGuide = true,
                Text =
$@"It's nice to meet you, {profile.UserName}.

For now, you can simply call me Guide.

If you'd ever like to give me another name, just tell me.

For example:

'I'd like to call you Scout.'

or

'I'd like to call you Atlas.'

I'll happily answer to whatever name you choose.

Now...

What would you like to organize today?"
            };
        }
    }
}