using System;
using System.Collections.Generic;
using System.Linq;
using Scout.Observations.Experts.EbookExpert.Investigations.Organization;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Organization
{
    /// <summary>
    /// =========================================================================
    /// OrganizationPathBuilder
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Converts the neutral organization evidence in OrganizationReport into
    /// the collection-level organization paths that the current Ebook Expert
    /// can actually support.
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • choose a path for the user.
    /// • modify OrganizationOptions.
    /// • communicate with Scout UI.
    /// • create an ExpertDecisionRequest.
    /// • perform filesystem work.
    /// • build an organization plan.
    ///
    /// It only answers:
    ///
    ///     "Given the metadata dimensions currently available,
    ///      which organization paths can we offer?"
    ///
    /// The returned paths are policy choices, not execution actions.
    /// =========================================================================
    /// </summary>
    internal sealed class OrganizationPathBuilder
    {
        public IReadOnlyList<OrganizationPathOption> Build(
            OrganizationReport report)
        {
            ArgumentNullException.ThrowIfNull(report);

            List<OrganizationPathOption> paths = new();

            bool hasTitle =
                report.AvailableDimensions.Contains(
                    OrganizationDimension.Title);

            bool hasAuthor =
                report.AvailableDimensions.Contains(
                    OrganizationDimension.Author);

            bool hasSeries =
                report.AvailableDimensions.Contains(
                    OrganizationDimension.Series);

            //---------------------------------------------------------
            // Author → Title
            //---------------------------------------------------------

            if (hasAuthor && hasTitle)
            {
                paths.Add(
                    new OrganizationPathOption(
                        "author-title",
                        "Author → Title",
                        "Books are grouped by author, with titles ordered within each author.",
                        new[]
                        {
                            OrganizationDimension.Author
                        },
                        OrganizationDimension.Title));
            }

            //---------------------------------------------------------
            // Author → Series → Title
            //---------------------------------------------------------

            if (hasAuthor && hasSeries && hasTitle)
            {
                paths.Add(
                    new OrganizationPathOption(
                        "author-series-title",
                        "Author → Series → Title",
                        "Books are grouped by author, then series, with titles ordered within each series.",
                        new[]
                        {
                            OrganizationDimension.Author,
                            OrganizationDimension.Series
                        },
                        OrganizationDimension.Title));
            }

            //---------------------------------------------------------
            // Series → Author → Title
            //---------------------------------------------------------

            if (hasSeries && hasAuthor && hasTitle)
            {
                paths.Add(
                    new OrganizationPathOption(
                        "series-author-title",
                        "Series → Author → Title",
                        "Books are grouped by series, then author, with titles ordered within each author.",
                        new[]
                        {
                            OrganizationDimension.Series,
                            OrganizationDimension.Author
                        },
                        OrganizationDimension.Title));
            }

            //---------------------------------------------------------
            // Series → Title
            //---------------------------------------------------------

            if (hasSeries && hasTitle)
            {
                paths.Add(
                    new OrganizationPathOption(
                        "series-title",
                        "Series → Title",
                        "Books are grouped by series, with titles ordered within each series.",
                        new[]
                        {
                            OrganizationDimension.Series
                        },
                        OrganizationDimension.Title));
            }

            return paths;
        }
    }
}