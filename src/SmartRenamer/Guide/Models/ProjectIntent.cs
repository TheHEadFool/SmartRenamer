using System.Collections.Generic;

namespace SmartRenamer.Models
{
    /// <summary>
    /// Represents the decisions Friend has made about how
    /// the project should be organized.
    ///
    /// Scout discovers facts.
    /// Friend provides intent.
    /// The Rename Planner combines both.
    /// </summary>
    public class ProjectIntent
    {
        public string Goal { get; set; } = "";

        public NamingStyle NamingStyle { get; set; } =
            NamingStyle.NotChosen;

        public bool CreateFolders { get; set; }

        public bool RenameFiles { get; set; }

        public bool KeepExistingNames { get; set; } = true;

        public Dictionary<string, string> Answers { get; } = new();
    }

    public enum NamingStyle
    {
        NotChosen,
        KeepExisting,
        Sequential,
        DateTaken,
        ByProject,
        Custom
    }
}
