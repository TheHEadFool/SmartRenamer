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
    }

}