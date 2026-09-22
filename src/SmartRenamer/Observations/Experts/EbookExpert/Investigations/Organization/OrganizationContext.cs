using System;
using System.Collections.Generic;
using Scout.Observations.Experts.EbookExpert.Investigations.Organization;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Organization
{
    /// <summary>
    /// =========================================================================
    /// OrganizationContext
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Holds the working information required by the Ebook Expert's
    /// organization stage.
    ///
    /// The context combines:
    ///
    ///     OrganizationReport
    ///         +
    ///     OrganizationOptions
    ///
    /// Repair handoffs will be added when the repair-to-organization boundary
    /// is established.
    ///
    /// This is Ebook Expert domain state.
    /// It is not part of the generic Scout Observation Framework.
    ///
    /// Lifetime
    /// -------------------------------------------------------------------------
    /// The context is working expedition data.
    ///
    /// It exists while the current Ebook organization process is active and
    /// may be released when that expedition is complete.
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Modify source files.
    /// • Copy or move files.
    /// • Create organization folders.
    /// • Execute an organization job.
    /// • Render the user interface.
    /// • Decide what the user wants.
    ///
    /// Those responsibilities belong to the appropriate Ebook Expert
    /// organization services and the Scout Conversation Framework.
    ///
    /// Safety Boundary
    /// -------------------------------------------------------------------------
    /// Organization operates from the collection information discovered by
    /// the Ebook Expert and the user's organization choices.
    ///
    /// The original source collection remains protected.
    /// =========================================================================
    /// </summary>
    internal sealed class OrganizationContext
    {
        //---------------------------------------------------------
        // Organization facts
        //---------------------------------------------------------

        /// <summary>
        /// Facts discovered by the Ebook Expert about the collection.
        /// </summary>
        public OrganizationReport Report { get; }

        //---------------------------------------------------------
        // User organization choices
        //---------------------------------------------------------

        /// <summary>
        /// The organization choices supplied by the user.
        ///
        /// These describe the intended organization structure and destination.
        /// They do not authorize filesystem changes by themselves.
        /// </summary>
        public OrganizationOptions Options { get; }

        //---------------------------------------------------------
        // Repair handoffs
        //---------------------------------------------------------

        /// <summary>
        /// Semantic results supplied by the Ebook Expert repair stage.
        ///
        /// These identify the protected original and the current working
        /// representation available to later organization stages.
        /// </summary>
        public IReadOnlyList<E_RepairHandoff> RepairHandoffs { get; }

        //---------------------------------------------------------
        // Organization books
        //---------------------------------------------------------

        /// <summary>
        /// Per-book organization information available to planning.
        ///
        /// These are passive snapshots. They do not perform filesystem work.
        /// </summary>
        public IReadOnlyList<OrganizationBook> Books { get; }

        //---------------------------------------------------------
        // Construction
        //---------------------------------------------------------

        /// <summary>
        /// Creates the working organization context for the current
        /// Ebook Expert expedition.
        /// </summary>
        public OrganizationContext(
            OrganizationReport report,
            OrganizationOptions options,
            IReadOnlyList<E_RepairHandoff>? repairHandoffs = null,
            IReadOnlyList<OrganizationBook>? books = null)
        {
            Report =
                report ??
                throw new ArgumentNullException(nameof(report));

            Options =
                options ??
                throw new ArgumentNullException(nameof(options));

            RepairHandoffs =
                repairHandoffs ??
                Array.Empty<E_RepairHandoff>();

            Books =
                books ??
                Array.Empty<OrganizationBook>();
        }
    }
}