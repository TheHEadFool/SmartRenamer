using System;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair;

/// <summary>
/// =========================================================================
/// E_RepairChange
/// =========================================================================
///
/// Represents one approved change that belongs to an Ebook repair plan.
///
/// A RepairChange describes:
///
///     WHAT is being repaired
///     WHAT the current value is
///     WHAT value has been approved
///     WHERE the proposed value came from
///     WHAT evidence supports it
///     HOW confident the Ebook Expert is
///     WHETHER the repair can be executed
///
/// It does not perform the repair.
///
/// Multiple RepairChange objects may belong to the same EPUB and are
/// accumulated in E_RepairPlan before the EPUB is physically written.
///
/// This allows one EPUB to receive multiple approved repairs in a single
/// execution operation.
///
/// This class does NOT
/// -------------------------------------------------------------------------
/// • Modify an EPUB.
/// • Create files or folders.
/// • Perform research.
/// • Decide whether a repair is appropriate.
/// • Interpret conversation.
/// • Approve a change.
///
/// Those responsibilities belong to the Ebook Expert repair architecture,
/// resources, Conversation Framework, and user.
/// =========================================================================
/// </summary>
internal sealed class E_RepairChange
{
    /// <summary>
    /// Identifies the type of repair represented by this change.
    ///
    /// Examples:
    ///     ISBN
    ///     Description
    ///     Publisher
    ///     Cover
    /// </summary>
    public string RepairType { get; }

    /// <summary>
    /// The value currently present in the EPUB.
    ///
    /// This may be null when the metadata field is completely missing.
    /// </summary>
    public object? CurrentValue { get; }

    /// <summary>
    /// The value explicitly approved for the repair.
    /// </summary>
    public object ApprovedValue { get; }

    /// <summary>
    /// Identifies where the approved value came from.
    ///
    /// Examples:
    ///     Local
    ///     Recovered
    ///     Inferred
    ///     Researched
    ///     UserProvided
    ///     Unknown
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Evidence supporting the proposed repair value.
    ///
    /// This is deliberately represented as text for now.
    /// The evidence model can become structured later without changing
    /// the repair-plan concept.
    /// </summary>
    public string Evidence { get; }

    /// <summary>
    /// Confidence in the proposed repair value.
    ///
    /// Expected range:
    ///     0.0 = no confidence
    ///     1.0 = complete confidence
    /// </summary>
    public double Confidence { get; }

    /// <summary>
    /// Indicates whether the repair is currently safe and possible to
    /// execute.
    /// </summary>
    public bool CanExecute { get; }

    /// <summary>
    /// Creates one approved repair change.
    /// </summary>
    public E_RepairChange(
        string repairType,
        object? currentValue,
        object approvedValue,
        string source,
        string evidence,
        double confidence,
        bool canExecute)
    {
        if (string.IsNullOrWhiteSpace(repairType))
            throw new ArgumentException(
                "A repair change requires a repair type.",
                nameof(repairType));

        if (approvedValue == null)
            throw new ArgumentNullException(nameof(approvedValue));

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException(
                "A repair change requires a source.",
                nameof(source));

        if (string.IsNullOrWhiteSpace(evidence))
            throw new ArgumentException(
                "A repair change requires supporting evidence.",
                nameof(evidence));

        if (confidence < 0.0 || confidence > 1.0)
            throw new ArgumentOutOfRangeException(
                nameof(confidence),
                "Repair confidence must be between 0.0 and 1.0.");

        RepairType = repairType;
        CurrentValue = currentValue;
        ApprovedValue = approvedValue;
        Source = source;
        Evidence = evidence;
        Confidence = confidence;
        CanExecute = canExecute;
    }
}