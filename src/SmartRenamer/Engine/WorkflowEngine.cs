using System.Collections.Generic;
using SmartRenamer.Models;

namespace SmartRenamer.Engine
{
    public class WorkflowEngine
    {
        public void Execute(
            Pipeline pipeline,
            IEnumerable<FileContext> files)
        {
            foreach (FileContext file in files)
            {
                foreach (var step in pipeline.Steps)
                {
                    step.Execute(file);
                }
            }
        }
    }
}