using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using VAP.Models;


namespace VAP.Pages
{
    public class IndexModel : PageModel
    {
        private readonly DetectionStore detectionStore;
        private readonly IConfiguration configuration;
        public IndexModel(IConfiguration configuration, DetectionStore detectionStore)
        {
            this.configuration = configuration;
            this.detectionStore = detectionStore;
        }

        [BindProperty(SupportsGet = true)]
        public string? NewStatus { get; set; }

        [BindProperty(Name = "dateFrom")]
        public DateTime DateFrom { get; set; } = DateTime.UtcNow;

        [BindProperty(Name = "dateTo")]
        public DateTime DateTo { get; set; } = DateTime.UtcNow;

        [BindProperty(Name = "detectionCheckFilter")]
        public string? DetectionCheckFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public List<Detection> Detections { get; set; } = new List<Detection>();
        public async Task OnGetAsync(string dateFrom, string dateTo, string detectionCheckFilter)
        {
            if (dateFrom == null && dateTo == null)
            {
                if (detectionStore.dateFrom == null && detectionStore.dateTo == null) return;

                dateFrom = detectionStore.dateFrom!;
                dateTo = detectionStore.dateTo!;
                DateFrom = DateTime.Parse(dateFrom);
                DateTo = DateTime.Parse(dateTo);
                DetectionCheckFilter = detectionCheckFilter = detectionStore.detectionFilter!;
            }
            else
            {
                detectionStore.dateFrom = dateFrom;
                detectionStore.dateTo = dateTo;
                detectionStore.detectionFilter = detectionCheckFilter;
            }

            List<Detection>? detections = detectionStore.GetDetections(DateTime.Parse(dateFrom!), DateTime.Parse(dateTo!));

            if (detections != null && detections.Count > 0)
            {
                Detections = detections.Where(det => det.DetectionCheck == detectionCheckFilter).ToList();
                return;
            }

            var dets = await GetDetectionsFromDatabase(dateFrom!, dateTo);
            Detections = dets.Where(d => d.DetectionCheck == detectionCheckFilter).ToList();
        }
        public async Task<IActionResult> OnPostUpdateStatusAsync(int detectionId)
        {
            string query = "UPDATE PPEDetectionTest SET DetectionCheck = @Status WHERE DetectionId = @DetectionId";

            string nullQuery = "UPDATE PPEDetectionTest SET DetectionCheck = NULL WHERE DetectionId = @DetectionId";

            int? status = NewStatus == "Alert" ? 1 : (NewStatus == "Not Alert" ? 0 : null);

            using (SqlConnection connection = new SqlConnection(configuration["ConnectionString:SqlServer"]))
            {
                await connection.OpenAsync();

                SqlCommand command = new SqlCommand(status == null ? nullQuery : query, connection);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@DetectionId", detectionId);

                await command.ExecuteNonQueryAsync();
            }

            detectionStore.UpdateStatus(detectionId, NewStatus!);

            return RedirectToPage("Index");
        }
        private async Task<List<Detection>> GetDetectionsFromDatabase(string dateFrom, string dateTo)
        {
            var FetchedDetections = new List<Detection>();

            string query = $"SELECT CameraSerial, Class, DetectionId, DetectionUnixEpoch, DetectionDateTime, DetectionCheck, DetectionImageUrl FROM PPEDetectionTest WHERE CAST(DetectionDateTime as DATE) BETWEEN '{dateFrom}' AND '{dateTo}'";

            using (SqlConnection connection = new SqlConnection(configuration["ConnectionString:SqlServer"]))
            {
                SqlCommand command = new SqlCommand(query, connection);

                try
                {
                    await connection.OpenAsync();
                    SqlDataReader reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        Detection detection = new Detection
                        {
                            Camera = reader["CameraSerial"].ToString(),
                            Label = MapDetectionLabel((int)reader["Class"]),
                            Id = (int)reader["DetectionId"],
                            UnixEpoch = (long)reader["DetectionUnixEpoch"],
                            Timestamp = (DateTime)reader["DetectionDateTime"],
                            DetectionCheck = MapDetectionCheck(reader["DetectionCheck"]),
                            ImageLink = reader["DetectionImageUrl"].ToString()
                        };

                        FetchedDetections.Add(detection);
                    }

                    detectionStore.StoreDetections(FetchedDetections);

                    reader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                return FetchedDetections.Count == 0 ? [] : FetchedDetections;
            }
        }
        private string MapDetectionCheck(object value)
        {
            int? intValue = value != DBNull.Value ? Convert.ToInt32(value) : null;

            switch (intValue)
            {
                case 0:
                    return "Not Alert";
                case 1:
                    return "Alert";
                default:
                    return "To be validated";
            }
        }
        private string MapDetectionLabel(int cls)
        {
            switch (cls)
            {
                case 1:
                    return "No Jacket";
                case 3:
                    return "No Helmet";
                case 6 or 7:
                    return "Drum on Floor";
                default:
                    return "Unknown";
            }
        }

    }

}