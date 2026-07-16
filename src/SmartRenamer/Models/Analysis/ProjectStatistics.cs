namespace SmartRenamer.Models.Analysis
{
    public class ProjectStatistics
    {
        public int FileCount { get; set; }

        public int FolderCount { get; set; }

        public long TotalBytes { get; set; }

        public int ImageCount { get; set; }

        public int VideoCount { get; set; }

        public int DocumentCount { get; set; }

        public string OldestDate { get; set; } = "";

        public string NewestDate { get; set; } = "";
    }
}