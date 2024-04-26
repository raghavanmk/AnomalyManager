using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using VAP.Models;

namespace VAP.Pages
{
    public class ActivityLogModel : PageModel
    {
        private readonly IConfiguration configuration;
        public ActivityLogModel(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [BindProperty]
        public List<ActivityLog> ActivityLogs { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            ActivityLogs = await GetActivityLogsFromDatabase();
            return Page();
        }

        private async Task<List<ActivityLog>> GetActivityLogsFromDatabase()
        {
            var activityLogs = new List<ActivityLog>();

            using (var connection = new SqlConnection(configuration["ConnectionString:SqlServer"]))
            {
                await connection.OpenAsync();
                // var query = "SELECT * FROM ActivityLog ORDER BY Timestamp DESC";
                var query = $"SELECT l.*, u.Username FROM {configuration["DbTable:Log"]} l INNER JOIN {configuration["DbTable:User"]} u ON l.UserID = u.UserID ORDER BY l.Timestamp DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var activityLog = new ActivityLog
                            {
                                UserID = Convert.ToInt32(reader["UserID"]),
                                LogID = Convert.ToInt32(reader["LogID"]),
                                Timestamp = Convert.ToDateTime(reader["Timestamp"]),
                                Username = reader["Username"].ToString(),
                                Action = reader["Action"].ToString(),
                                Detections = reader["Detections"].ToString()
                            };
                            activityLogs.Add(activityLog);
                        }
                    }
                }
            }

            return activityLogs;
        }
    }
}
