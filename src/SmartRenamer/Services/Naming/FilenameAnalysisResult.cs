using System.Collections.Generic;

namespace SmartRenamer.Services.Naming
{
    public class FilenameAnalysisResult
    {
        public string OriginalName { get; set; } = "";

        public string Extension { get; set; } = "";

        public bool HasExtension { get; set; }

        public bool IsAllUppercase { get; set; }

        public bool IsAllLowercase { get; set; }

        public bool IsTitleCase { get; set; }

        public bool ContainsUnderscores { get; set; }

        public bool ContainsExtraSpaces { get; set; }

        public bool LooksLikeCameraFile { get; set; }

        public bool LooksLikeTrackNumber { get; set; }

        public List<string> Observations { get; } = new();

        public List<NamingSuggestion> Suggestions { get; } = new();
    }
}