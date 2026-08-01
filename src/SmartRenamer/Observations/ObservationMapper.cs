using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Models.Analysis;

namespace SmartRenamer.Observations
{
    /// <summary>
    /// =========================================================================
    /// ObservationMapper
    /// =========================================================================
    ///
    /// Converts ExpertFindings into ProjectObservations that Scout's
    /// existing user interface already understands.
    ///
    /// =========================================================================
    /// </summary>
    public static class ObservationMapper
    {
        public static List<ProjectObservation> Map(
            IEnumerable<ExpertFinding> findings)
        {
            List<ProjectObservation> observations = new();

            foreach (ExpertFinding finding in findings)
            {
                observations.Add(new ProjectObservation
                {
                    Title = finding.Summary,
                    Description =
                        string.Join(
                            "\n",
                            finding.Evidence),

                    WhyItMatters =
                        string.Join(
                            "\n",
                            finding.Questions),

                    Severity =
                        ObservationSeverity.Information
                });
            }

            return observations;
        }
    }
}