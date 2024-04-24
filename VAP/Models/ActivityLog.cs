namespace VAP.Models
{
    public class ActivityLog
    { 
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string User { get; set; }
        public string Action { get; set; }
        public string DetectionIds { get; set; }
        
    }
}
