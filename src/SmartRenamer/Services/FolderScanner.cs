using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SmartRenamer.Models;
using SmartRenamer.Observations;

namespace SmartRenamer.Services
{
    /// <summary>
    /// Scans folders and creates the shared FileContext collection used by Scout.
    ///
    /// The scanner is deliberately domain-neutral. It does not know what an
    /// EPUB, music file, photograph, document, or any other file type means.
    /// Domain Experts provide candidate extension hints through
    /// ExpertDiscoveryRequest.
    /// </summary>
    public class FolderScanner
    {
        private static readonly string[] ImageExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".bmp",
            ".tif",
            ".tiff",
            ".heic",
            ".webp"
        };

        private static readonly string[] RawExtensions =
        {
            ".cr2",
            ".cr3",
            ".nef",
            ".arw",
            ".dng",
            ".orf",
            ".rw2",
            ".raf"
        };

        private static readonly string[] VideoExtensions =
        {
            ".mp4",
            ".mov",
            ".avi",
            ".mkv",
            ".wmv",
            ".m4v"
        };

        private static readonly string[] DocumentExtensions =
        {
            ".pdf",
            ".doc",
            ".docx",
            ".xls",
            ".xlsx",
            ".ppt",
            ".pptx",
            ".txt",
            ".epub"
        };

        /// <summary>
        /// Performs the single physical folder scan and creates the shared
        /// FileContext collection.
        ///
        /// Legacy FolderSummary classification is preserved.
        /// </summary>
        public FolderSummary Scan(string folderPath)
        {
            FolderSummary summary = new()
            {
                FolderPath = folderPath
            };

            if (!Directory.Exists(folderPath))
                return summary;

            EnumerationOptions options = new()
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            };

            List<string> files = Directory
                .EnumerateFiles(folderPath, "*.*", options)
                .ToList();

            //-------------------------------------------------
            // Legacy support
            //-------------------------------------------------

            summary.Files.AddRange(files);

            //-------------------------------------------------
            // Scout file contexts
            //-------------------------------------------------

            foreach (string file in files)
            {
                FileInfo info = new(file);

                string parentFolder =
                    info.DirectoryName ?? folderPath;

                string relativeFolder =
                    string.Equals(
                        parentFolder,
                        folderPath,
                        StringComparison.OrdinalIgnoreCase)
                    ? string.Empty
                    : Path.GetRelativePath(
                        folderPath,
                        parentFolder);

                summary.FileContexts.Add(new FileContext
                {
                    OriginalFullPath = file,
                    CurrentFullPath = file,
                    OriginalName = info.Name,
                    CurrentName = info.Name,
                    DestinationFolder = "",
                    DestinationName = info.Name,
                    Extension = info.Extension.ToLowerInvariant(),
                    Status = "Discovered",
                    ParentFolder = parentFolder,
                    RelativeFolder = relativeFolder
                });
            }

            summary.FileCount = files.Count;

            summary.FolderCount = Directory
                .EnumerateDirectories(folderPath, "*", options)
                .Count();

            summary.HasSubfolders = summary.FolderCount > 0;

            HashSet<string> extensions =
                new(StringComparer.OrdinalIgnoreCase);

            foreach (string file in files)
            {
                FileInfo info = new(file);

                summary.TotalBytes += info.Length;

                string extension =
                    info.Extension.ToLowerInvariant();

                if (extensions.Add(extension))
                    summary.Extensions.Add(extension);

                if (ImageExtensions.Contains(extension))
                {
                    summary.ImageCount++;
                    summary.HasExifImages = true;
                }
                else if (RawExtensions.Contains(extension))
                {
                    summary.ImageCount++;
                    summary.HasRawPhotos = true;
                    summary.HasExifImages = true;
                }
                else if (VideoExtensions.Contains(extension))
                {
                    summary.VideoCount++;
                    summary.HasVideos = true;
                }
                else if (DocumentExtensions.Contains(extension))
                {
                    summary.DocumentCount++;
                }
            }

            if (files.Any())
            {
                DateTime oldest =
                    files.Min(File.GetCreationTime);

                DateTime newest =
                    files.Max(File.GetCreationTime);

                summary.OldestFileDate =
                    oldest.ToShortDateString();

                summary.NewestFileDate =
                    newest.ToShortDateString();
            }

            summary.Extensions.Sort();

            return summary;
        }

        /// <summary>
        /// Filters an existing shared FileContext collection using the
        /// candidate extension hints supplied by Experts.
        ///
        /// This performs no additional physical folder scan. It simply
        /// evaluates the FileContexts already produced by Scan().
        ///
        /// Candidate extensions are hints only. They do not establish
        /// domain classification; the appropriate Expert remains responsible
        /// for deciding whether a candidate actually belongs to its domain.
        /// </summary>
        public IReadOnlyList<FileContext> GetCandidates(
            IReadOnlyList<FileContext> files,
            IReadOnlyList<ExpertDiscoveryRequest> requests)
        {
            ArgumentNullException.ThrowIfNull(files);
            ArgumentNullException.ThrowIfNull(requests);

            HashSet<string> candidateExtensions =
                new(StringComparer.OrdinalIgnoreCase);

            foreach (ExpertDiscoveryRequest request in requests)
            {
                if (request == null)
                    continue;

                foreach (string extension in request.CandidateExtensions)
                {
                    if (string.IsNullOrWhiteSpace(extension))
                        continue;

                    string normalizedExtension =
                        extension.StartsWith(".")
                            ? extension
                            : "." + extension;

                    candidateExtensions.Add(
                        normalizedExtension.ToLowerInvariant());
                }
            }

            if (candidateExtensions.Count == 0)
                return Array.Empty<FileContext>();

            return files
                .Where(file =>
                    !string.IsNullOrWhiteSpace(file.CurrentFullPath) &&
                    candidateExtensions.Contains(
                        Path.GetExtension(file.CurrentFullPath)
                            .ToLowerInvariant()))
                .ToList();
        }
    }
}