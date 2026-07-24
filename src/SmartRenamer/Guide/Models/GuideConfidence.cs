namespace SmartRenamer.Guide.Models
{
    /******************************************************************************
     * GuideConfidence
     *
     * Indicates how confident Scout is in an observation.
     *
     * Confidence is presented to the user to help establish trust through
     * transparency rather than certainty.
     ******************************************************************************/

    public enum GuideConfidence
    {
        NotApplicable,

        Low,

        Medium,

        High,

        Certain
    }
}