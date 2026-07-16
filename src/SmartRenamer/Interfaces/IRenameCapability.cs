using System.Collections.Generic;
using SmartRenamer.Models.Rename;

namespace SmartRenamer.Interfaces
{
    /// <summary>
    /// Implemented by every capability that can
    /// generate a rename preview.
    /// </summary>
    public interface IRenameCapability
    {
        /// <summary>
        /// Generates a preview of the proposed
        /// filename changes.
        /// </summary>
        List<RenamePreview> BuildPreview(
            IEnumerable<string> files,
            object request);
    }
}