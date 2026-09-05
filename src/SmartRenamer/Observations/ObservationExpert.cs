using System;
using System.Collections.Generic;
using Scout.Observations.Conversation;
using SmartRenamer.Models;
using SmartRenamer.Observations.Specialists;

namespace SmartRenamer.Observations
{
    /// <summary>
    /// =========================================================================
    /// ObservationExpert
    /// =========================================================================
    ///
    /// Motto
    /// -------------------------------------------------------------------------
    /// "Observe. Understand. Explain."
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Represents a self-contained domain expert.
    ///
    /// An ObservationExpert owns everything required to understand its
    /// domain including Investigations, Consultants, Reports, Findings,
    /// Translators and Specialists.
    ///
    /// Scout communicates with Experts only through the ObservationEngine.
    /// The ObservationEngine coordinates investigations and asks each
    /// Expert to translate its own findings into conversation-ready
    /// recommendations.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Investigate files.
    /// • Produce ExpertFindings.
    /// • Translate findings into conversation recommendations.
    /// • Describe its domain.
    /// • Optionally execute domain-specific actions.
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Render the user interface.
    /// • Manage conversations.
    /// • Execute rename operations directly.
    ///
    /// =========================================================================
    ///
    /// Generation 2 Architecture
    /// -------------------------------------------------------------------------
    /// Every Expert is responsible for:
    ///
    ///     FileContext
    ///          ↓
    ///     Investigations
    ///          ↓
    ///     ExpertFindings
    ///          ↓
    ///     Recommendation Translator
    ///          ↓
    ///     CV_Recommendations
    ///          ↓
    ///     Domain Actions
    ///
    /// Scout never translates ExpertFindings.
    /// Each Expert owns its own translation.
    ///
    /// =========================================================================
    ///
    /// Migration Status
    /// -------------------------------------------------------------------------
    /// The EbookExpert is the reference implementation for Generation 2.
    ///
    /// Remaining Experts will be migrated individually to preserve their
    /// internal architecture and domain knowledge.
    ///
    /// TODO (Generation 2 Migration)
    /// -------------------------------------------------------------------------
    /// □ MusicExpert
    /// □ PhotoExpert
    /// □ VideoExpert
    /// □ DocumentExpert
    /// □ DownloadExpert
    /// □ SpreadsheetExpert
    /// □ PresentationExpert
    /// □ ArchiveExpert
    /// □ SoftwareProjectExpert
    ///
    /// =========================================================================
    /// </summary>
    public abstract class ObservationExpert
    {
        //---------------------------------------------------------
        // Identity
        //---------------------------------------------------------

        public abstract string Name { get; }

        public abstract string Summary { get; }

        public abstract string WhyItMatters { get; }

        public abstract IReadOnlyList<ObservationSpecialist> Specialists { get; }

        //---------------------------------------------------------
        // Expert Responsibilities
        //---------------------------------------------------------

        /// <summary>
        /// Performs this Expert's investigations and returns the
        /// findings discovered within its domain.
        /// </summary>
        public abstract List<ExpertFinding> Investigate(
            IReadOnlyList<FileContext> files);
        /// <summary>
        /// Gives the Expert an opportunity to initialize domain-specific
        /// project state before investigation begins.
        ///
        /// The default implementation does nothing.
        /// </summary>
        public virtual void BeginProject(
            string sourceFolderPath,
            IReadOnlyList<FileContext> files)
        {
        }

        /// <summary>
        /// Converts this Expert's findings into conversation-ready
        /// recommendations.
        ///
        /// Each Expert owns its own Recommendation Translator.
        /// </summary>
        public abstract List<CV_Recommendation> BuildRecommendations(
            IReadOnlyList<ExpertFinding> findings);

        //---------------------------------------------------------
        // Domain Actions
        //---------------------------------------------------------

        /// <summary>
        /// Executes a domain-specific action requested through the
        /// Conversation Framework.
        ///
        /// The default implementation deliberately does nothing.
        /// Experts that expose executable domain capabilities override
        /// this method.
        ///
        /// This keeps action knowledge inside the appropriate Expert
        /// rather than placing domain-specific logic in the generic
        /// Observation Framework or Conversation Framework.
        /// </summary>
        public virtual CV_ActionResult ExecuteAction(
            CV_ActionRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new CV_ActionResult
            {
                ActionId = request.ActionId,
                Success = false,
                Message =
                    $"Expert '{Name}' does not handle action '{request.ActionId}'."
            };
        }
    }
}