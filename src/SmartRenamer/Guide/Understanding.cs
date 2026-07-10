namespace SmartRenamer.Guide
{
    public class Understanding
    {
        public int GoalConfidence { get; set; }

        public int SourceConfidence { get; set; }

        public int DestinationConfidence { get; set; }

        public int NamingConfidence { get; set; }

        public int SafetyConfidence { get; set; }

        public int OverallConfidence =>
            (GoalConfidence +
             SourceConfidence +
             DestinationConfidence +
             NamingConfidence +
             SafetyConfidence) / 5;
    }
}