using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scout.Observations.Conversation
{
    public sealed class CV_UserIntent
    {
    }
}
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
/// Represents Scout's understanding of the user's intent, regardless of
/// how the user expressed it.
///
/// Future Responsibilities
/// -------------------------------------------------------------------------
/// • Interpret typed responses.
/// • Interpret button selections.
/// • Interpret voice commands.
/// • Distinguish between explicit requests and implied intent.
/// • Preserve conversational context.
///
/// This class does NOT
/// -------------------------------------------------------------------------
/// • Decide what Scout should do next.
/// • Analyze files.
/// • Render the UI.
///
/// Those responsibilities belong to the Conversation Planner,
/// Experts, and the User Interface.
/// =========================================================================