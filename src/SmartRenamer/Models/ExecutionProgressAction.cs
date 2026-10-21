using System;
using System.Collections.Generic;
namespace SmartRenamer.Models
{
    /// <summary>
    /// A generic user action offered for one collection item.
    ///
    /// The progress model does not understand ebook-domain meaning. It only
    /// carries the action identity and its stable domain context to the Guide.
    /// </summary>
    public sealed class ExecutionProgressAction
    {
        public string Id { get; init; } = "";

        public string Label { get; init; } = "";

        public string ActionId { get; init; } = "";

        public string ContextId { get; init; } = "";

        // True when this action is backed by an EbookExpert research decision
        // rather than a generic collection-progress action.
        public bool IsResearchOption { get; init; }

        public double Confidence { get; init; }

        public string Source { get; init; } = "";

        public string SuggestedUserInput { get; init; } = "";

        public IReadOnlyList<string> Evidence { get; init; } = Array.Empty<string>();
    }

}