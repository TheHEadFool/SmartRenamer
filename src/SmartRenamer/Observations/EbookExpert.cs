using System;
using System.Collections.Generic;
using Scout.Observations.Conversation;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Action;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations;
using SmartRenamer.Observations.Experts.EbookExpert.Translators;
using SmartRenamer.Observations.Specialists;

namespace SmartRenamer.Observations

// Begin namespace
{
    // =========================================================================
    // PROJECT STATUS
    // =========================================================================
    //
    // WHY THIS CLASS EXISTS
    // -------------------------------------------------------------------------
    // The EbookExpert is Scout's reference implementation of a complete
    // Observation Expert. It demonstrates the architecture that every future
    // Expert should follow.
    //
    // It coordinates Investigations, produces ExpertFindings, and translates
    // those findings into conversation-ready recommendations that Scout can
    // present to the user.
    //
    // CURRENT MILESTONE
    // -------------------------------------------------------------------------
    // Display Ebook Expert recommendations in the existing UI.
    //
    // CURRENT STATUS
    // -------------------------------------------------------------------------
    // ✓ Observation architecture complete
    // ✓ Investigation pipeline complete
    // ✓ Report pipeline complete
    // ✓ Consultant pipeline complete
    // ✓ ExpertFinding pipeline complete
    // ✓ Recommendation translation implemented
    // ☐ Recommendation pipeline connected to existing UI
    // ☐ Conversation integration complete
    //
    // CURRENTLY DRIVING
    // -------------------------------------------------------------------------
    // The Recommendation panel (left side of the UI).
    //
    // The information produced here will determine:
    //
    // • Which recommendation buttons appear.
    // • Which actions Scout can perform immediately.
    // • Which conversation topics Scout can introduce.
    //
    // Recommendation buttons and Scout's conversation always represent the
    // same underlying understanding of the user's project.
    //
    // DO NOT CHANGE UNTIL
    // -------------------------------------------------------------------------
    // The Ebook Expert is successfully driving the Recommendation panel.
    //
    // Avoid adding new architecture or renaming classes until the current UI
    // demonstrates what information the Expert already provides.
    //
    // EXPERT FACTORY
    // -------------------------------------------------------------------------
    // YES
    //
    // This class is intended to become the template for future Experts.
    //
    // When complete, Scout should be able to generate an Expert with this
    // structure from an interview and a ChatGPT Knowledge Package.
    //
    // NEXT STEP
    // -------------------------------------------------------------------------
    // Connect the translated recommendations produced by this Expert to the
    // existing Recommendation panel without replacing the current UI.
    // =========================================================================
    public sealed class EbookExpert

    // Begin EbookExpert
        : ObservationExpert
    {
        //---------------------------------------------------------
        // Investigations
        //---------------------------------------------------------

        private readonly E_MetadataInvestigation _metadataInvestigation = new();

        private readonly E_ContentsInvestigation _contentsInvestigation = new();

        private readonly E_OrganizationInvestigation _organizationInvestigation = new();

        private readonly E_DuplicateInvestigation _duplicateInvestigation = new();

        private readonly E_QualityInvestigation _qualityInvestigation = new();

        private readonly E_RepairInvestigation _repairInvestigation = new();

        private readonly E_EnrichmentInvestigation _enrichmentInvestigation = new();

        private readonly E_CoverInvestigation _coverInvestigation = new();

        //---------------------------------------------------------
        // Domain Action Dispatcher
        //---------------------------------------------------------
        //
        // The dispatcher translates generic Conversation Framework
        // action requests into Ebook Expert domain operations.
        //
        // The dispatcher does not perform the investigation itself.
        // It uses the existing investigations and domain services owned
        // by this Expert.
        //---------------------------------------------------------

        private readonly E_ActionDispatcher _actionDispatcher = new();

        //---------------------------------------------------------
        // Legacy Specialists
        //---------------------------------------------------------

        private static readonly IReadOnlyList<ObservationSpecialist> _specialists =
        [
            new E_EbookMetadataSpecialist()
        ];

        public override IReadOnlyList<ObservationSpecialist> Specialists =>
            _specialists;

        //---------------------------------------------------------

        public override string Name =>
            "eBook Library";

        public override string Summary =>
            "I noticed what appears to be a collection of ebooks.";

        public override string WhyItMatters =>
            "Keeping ebooks organized by author, series, or subject makes your library easier to browse and enjoy.";

        /// <summary>
        /// Initializes Ebook Expert project-specific repair state.
        /// </summary>
        public override void BeginProject(
            string sourceFolderPath,
            IReadOnlyList<FileContext> files)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceFolderPath);
            ArgumentNullException.ThrowIfNull(files);

            _repairInvestigation.BeginExpedition(
                sourceFolderPath,
                files);
        }

        /// <summary>
        /// Initializes Ebook Expert project-specific repair state.
        /// </summary>
        

        /// <summary>
        /// =========================================================================
        /// Generation 2 Entry Point
        /// =========================================================================
        /// </summary>
        public override List<ExpertFinding> Investigate(
            IReadOnlyList<FileContext> files)

        // Begin Investigate()
        {
            List<ExpertFinding> findings = new();

            //---------------------------------------------------------
            // Acquire metadata once.
            //---------------------------------------------------------

            MetadataReport metadataReport =
                _metadataInvestigation.Investigate(files);

            //---------------------------------------------------------
            // Metadata ExpertFindings
            //---------------------------------------------------------
            //
            // Metadata research is shared with downstream Investigations,
            // but the Metadata Consultant also produces findings that belong
            // in the Ebook Expert's overall understanding.
            //
            //---------------------------------------------------------

            findings.AddRange(
                _metadataInvestigation.Findings);

            //---------------------------------------------------------
            // Completed Generation 2 Investigations
            //---------------------------------------------------------

            findings.AddRange(
                _contentsInvestigation.Investigate(
                    metadataReport));

            findings.AddRange(
                _organizationInvestigation.Investigate(
                    metadataReport));

            findings.AddRange(
                _duplicateInvestigation.Investigate(
                    files));

            findings.AddRange(
                _qualityInvestigation.Investigate(
                    metadataReport));

            findings.AddRange(
                _repairInvestigation.Investigate(
                    metadataReport));

            findings.AddRange(
                _coverInvestigation.Investigate(
                    metadataReport));

            //---------------------------------------------------------
            // Enrichment Investigation
            //---------------------------------------------------------

            findings.AddRange(
                _enrichmentInvestigation.Investigate(
                    metadataReport));

            return findings;

        } // End Investigate()

        /// <summary>
        /// Translates the Expert's findings into conversation-ready
        /// recommendations.
        /// </summary>
        public override List<CV_Recommendation> BuildRecommendations(
            IReadOnlyList<ExpertFinding> findings)
        {
            E_RecommendationTranslator translator = new();

            List<CV_Recommendation> recommendations = new();

            foreach (ExpertFinding finding in findings)
            {
                recommendations.Add(
                    translator.Translate(finding));
            }

            return recommendations;
        }

        /// <summary>
        /// Executes an Ebook Expert action requested through the
        /// Conversation Framework.
        ///
        /// The action is routed through the Ebook Expert's Action Dispatcher.
        ///
        /// The Repair Investigation is intentionally passed through here
        /// rather than recreated. This preserves the RepairOpportunity
        /// objects discovered during the most recent investigation.
        ///
        /// Current supported action:
        ///
        ///     ResearchMissingIsbn
        ///
        /// Future Ebook actions can use this same gateway:
        ///
        ///     ResearchMissingCover
        ///     ResearchMissingSummary
        ///     RepairMissingMetadata
        /// </summary>
        public override CV_ActionResult ExecuteAction(
            CV_ActionRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return _actionDispatcher.Execute(
                request,
                _repairInvestigation.RepairOpportunities);
        }

    } // End EbookExpert

} // End namespace