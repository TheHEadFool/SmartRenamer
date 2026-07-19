using SmartRenamer.Models;
using SmartRenamer.Services.Intelligence.Capabilities;
using System.Collections.Generic;

namespace SmartRenamer.Services.Intelligence
{
    /// <summary>
    /// Runs all registered capabilities.
    /// Capabilities discover facts only.
    /// They never make planning decisions.
    /// </summary>
    public class IntelligenceEngine
    {
        private readonly List<ICapability> capabilities = new();

        public IntelligenceEngine()
        {
            // Capabilities will be registered here.
        }

        public void Register(ICapability capability)
        {
            capabilities.Add(capability);
        }

        public void Analyze(ProjectContext context)
        {
            foreach (ICapability capability in capabilities)
            {
                capability.Analyze(context);
            }
        }
    }
}