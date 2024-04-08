namespace VAP.Models
{
    public class Detection
    {
        public string? Label { get; set; }
        public string? Camera { get; set; }
        public int Id { get; set; }
        public long UnixEpoch { get; set; }
        public DateTime Timestamp { get; set; }
        public string? DetectionCheck { get; set; }
        public string? Image {  get; set; }
    }
}
