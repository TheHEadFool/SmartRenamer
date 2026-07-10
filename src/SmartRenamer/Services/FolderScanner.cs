using System;
using System.IO;
using System.Linq;

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

            var files = Directory
                .EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .ToList();

            summary.FileCount = files.Count;

            summary.FolderCount = Directory
                .EnumerateDirectories(folderPath, "*", SearchOption.AllDirectories)
                .Count();

            foreach (string file in files)
            {
                FileInfo info = new(file);

                summary.TotalBytes += info.Length;

                string extension = info.Extension.ToLowerInvariant();

                if (ImageExtensions.Contains(extension))
                    summary.ImageCount++;

                else if (VideoExtensions.Contains(extension))
                    summary.VideoCount++;

                else if (DocumentExtensions.Contains(extension))
                    summary.DocumentCount++;
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

            return summary;
        }
    }
}