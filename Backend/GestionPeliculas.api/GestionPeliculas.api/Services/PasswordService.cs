using System.Security.Cryptography;
using System.Text;

namespace GestionPeliculas.api.Services
{
    public class PasswordService
    {
        public string GenerateSalt()
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
            return Convert.ToBase64String(saltBytes);
        }

        public string HashPassword(string password, string salt)
        {
            using var sha256 = SHA256.Create();

            byte[] bytes = Encoding.UTF8.GetBytes(password + salt);
            byte[] hash = sha256.ComputeHash(bytes);

            return Convert.ToBase64String(hash);
        }

        public bool VerifyPassword(string password, string salt, string storedHash)
        {
            string newHash = HashPassword(password, salt);
            return newHash == storedHash;
        }
    }
}