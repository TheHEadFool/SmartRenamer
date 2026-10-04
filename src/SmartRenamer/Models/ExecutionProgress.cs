using System;
using System.Collections.Generic;

namespace SmartRenamer.Models
{
    /// <summary>
    /// Reports progress for a Scout operation.
    ///
    /// Completed/Total represent the weighted pipeline work currently
    /// reported by the Observation Engine. The stage and collection fields
    /// provide the domain context needed to explain that progress honestly.
    /// </summary>
    public class ExecutionProgress
    {
        public int Completed { get; init; }

        public int Total { get; init; }

        public string CurrentFile { get; init; } = "";

        public string Status { get; init; } = "";

        public string Stage { get; init; } = "";

        public int StageCompleted { get; init; }

        public int StageTotal { get; init; }

        public int CollectionTotal { get; init; }

        public int CollectionCompleted { get; init; }

        public int CollectionProcessing { get; init; }

        public int CollectionWaiting { get; init; }

        public int CollectionPending { get; init; }

        /// <summary>
        /// Current per-item collection state. The list is a snapshot so
        /// progress consumers never depend on domain-specific collection types.
        /// </summary>
        public IReadOnlyList<ExecutionProgressItem> Items { get; init; } =
            Array.Empty<ExecutionProgressItem>();
    }
}
