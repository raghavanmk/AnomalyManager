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

            using (var connection = new SqlConnection(configuration["ConnectionString:Sql"]))
            {
                await connection.OpenAsync();
                var query = "SELECT * FROM ActivityLog ORDER BY Timestamp DESC";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var activityLog = new ActivityLog
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Timestamp = Convert.ToDateTime(reader["Timestamp"]),
                                User = reader["User"].ToString(),
                                Action = reader["Action"].ToString(),
                                DetectionIds = reader["DetectionIds"].ToString()
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
