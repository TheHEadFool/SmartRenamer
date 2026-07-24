using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.Specialists;

namespace SmartRenamer.Observations
{
    /// <summary>
    /// An ObservationExpert teaches Scout how to recognize one type of collection.
    /// </summary>
    public abstract class ObservationExpert
    {
        public abstract string Name { get; }

        public abstract string Summary { get; }

        public abstract string WhyItMatters { get; }

        public virtual int Confidence => 100;

        public abstract IReadOnlyList<ObservationSpecialist> Specialists { get; }

        /// <summary>
        /// Ask this expert to investigate the supplied files.
        /// By default the expert consults each of its specialists
        /// and gathers their findings.
        /// </summary>
        public virtual List<ExpertFinding> Observe(IReadOnlyList<FileContext> files)
        {
            List<ExpertFinding> findings = new();

            foreach (ObservationSpecialist specialist in Specialists)
            {
                ExpertFinding finding = specialist.Observe(files);

                if (finding.FoundSomething)
                {
                    findings.Add(finding);
                }
            }

            return findings;
        }
    }
}