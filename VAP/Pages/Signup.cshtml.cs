using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

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

            string encryptedPassword = EncryptPassword(SignupPassword);

            InsertUser(SignupUsername, encryptedPassword);

            return RedirectToPage("/Login");
        }

        private bool IsUsernameExists(string username)
        {
            using (var connection = new SqlConnection(configuration["ConnectionString:SqlServer"]))
            {
                connection.Open();
                var command = new SqlCommand($"SELECT COUNT(*) FROM {configuration["DbTable:User"]} WHERE Username = @Username", connection);
                command.Parameters.AddWithValue("@Username", username);
                return (int)command.ExecuteScalar() > 0;
            }
        }

        private void InsertUser(string username, string password)
        {
            using (var connection = new SqlConnection(configuration["ConnectionString:SqlServer"]))
            {
                connection.Open();
                var command = new SqlCommand($"INSERT INTO {configuration["DbTable:User"]} (Username, Password) VALUES (@Username, @Password)", connection);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Password", password);
                command.ExecuteNonQuery();
            }
        }

        private string EncryptPassword(string password)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(configuration["EncryptionKey"]);
                aesAlg.IV = new byte[16];

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(password);
                        }
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }
    }
}
