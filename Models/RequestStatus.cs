namespace LanguageServiceAPI.Models
{
    public class RequestStatus
    {
        public string Text { get; set; }
        public bool IsProcessed { get; set; }
        public DateTime RequestTime { get; set; }
        public string Language { get; set; }
        public string ISOCode { get; set; }
        public double ConfidenceScore { get; set; }
    }
}
