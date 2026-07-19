using SmartRenamer.Services.Intelligence.Capabilities;
using System.Collections.Generic;

namespace SmartRenamer.Services.Intelligence
{
    /// <summary>
    /// Registers every capability available to Scout.
    /// The IntelligenceEngine executes whatever is registered here.
    /// </summary>
    public static class CapabilityRegistry
    {
        public static IEnumerable<ICapability> Create()
        {
            return new List<ICapability>()
            {
                // Capabilities will be added here.
            };
        }
    }
}