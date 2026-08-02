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

        /// <summary>
        /// Existing Experts expose Specialists directly.
        /// Newer Experts may instead override Investigate().
        /// </summary>
        public abstract IReadOnlyList<ObservationSpecialist> Specialists { get; }

        /// <summary>
        /// Legacy observation model.
        /// By default the expert consults each of its specialists
        /// and gathers their findings.
        /// </summary>
        public virtual List<ExpertFinding> Observe(
            IReadOnlyList<FileContext> files)
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

        /// <summary>
        /// New Investigation-based entry point.
        /// Existing Experts continue to work because this
        /// simply delegates to the legacy Observe() method.
        /// New Experts should override this method instead.
        /// </summary>
        public virtual List<ExpertFinding> Investigate(
            IReadOnlyList<FileContext> files)
        {
            return Observe(files);
        }
    }
}