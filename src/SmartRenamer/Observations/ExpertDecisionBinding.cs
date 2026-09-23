using System;

namespace SmartRenamer.Observations
{
    /// <summary>
    /// Associates a generic decision request with the Expert that owns it.
    ///
    /// The binding exists only so the generic infrastructure can route
    /// the user's selected option back to the correct Expert.
    /// </summary>
    public sealed class ExpertDecisionBinding
    {
        public ExpertDecisionBinding(
            ObservationExpert expert,
            ExpertDecisionRequest request)
        {
            ArgumentNullException.ThrowIfNull(expert);
            ArgumentNullException.ThrowIfNull(request);

            Expert = expert;
            Request = request;
        }

        /// <summary>
        /// The Expert that owns the decision.
        /// </summary>
        public ObservationExpert Expert { get; }

        /// <summary>
        /// The decision request supplied by that Expert.
        /// </summary>
        public ExpertDecisionRequest Request { get; }
    }
}