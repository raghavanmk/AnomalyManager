namespace VAP.Models
{
    public class DetectionStore
    {
        private List<Detection> _detections = new List<Detection>();
        public string? dateFilter {  get; set; }
        public string? detectionFilter { get; set; }    
        public List<Detection>? GetDetections(DateTime startDate)
        {
            var filteredDetections = _detections
                .Where(d => d.Timestamp.Date == startDate);

            return filteredDetections.ToList();
        }

        public void StoreDetections(List<Detection> detections)
        {
            //_detections.Clear();
            _detections.AddRange(detections);
        }

        public void UpdateStatus(int detectionId, string status) 
        { 
           _detections.FirstOrDefault(d => d.Id == detectionId)!.DetectionCheck = status;
        }
    }
}

