using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Quality
{
    internal class E_QualityBlock
    {
        public QualityReport Analyze(
        MetadataReport metadataReport)
        {
            QualityReport report = new();

            report.ExcellentMetadata =
    metadataReport.ExcellentMetadata;

            report.NeedsAttention =
                metadataReport.NeedsAttention;

            report.MissingCovers =
                metadataReport.MissingCovers;

            return report;
        }

    }
}
