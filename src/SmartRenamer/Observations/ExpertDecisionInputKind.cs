namespace SmartRenamer.Observations
{
    /// <summary>
    /// Describes whether a generic Expert decision option needs additional
    /// user input before the owning Expert can apply it.
    ///
    /// The enum describes a generic interaction capability. It does not know
    /// anything about Ebook organization or any other domain.
    /// </summary>
    public enum ExpertDecisionInputKind
    {
        None,
        Folder
    }
}
