using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace VAP.Pages
{
    public class SignupModel : PageModel
    {
        private readonly IConfiguration configuration;
        public SignupModel(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [BindProperty(SupportsGet = true)]
        public string SignupUsername { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SignupPassword { get; set; }
        public IActionResult OnGet()
        {
            ModelState.Clear();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            if (string.IsNullOrWhiteSpace(SignupUsername) || string.IsNullOrWhiteSpace(SignupPassword))
            {
                ModelState.AddModelError("", "Username and password are required.");
                return Page();
            }

            if (IsUsernameExists(SignupUsername))
            {
                ModelState.AddModelError("", "Username already exists. Please choose a different one.");
                return Page();
            }

            InsertUser(SignupUsername, SignupPassword);

            return RedirectToPage("/Login");
        }

        private bool IsUsernameExists(string username)
        {
            using (var connection = new SqlConnection(configuration["ConnectionString:Sql"]))
            {
                connection.Open();
                var command = new SqlCommand($"SELECT COUNT(*) FROM {configuration["DbTable:Users"]} WHERE Username = @Username", connection);
                command.Parameters.AddWithValue("@Username", username);
                return (int)command.ExecuteScalar() > 0;
            }
        }

        private void InsertUser(string username, string password)
        {
            using (var connection = new SqlConnection(configuration["ConnectionString:Sql"]))
            {
                connection.Open();
                var command = new SqlCommand($"INSERT INTO {configuration["DbTable:Users"]} (Username, Password) VALUES (@Username, @Password)", connection);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Password", password);
                command.ExecuteNonQuery();
            }
        }
    }
}
