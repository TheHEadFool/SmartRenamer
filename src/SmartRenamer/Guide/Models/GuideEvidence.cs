using System;

namespace SmartRenamer.Guide.Models
{
    /******************************************************************************
     * GuideEvidence
     *
     * Represents one piece of evidence supporting a Guide observation.
     *
     * Evidence allows Scout to explain why it reached a conclusion instead of
     * asking the user to trust it blindly.
     ******************************************************************************/

    public class GuideEvidence
    {
        /// <summary>
        /// Short description of the evidence.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Optional supporting value.
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// Optional source.
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Confidence associated with this piece of evidence.
        /// </summary>
        public GuideConfidence Confidence { get; set; } =
            GuideConfidence.High;

        public override string ToString()
        {
            return Description;
        }
    }
}
