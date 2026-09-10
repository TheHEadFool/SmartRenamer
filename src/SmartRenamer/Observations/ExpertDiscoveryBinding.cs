using System;
using SmartRenamer.Models;

namespace SmartRenamer.Observations
{
    /// <summary>
    /// Associates an Observation Expert with the generic discovery request
    /// supplied by that Expert.
    ///
    /// This association belongs to the Observation infrastructure. It does
    /// not interpret the meaning of the requested extensions.
    /// </summary>
    public sealed class ExpertDiscoveryBinding
    {
        /// <summary>
        /// The Expert that owns the discovery request.
        /// </summary>
        public ObservationExpert Expert { get; }

        /// <summary>
        /// The discovery request supplied by the Expert.
        /// </summary>
        public ExpertDiscoveryRequest Request { get; }

        /// <summary>
        /// Creates an Expert/request discovery binding.
        /// </summary>
        public ExpertDiscoveryBinding(
            ObservationExpert expert,
            ExpertDiscoveryRequest request)
        {
            Expert =
                expert ?? throw new ArgumentNullException(nameof(expert));

            Request =
                request ?? throw new ArgumentNullException(nameof(request));
        }
    }
}