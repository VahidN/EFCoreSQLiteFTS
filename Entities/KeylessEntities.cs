namespace EFCoreSQLiteFTS.Entities
{
    public class ChapterFTS
    {
        public int RowId { get; set; }
        public decimal? Rank { get; set; }

        public string Title { get; set; }
        public string Text { get; set; }
    }

    public class SpellCheck
    {
        public string Word { get; set; }
        public decimal Rank { get; set; }
        public decimal Distance { get; set; }
        public decimal Score { get; set; }
        public decimal Matchlen { get; set; }
    }
}