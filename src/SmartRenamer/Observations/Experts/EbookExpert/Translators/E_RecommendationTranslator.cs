using Scout.Observations.Conversation;
using SmartRenamer.Observations;

namespace SmartRenamer.Observations.Experts.EbookExpert.Translators;

/// <summary>
/// =========================================================================
/// E_RecommendationTranslator
/// =========================================================================
///
/// Motto
/// -------------------------------------------------------------------------
/// "Translate ebook expertise into conversation."
///
/// Purpose
/// -------------------------------------------------------------------------
/// Converts Ebook ExpertFindings into user-friendly conversation
/// recommendations that Scout can discuss with the user.
///
/// Responsibilities
/// -------------------------------------------------------------------------
/// • Translate ExpertFindings into CV_Recommendations.
/// • Preserve supporting evidence.
/// • Preserve follow-up questions.
/// • Preserve the meaning of the Expert's finding.
///
/// This class does NOT
/// -------------------------------------------------------------------------
/// • Analyze files.
/// • Decide which recommendation is most important.
/// • Render the user interface.
/// • Decide which recommendation Scout discusses next.
///
/// Those responsibilities belong to the Ebook Expert,
/// Conversation Planner, and User Interface.
/// =========================================================================
/// </summary>
public sealed class E_RecommendationTranslator
{
    /// <summary>
    /// Converts a single ExpertFinding into a Conversation Recommendation.
    /// </summary>
    public CV_Recommendation Translate(
        ExpertFinding finding)
    {
        CV_Recommendation recommendation = new()
        {
            // The ExpertFinding summary becomes the conversational reason.
            Reason = finding.Summary,

            // The first follow-up question becomes the question Scout can
            // use when discussing this recommendation.
            Question = finding.Questions.Count > 0
                ? finding.Questions[0]
                : string.Empty
        };

        //---------------------------------------------------------
        // Preserve supporting evidence.
        //---------------------------------------------------------

        recommendation.Evidence.AddRange(
            finding.Evidence);

        return recommendation;
    }
}