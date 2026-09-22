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
        private const string WorkspaceRootName =
            "Scout";

        private const string ActiveLockFileName =
            ".active";

        private readonly string _workspacePath;

        private FileStream? _sessionLock;

        /// <summary> 
        /// Creates a new temporary Ebook Expert repair workspace. 
        /// </summary> 
        public E_RepairWorkspace()
        {
            _workspacePath = Path.Combine(
                Path.GetTempPath(),
                WorkspaceRootName,
                "EbookExpert",
                "Repair",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(_workspacePath);

            string lockPath =
                Path.Combine(
                    _workspacePath,
                    ActiveLockFileName);

            _sessionLock =
                new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None);
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

            if (string.Equals(
                    Path.GetFullPath(file.CurrentFullPath),
                    Path.GetFullPath(workingPath),
                    StringComparison.OrdinalIgnoreCase))
            {
                return workingPath;
            }

            File.Copy(
                file.CurrentFullPath,
                workingPath,
                true);

            return workingPath;
        }

        /// <summary> 
        /// Removes abandoned Scout Ebook Expert repair sessions. 
        /// 
        /// This method only performs cleanup. It never resumes, retries, or 
        /// reconstructs a repair operation. 
        ///  
        /// </summary> 
        /// 
        /// This method only performs cleanup. It never resumes, retries, or 
        /// reconstructs a repair operation. 
        /// </summary> 
        public static void CleanupAbandonedWorkspaces()
        {
            string repairRoot =
                Path.Combine(
                    Path.GetTempPath(),
                    WorkspaceRootName,
                    "EbookExpert",
                    "Repair");

            if (!Directory.Exists(repairRoot))
                return;

            foreach (string workspacePath in
                     Directory.EnumerateDirectories(repairRoot))
            {
                string directoryName =
                    Path.GetFileName(workspacePath);

                //--------------------------------------------------------- 
                // Repair sessions created by Scout use GUID directory names. 
                // 
                // Ignore anything else that might have been placed in 
                // this folder. 
                //--------------------------------------------------------- 

                if (!Guid.TryParse(
                        directoryName,
                        out _))
                {
                    continue;
                }

                string lockPath =
                    Path.Combine(
                        workspacePath,
                        ActiveLockFileName);

                //--------------------------------------------------------- 
                // If the session has no lock file, it belongs to an older 
                // session format and is safe to treat as abandoned. 
                //--------------------------------------------------------- 

                bool abandoned = true;

                if (File.Exists(lockPath))
                {
                    try
                    {
                        //----------------------------------------------------- 
                        // If we can obtain the lock, no Scout process is 
                        // currently using this repair session. 
                        //----------------------------------------------------- 

                        using FileStream sessionLock =
                            new(
                                lockPath,
                                FileMode.Open,
                                FileAccess.ReadWrite,
                                FileShare.None);
                    }
                    catch (IOException)
                    {
                        //----------------------------------------------------- 
                        // Another Scout process still owns the lock. 
                        //----------------------------------------------------- 

                        abandoned = false;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        //----------------------------------------------------- 
                        // Treat an inaccessible lock as active rather than 
                        // risking deletion of another Scout session. 
                        //----------------------------------------------------- 

                        abandoned = false;
                    }
                }

                if (!abandoned)
                    continue;

                try
                {
                    Directory.Delete(
                        workspacePath,
                        recursive: true);
                }
                catch (IOException)
                {
                    // Cleanup is best-effort. 
                }
                catch (UnauthorizedAccessException)
                {
                    // Cleanup is best-effort. 
                }
            }
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