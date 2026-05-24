using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace BusinessLayer.Services
{
    public interface IDataHasher
    {
        string HashData(string password);
        bool VerifyHashed(string password, string hashedPassword);
        string HashComparison(string email);
    }

    public class DataHasher : IDataHasher
    {
        private readonly int _workFactor;

        public DataHasher(IOptions<PasswordHashSettings> passwordHashOptions)
        {
            _workFactor = passwordHashOptions.Value.WorkFactor;
        }

        public string HashData(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: _workFactor);
        }

        public bool VerifyHashed(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                return false;

            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

        public bool NeedsRehash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                return false;

            return BCrypt.Net.BCrypt.PasswordNeedsRehash(passwordHash, _workFactor);
        }

        public string HashComparison(string email)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(email.Trim().ToLower());
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}