namespace SmartRenamer.Services
{
    public class FolderSummary
    {
        public string FolderPath { get; set; } = "";

        public int FolderCount { get; set; }

        public int FileCount { get; set; }

        public int ImageCount { get; set; }

        public int VideoCount { get; set; }

        public int DocumentCount { get; set; }

        public long TotalBytes { get; set; }

        public string OldestFileDate { get; set; } = "";

        public string NewestFileDate { get; set; } = "";
    }
}