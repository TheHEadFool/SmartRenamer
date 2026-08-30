using System;
using System.IO;
using SmartRenamer.Models;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair
{
    /// <summary>
    /// =========================================================================
    /// E_RepairWorkspace
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Provides a temporary working copy of an ebook for Ebook Expert repairs.
    ///
    /// Safety Boundary
    /// -------------------------------------------------------------------------
    /// The original ebook is never modified by this workspace.
    ///
    /// The workspace creates a separate physical copy that can be safely
    /// repaired and verified before the file is handed back to the workflow.
    ///
    /// The workspace knows nothing about Scout's final organization folder.
    ///
    /// =========================================================================
    /// </summary>
    internal sealed class E_RepairWorkspace
    {
        private readonly string _workspacePath;

        /// <summary>
        /// Creates a new temporary Ebook Expert repair workspace.
        /// </summary>
        public E_RepairWorkspace()
        {
            _workspacePath = Path.Combine(
                Path.GetTempPath(),
                "Scout",
                "EbookExpert",
                "Repair",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(_workspacePath);
        }

        /// <summary>
        /// Creates a working copy of the supplied ebook.
        ///
        /// The original file is never modified.
        /// </summary>
        public string CreateWorkingCopy(
            FileContext file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (string.IsNullOrWhiteSpace(file.CurrentFullPath))
                throw new ArgumentException(
                    "The ebook does not have a usable source path.",
                    nameof(file));

            if (!File.Exists(file.CurrentFullPath))
                throw new FileNotFoundException(
                    "The ebook could not be found.",
                    file.CurrentFullPath);

            string fileName =
                Path.GetFileName(file.CurrentFullPath);

            string workingPath =
                Path.Combine(
                    _workspacePath,
                    fileName);

            File.Copy(
                file.CurrentFullPath,
                workingPath,
                true);

            return workingPath;
        }

        /// <summary>
        /// Removes the temporary repair workspace.
        /// </summary>
        public void Cleanup()
        {
            if (!Directory.Exists(_workspacePath))
                return;

            try
            {
                Directory.Delete(
                    _workspacePath,
                    true);
            }
            catch (IOException)
            {
                // Temporary cleanup failure must not hide
                // the result of the repair operation.
            }
            catch (UnauthorizedAccessException)
            {
                // Temporary cleanup failure must not hide
                // the result of the repair operation.
            }
        }
    }
}