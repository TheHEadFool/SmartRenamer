using Scout.Observations.Conversation;
using SmartRenamer.Observations;
using System;

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
/// • Preserve the identity of the original ExpertFinding.
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
///
/// =========================================================================
/// MIGRATION NOTE
/// =========================================================================
///
/// The application is currently transitioning from the legacy observation
/// system to the new Conversation Framework.
///
/// An ExpertFinding is the authoritative discovery.
///
/// That same discovery is represented downstream as both:
///
///     ExpertFinding
///          │
///          ├──────────────► ProjectObservation
///          │                    │
///          │                    └── Workspace button
///          │
///          └──────────────► CV_Recommendation
///                               │
///                               └── Conversation / Review All
///
/// The Id must therefore be preserved during translation.
///
/// This allows Review All to create a report containing links that point
/// back to the exact observation represented by the corresponding button
/// in the Workspace.
///
/// DO NOT generate a new Guid here.
///
/// The ExpertFinding already owns the identity of the discovery.
/// =========================================================================
/// </summary>
public sealed class E_RecommendationTranslator
{
    /// <summary>
    /// Converts a single ExpertFinding into a Conversation Recommendation.
    ///
    /// The recommendation preserves the identity and meaning of the
    /// original ExpertFinding.
    /// </summary>
    public CV_Recommendation Translate(
        ExpertFinding finding)
    {
        if (finding == null)
            throw new ArgumentNullException(nameof(finding));

        CV_Recommendation recommendation = new()
        {
            //---------------------------------------------------------
            // Preserve the identity of the original finding.
            //
            // This is critical for Review All. The corresponding
            // ProjectObservation uses the same Id, allowing a report
            // link to identify the matching Workspace observation.
            //---------------------------------------------------------

            Id = finding.Id,

            //---------------------------------------------------------
            // The ExpertFinding summary becomes the recommendation title.
            //---------------------------------------------------------

            Title = finding.Summary,

            //---------------------------------------------------------
            // The first follow-up question becomes the question Scout
            // can use when discussing this recommendation.
            //---------------------------------------------------------

            Question = finding.Questions.Count > 0
                ? finding.Questions[0]
                : string.Empty,

            //---------------------------------------------------------
            // The ExpertFinding summary becomes the conversational reason.
            //---------------------------------------------------------

            Reason = finding.Summary
        };

        //---------------------------------------------------------
        // Preserve supporting evidence.
        //
        // The Expert already determined what evidence supports the
        // finding. The Translator does not reinterpret it.
        //---------------------------------------------------------

        recommendation.Evidence.AddRange(
            finding.Evidence);

        return recommendation;
    }
}