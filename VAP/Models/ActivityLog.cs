namespace VAP.Models
{
    public class ActivityLog
    { 
        public int UserID { get; set; }
        public int LogID { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Username { get; set; }
        public string? Action { get; set; }
        public string? Detections { get; set; }
        
    }
}
