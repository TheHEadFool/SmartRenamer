namespace SmartRenamer.Guide.Models
{
    /******************************************************************************
     * GuideSeverity
     *
     * Represents the relative importance of a GuideMessage.
     *
     * Severity affects presentation, not behavior.
     ******************************************************************************/

    public enum GuideSeverity
    {
        Information,

        Success,

        Suggestion,

        Warning,

        Attention
    }
}