using System.Collections.Generic;
using SmartRenamer.Interfaces;

namespace SmartRenamer.Models
{
    public class Pipeline
    {
        public string Name { get; set; } = "";

        public string Description { get; set; } = "";

        public List<IPipelineStep> Steps { get; } = new();
    }
}