using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Azure.Storage.Blobs;
using VAP.Models;
using Azure.Storage.Sas;
using Azure.Storage;

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

        [BindProperty(Name = "dateFilter")]
        public DateTime DateFilter { get; set; } = DateTime.UtcNow;

        [BindProperty(Name = "detectionCheckFilter")]
        public string? DetectionCheckFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public List<Detection> Detections { get; set; } = new List<Detection>();
        public async Task OnGetAsync(string dateFilter, string detectionCheckFilter)
        {

            if (dateFilter == null)
            {
                if (detectionStore.dateFilter == null) return;

                dateFilter = detectionStore.dateFilter;
                DateFilter = DateTime.Parse(dateFilter);
                DetectionCheckFilter = detectionCheckFilter = detectionStore.detectionFilter!;
            }
            else
            {     
                detectionStore.dateFilter = dateFilter;
                detectionStore.detectionFilter = detectionCheckFilter;
            }
           
            List<Detection>? detections = detectionStore.GetDetections(DateTime.Parse(dateFilter));

            if (detections != null && detections.Count > 0)
            {
                Detections = detections.Where(det => det.DetectionCheck == detectionCheckFilter).ToList();
                return;
            }
            
            var dets = await GetDetectionsFromDatabase(dateFilter);
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
        private async Task<List<Detection>> GetDetectionsFromDatabase(string dateFilter)
        {
            var FetchedDetections = new List<Detection>();

            string query = $"SELECT CameraSerial, Class, DetectionId, DetectionUnixEpoch, DetectionDateTime, DetectionCheck FROM PPEDetectionTest WHERE CAST(DetectionDateTime as DATE) = '{dateFilter}'";

          /*  int? detCheck = detectionCheck == "Alert" ? 1 : (detectionCheck == "Not Alert" ? 0 : null);

            if (detCheck != null) query += $" AND DetectionCheck = {detCheck}";
            else query += " AND DetectionCheck IS NULL";
          */
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
                            DetectionCheck = MapDetectionCheck(reader["DetectionCheck"])
                        };

                        detection.Image = await GenerateSasUrlAsync(detection.Camera!, detection.UnixEpoch);

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
        private async Task<string?> GenerateSasUrlAsync(string cameraSerial, long unixEpoch)
        {
            var blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("BLOB__CONNECTIONSTRING", EnvironmentVariableTarget.User));
            var containerClient = blobServiceClient.GetBlobContainerClient(Environment.GetEnvironmentVariable("BLOB__CONTAINERNAME", EnvironmentVariableTarget.User));

            string blobNamePattern = $"{cameraSerial}_{unixEpoch}_.*";

            try
            {
                await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: cameraSerial))
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(blobItem.Name, blobNamePattern))
                    {
                        Console.WriteLine($"Blob found: {blobItem.Name}");

                        var blobClient = containerClient.GetBlobClient(blobItem.Name);

                        var sasBuilder = new BlobSasBuilder
                        {
                            BlobContainerName = containerClient.Name,
                            BlobName = blobClient.Name,
                            Resource = "b",
                            StartsOn = DateTimeOffset.UtcNow,
                            ExpiresOn = DateTimeOffset.UtcNow.AddYears(100),
                            Protocol = SasProtocol.Https
                        };

                        sasBuilder.SetPermissions(BlobSasPermissions.Read);

                        string sasToken = sasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(Environment.GetEnvironmentVariable("BLOB__ACCOUNTNAME", EnvironmentVariableTarget.User), Environment.GetEnvironmentVariable("BLOB__ACCOUNTKEY", EnvironmentVariableTarget.User))).ToString();

                        var sasUrl = $"{blobClient.Uri}?{sasToken}";

                        return sasUrl;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
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