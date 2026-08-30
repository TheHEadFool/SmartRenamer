using System;
using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair;

/// <summary>
/// =========================================================================
/// E_RepairPlan
/// =========================================================================
///
/// Represents the complete set of approved repairs for ONE EPUB.
///
/// A single EPUB may require multiple repairs:
///
///     ISBN
///     Description
///     Publisher
///     Cover
///     etc.
///
/// Those repairs are accumulated in this plan before the EPUB is physically
/// written.
///
/// This is intentional.
///
/// The repair system must NOT create a separate partially repaired EPUB for
/// every individual change. The plan represents the complete work that will
/// eventually be applied to one repaired copy.
///
/// This class does NOT
/// -------------------------------------------------------------------------
/// • Modify an EPUB.
/// • Create files or folders.
/// • Perform research.
/// • Decide which repairs are appropriate.
/// • Approve repairs.
/// • Execute repairs.
///
/// It is simply the domain object that holds the approved repair work.
/// =========================================================================
/// </summary>
internal sealed class E_RepairPlan
{
    /// <summary>
    /// The original EPUB that this repair plan belongs to.
    ///
    /// This identifies the source file that must remain untouched.
    /// </summary>
    public string SourcePath { get; }

    /// <summary>
    /// The approved changes that will eventually be applied to ONE repaired
    /// copy of the source EPUB.
    /// </summary>
    public List<E_RepairChange> Changes { get; } = new();

    /// <summary>
    /// Creates a repair plan for one source EPUB.
    /// </summary>
    public E_RepairPlan(
        string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException(
                "A repair plan requires a source EPUB path.",
                nameof(sourcePath));

        SourcePath = sourcePath;
    }

    /// <summary>
    /// Adds one approved repair to this EPUB's repair plan.
    /// </summary>
    public void AddChange(
        E_RepairChange change)
    {
        if (change == null)
            throw new ArgumentNullException(nameof(change));

        Changes.Add(change);
    }
}