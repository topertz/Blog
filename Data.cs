namespace BlogEntries
{
    public class Data
    {
        public int id { get; set; }
        public required string? text { get; set; }
        public required string? title { get; set; }
        public int authorID { get; set; }
        public int date { get; set; }
    }
}
