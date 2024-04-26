using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

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
            using (var connection = new SqlConnection(configuration["ConnectionString:SqlServer"]))
            {
                connection.Open();
                var command = new SqlCommand($"SELECT * FROM {configuration["DbTable:User"]} WHERE Username = @Username", connection);
                command.Parameters.AddWithValue("@Username", LoginUsername);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int userId = (int)reader["UserID"];
                        string storedEncryptedPassword = reader["Password"].ToString();
                        string storedPassword = DecryptPassword(storedEncryptedPassword!);

                        if (storedPassword == LoginPassword)
                        {
                            var claims = new[]
                            {
                                 new Claim(ClaimTypes.Name, LoginUsername),
                                 new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                             };

                            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                            var authProperties = new AuthenticationProperties();

                            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                            return RedirectToPage("/Index");
                        }
                    }
                }
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();

        }

        private string DecryptPassword(string encryptedPassword)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(configuration["EncryptionKey"]);
                aesAlg.IV = new byte[16];

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedPassword)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}