using System;
using System.Collections.Generic;
using System.Linq;
using Scout.Observations.Experts.EbookExpert.Data;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants
{
    /// <summary>
    /// Performs Phase 1 metadata-evidence evaluation.
    ///
    /// This evaluator deliberately does not perform external research and does
    /// not infer a value from a filename or opening content. Those sources are
    /// preserved as identity evidence for the deeper Repair evaluation.
    ///
    /// Its first responsibility is to distinguish an observed value from a
    /// missing value or a direct conflict already present in independent,
    /// field-specific evidence.
    /// </summary>
    internal sealed class E_MetadataReconciliationEvaluator
    {
        public MetadataReconciliation Evaluate(MetadataRecord record)
        {
            MetadataReconciliation result = new();

            EvaluateField(result.Title, record.Metadata.Title, "Title", record.Evidence);
            EvaluateField(result.Author, record.Metadata.Author, "Author", record.Evidence);
            EvaluateField(result.Series, record.Metadata.Series, "Series", record.Evidence);
            EvaluateField(result.SeriesNumber, record.Metadata.SeriesNumber, "SeriesNumber", record.Evidence);

            foreach (MetadataEvidence evidence in record.Evidence)
            {
                if (string.Equals(evidence.Source, "Filename", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(evidence.Source, "Opening Content", StringComparison.OrdinalIgnoreCase))
                {
                    result.UninterpretedIdentityEvidence.Add(evidence);
                }
            }

            return result;
        }

        private static void EvaluateField(
            MetadataFieldReconciliation field,
            string observedValue,
            string fieldName,
            IReadOnlyList<MetadataEvidence> evidence)
        {
            field.ObservedValue = observedValue?.Trim() ?? "";

            // Source-folder and derived collection evidence are context, not
            // direct field-specific corroboration. They must remain available
            // to domain evaluators without automatically turning collection
            // context into a metadata conflict or repair candidate.
            //
            // Collection evidence is derived from other EPUB observations in
            // the same folder. Treating it as independent here would double-
            // count the same underlying observation and could make a folder
            // name appear to prove a Series value before a Series evaluator
            // has decided that it is actually relevant to this book.
            List<MetadataEvidence> fieldEvidence = evidence
                .Where(item =>
                    !string.Equals(item.Source, "EPUB Metadata", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(item.Source, "Source Folder", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(item.Source, "Collection", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.Field, fieldName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            field.Evidence.AddRange(fieldEvidence);

            List<string> independentValues = fieldEvidence
                .Select(item => item.Value?.Trim() ?? "")
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (string value in independentValues)
                field.CandidateValues.Add(value);

            if (string.IsNullOrWhiteSpace(field.ObservedValue))
            {
                field.State = MetadataFieldReconciliationState.Missing;
                return;
            }

            bool hasIndependentConflict = independentValues.Any(value =>
                !string.Equals(value, field.ObservedValue, StringComparison.OrdinalIgnoreCase));

            if (hasIndependentConflict)
            {
                field.State = MetadataFieldReconciliationState.Conflicting;
                return;
            }

            if (independentValues.Count > 0)
            {
                field.State = MetadataFieldReconciliationState.Supported;
                return;
            }

            field.State = MetadataFieldReconciliationState.ObservedOnly;
        }
    }
}
