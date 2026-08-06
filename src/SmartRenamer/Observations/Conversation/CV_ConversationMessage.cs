using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scout.Observations.Conversation
{
    internal class CV_ConversationMessage
    {
    }
}
/// =========================================================================
/// CV_ConversationMessage
/// =========================================================================
///
/// Motto
/// -------------------------------------------------------------------------
/// "Capture one step of the conversation."
///
/// Purpose
/// -------------------------------------------------------------------------
/// Represents a single conversational exchange between Scout
/// and the user.
///
/// Future Responsibilities
/// -------------------------------------------------------------------------
/// • Record who spoke.
/// • Record what was said.
/// • Preserve conversational order.
/// • Associate messages with recommendations and evidence.
/// • Support future conversation history and review.
///
/// This class does NOT
/// -------------------------------------------------------------------------
/// • Decide what should be said.
/// • Interpret user intent.
/// • Analyze files.
///
/// Those responsibilities belong to the Conversation Planner
/// and UserIntent.
/// =========================================================================