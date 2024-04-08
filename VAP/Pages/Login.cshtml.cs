using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace VAP.Pages
{
    public class LoginModel : PageModel
    {
        private readonly string _connectionString = "YourConnectionString";

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public IActionResult OnPost()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM Users WHERE Username = @Username AND Password = @Password";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", Username);
                    command.Parameters.AddWithValue("@Password", Password);
                    int count = (int)command.ExecuteScalar();
                    if (count == 1)
                    {
                        // Login successful
                        return RedirectToPage("/Index");
                    }
                    else
                    {
                        // Login failed
                        ErrorMessage = "Invalid username or password";
                        return Page();
                    }
                }
            }
        }
    }

}
