using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace VAP.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration configuration;
        public LoginModel(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [BindProperty(SupportsGet = true)]
        public string SignupUsername { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SignupPassword { get; set; }

        [BindProperty(SupportsGet = true)]
        public string LoginUsername { get; set; }

        [BindProperty(SupportsGet = true)]
        public string LoginPassword { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public IActionResult OnPostSignup()
        {
            using (var connection = new SqlConnection(configuration["ConnectionString:Sql"]))
            {
                connection.Open();
                var command = new SqlCommand("INSERT INTO Users (Username, Password) VALUES (@Username, @Password)", connection);
                command.Parameters.AddWithValue("@Username", SignupUsername);
                command.Parameters.AddWithValue("@Password", SignupPassword);
                command.ExecuteNonQuery();
            }

            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostAsync()
        {

            using (var connection = new SqlConnection(configuration["ConnectionString:Sql"]))
            {
                connection.Open();
                var command = new SqlCommand("SELECT * FROM [User] WHERE Username = @Username AND Password = @Password", connection);
                command.Parameters.AddWithValue("@Username", LoginUsername);
                command.Parameters.AddWithValue("@Password", LoginPassword);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var claims = new[]
                        {
                             new Claim(ClaimTypes.Name, LoginUsername)
                         };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                        var authProperties = new AuthenticationProperties();

                        HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties).Wait();
                        return RedirectToPage("/Index");
                    }
                }
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();

        }
    }
}
