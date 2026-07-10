using System.Collections.Generic;
using SmartRenamer.Interfaces;
using SmartRenamer.Steps;

namespace SmartRenamer.Catalog
{
    public class StepCatalog
    {
        public IEnumerable<IPipelineStep> GetAvailableSteps()
        {
            return new IPipelineStep[]
            {
                new FindReplaceStep()
            };
        }
    }
}