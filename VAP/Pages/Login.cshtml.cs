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
        public string LoginUsername { get; set; }

        [BindProperty(SupportsGet = true)]
        public string LoginPassword { get; set; }

        public IActionResult OnGet()
        {
            ModelState.Clear();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            using (var connection = new SqlConnection(configuration["ConnectionString:Sql"]))
            {
                connection.Open();
                var command = new SqlCommand($"SELECT * FROM {configuration["DbTable:Users"]} WHERE Username = @Username AND Password = @Password", connection);
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