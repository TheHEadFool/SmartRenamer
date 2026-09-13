using System;
using System.Collections.Generic;
using System.IO;
using SmartRenamer.Models;

namespace SmartRenamer.Services
{
    /// <summary>
    /// =========================================================================
    /// WorkingCopyService
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Establishes the protected working representation of a Scout project.
    ///
    /// Safety Boundary
    /// -------------------------------------------------------------------------
    /// OriginalFullPath remains the permanent source identity.
    /// CurrentFullPath is changed to a Scout-owned working copy before the
    /// project enters active analysis or repair.
    ///
    /// This service does not understand EPUBs or any other file type. It only
    /// establishes safe working copies and preserves the source-relative
    /// location needed by later organization work.
    /// =========================================================================
    /// </summary>
    internal sealed class WorkingCopyService
    {
        private const string WorkspaceRootName = "Scout";
        private const string WorkingRootName = "Working";

        private readonly string _workspacePath;

        public WorkingCopyService()
        {
            _workspacePath = Path.Combine(
                Path.GetTempPath(),
                WorkspaceRootName,
                WorkingRootName,
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(_workspacePath);
        }

        /// <summary>
        /// Ensures every file has a Scout-owned working copy.
        ///
        /// Existing working copies are retained so a later workflow pass or
        /// re-observation continues working with the same physical files.
        /// </summary>
        public void EnsureWorkingCopies(
            IEnumerable<FileContext> files,
            string sourceFolderPath)
        {
            if (files == null)
                throw new ArgumentNullException(nameof(files));

            if (string.IsNullOrWhiteSpace(sourceFolderPath))
                throw new ArgumentException(
                    "The source folder path cannot be empty.",
                    nameof(sourceFolderPath));

            foreach (FileContext file in files)
            {
                if (file == null)
                    continue;

                EnsureWorkingCopy(
                    file,
                    sourceFolderPath);
            }
        }

        private void EnsureWorkingCopy(
            FileContext file,
            string sourceFolderPath)
        {
            if (string.IsNullOrWhiteSpace(file.OriginalFullPath))
                return;

            if (!File.Exists(file.OriginalFullPath))
                return;

            if (!string.IsNullOrWhiteSpace(file.CurrentFullPath) &&
                !string.Equals(
                    file.CurrentFullPath,
                    file.OriginalFullPath,
                    StringComparison.OrdinalIgnoreCase) &&
                File.Exists(file.CurrentFullPath))
            {
                return;
            }

            string relativePath =
                Path.GetRelativePath(
                    sourceFolderPath,
                    file.OriginalFullPath);

            if (relativePath.StartsWith(".." + Path.DirectorySeparatorChar,
                    StringComparison.Ordinal) ||
                string.Equals(relativePath, "..", StringComparison.Ordinal))
            {
                relativePath = Path.GetFileName(
                    file.OriginalFullPath);
            }

            string workingPath =
                Path.Combine(
                    _workspacePath,
                    relativePath);

            string? workingDirectory =
                Path.GetDirectoryName(workingPath);

            if (!string.IsNullOrWhiteSpace(workingDirectory))
                Directory.CreateDirectory(workingDirectory);

            File.Copy(
                file.OriginalFullPath,
                workingPath,
                overwrite: true);

            file.CurrentFullPath = workingPath;
            file.CurrentName = Path.GetFileName(workingPath);
        }
    }
}
