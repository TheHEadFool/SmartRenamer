using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SmartRenamer.Models;

namespace SmartRenamer.Services
{
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
            ".txt"
        };

        public FolderSummary Scan(string folderPath)
        {
            FolderSummary summary = new()
            {
                FolderPath = folderPath
            };

            if (!Directory.Exists(folderPath))
                return summary;

            List<string> files = Directory
                .EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
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

                summary.FileContexts.Add(new FileContext
                {
                    OriginalFullPath = file,
                    CurrentFullPath = file,
                    OriginalName = info.Name,
                    CurrentName = info.Name,
                    DestinationFolder = "",
                    DestinationName = info.Name,
                    Status = "Discovered"
                });
            }

            summary.FileCount = files.Count;

            summary.FolderCount = Directory
                .EnumerateDirectories(folderPath, "*", SearchOption.AllDirectories)
                .Count();

            summary.HasSubfolders = summary.FolderCount > 0;

            HashSet<string> extensions =
                new(StringComparer.OrdinalIgnoreCase);

            foreach (string file in files)
            {
                FileInfo info = new(file);

                summary.TotalBytes += info.Length;

                string extension = info.Extension.ToLowerInvariant();

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
                DateTime oldest = files.Min(File.GetCreationTime);
                DateTime newest = files.Max(File.GetCreationTime);

                summary.OldestFileDate = oldest.ToShortDateString();
                summary.NewestFileDate = newest.ToShortDateString();
            }

            summary.Extensions.Sort();

            return summary;
        }
    }
}