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
    /// • Describe its domain.
    /// • Optionally describe the discovery information it needs.
    /// • Investigate files.
    /// • Produce ExpertFindings.
    /// • Translate findings into conversation recommendations.
    /// • Optionally execute domain-specific actions.
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Render the user interface.
    /// • Manage conversations.
    /// • Execute rename operations directly.
    /// • Know the discovery rules of any particular domain.
    ///
    /// =========================================================================
    ///
    /// Generation 2 Architecture
    /// -------------------------------------------------------------------------
    /// Every Expert is responsible for:
    ///
    ///     Discovery Requirements
    ///          ↓
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
    /// The discovery capability is deliberately optional.
    ///
    /// Scout infrastructure may ask an Expert whether it provides discovery
    /// requirements. An Expert that does not yet participate in the discovery
    /// contract may return null and continue using the existing workflow.
    ///
    /// IMPORTANT
    /// -------------------------------------------------------------------------
    /// The discovery capability describes what an Expert wants Scout to make
    /// available as candidates. It does NOT determine whether a file actually
    /// belongs to the Expert's domain.
    ///
    /// Domain classification remains the responsibility of the Expert.
    ///
    /// For example, EbookExpert may eventually request PDFs, TXT files,
    /// DOC/DOCX files, EPUBs and other ebook candidates. EbookExpert, not
    /// FolderScanner, determines which of those candidates are actually books.
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
    /// =========================================================================
    /// </summary>
    public abstract class ObservationExpert
    {
        //---------------------------------------------------------
        // Discovery Capability
        //---------------------------------------------------------

        /// <summary>
        /// Describes the discovery requirements requested by this Expert.
        ///
        /// The default implementation returns null because discovery support
        /// is being introduced incrementally. Experts may override this
        /// property when they are ready to participate in the Expert-owned
        /// discovery contract.
        ///
        /// The request describes candidate discovery requirements only.
        /// It does not classify files as belonging to the Expert's domain.
        /// </summary>
        public virtual ExpertDiscoveryRequest? DiscoveryRequest =>
            null;

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

        //---------------------------------------------------------
        // Re-observation Completion
        //---------------------------------------------------------

        /// <summary>
        /// Gives the Expert an opportunity to advance any domain-specific
        /// workflow after a successful re-observation pass.
        ///
        /// The generic Observation Framework does not know what "complete"
        /// means for any particular domain.
        ///
        /// The default implementation deliberately does nothing.
        /// Domain Experts may override this method when they own an
        /// expedition or other workflow that must advance only after
        /// the repaired state has been observed again.
        /// </summary>
        public virtual bool CompleteCurrentIfComplete()
        {
            return false;
        }

        //---------------------------------------------------------
        // Recommendations
        //---------------------------------------------------------

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

    /// <summary>
    /// Describes the generic discovery requirements requested by an Expert.
    ///
    /// This is a Scout infrastructure contract, not a domain classification
    /// system. The actual meaning of candidate files remains with the Expert.
    ///
    /// The contract will be expanded only as the discovery architecture
    /// establishes additional generic requirements.
    /// </summary>
    public sealed class ExpertDiscoveryRequest
    {
        /// <summary>
        /// File extensions that Scout may use as candidate discovery hints.
        ///
        /// These are NOT declarations that files with these extensions belong
        /// to the Expert's domain.
        ///
        /// Domain Experts remain responsible for classification.
        /// </summary>
        public IReadOnlyList<string> CandidateExtensions { get; init; } =
            Array.Empty<string>();
    }
}