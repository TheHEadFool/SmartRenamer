using System;
using System.Collections.Generic;

namespace SmartRenamer.Models.Rename
{
    /// <summary>
    /// Represents one completed rename operation.
    /// </summary>
    public class RenameTransaction
    {
        public DateTime Started { get; } = DateTime.Now;

        public List<RenameTransactionEntry> Entries { get; } = new();

        public int FilesRenamed => Entries.Count;
    }
}