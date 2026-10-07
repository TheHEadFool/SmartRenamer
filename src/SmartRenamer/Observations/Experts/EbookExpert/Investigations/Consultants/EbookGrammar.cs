namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants
{
    internal static class EbookGrammar
    {
        public static string CountNoun(int count) =>
            CountPhrase(count, "ebook", "ebooks");

        public static string CountPhrase(
            int count,
            string singular,
            string plural) =>
            $"{count:N0} {(count == 1 ? singular : plural)}";

        public static string IsAre(int count) =>
            count == 1 ? "is" : "are";

        public static string HasHave(int count) =>
            count == 1 ? "has" : "have";

        public static string WasWere(int count) =>
            count == 1 ? "was" : "were";
    }
}
