namespace VAP.Models
{
    public class DetectionStore
    {
        private List<Detection> _detections = new List<Detection>();
        public string? dateFrom { get; set; }
        public string? dateTo { get; set; }
        public string? detectionFilter { get; set; }
        public List<Detection>? GetDetections(DateTime startDate, DateTime endDate)
        {
            var filteredDetections = _detections
                .Where(d => d.Timestamp.Date >= startDate.Date && d.Timestamp.Date <= endDate.Date);

            return filteredDetections.ToList();
        }

        public void StoreDetections(List<Detection> detections)
        {
            _detections.Clear();
            _detections.AddRange(detections);
        }

        public void UpdateStatus(int detectionId, string status)
        {
            _detections.FirstOrDefault(d => d.Id == detectionId)!.DetectionCheck = status;
        }
    }
}

