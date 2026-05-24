using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using BusinessLayer.Services;

namespace BusinessLayer.Services
{
    public class AdminSeeder
    {
        private readonly DBContext _context;
        private readonly IDataHasher _hasher;
        private readonly IDataCiphers _ciphers;
        public AdminSeeder(DBContext context, IDataHasher hasher,IDataCiphers ciphers)
        {
            _context = context;
            _hasher = hasher;
            _ciphers = ciphers;
        }

        public async Task SeedAdminAsync()
        {
            try
            {
                var adminExists = await _context.Users.AnyAsync(u => u.Type == _hasher.HashComparison(AccountType.Admin.ToString()));

                if (adminExists)
                    return;

                var adminUser = new User
                {
                    UserId = Guid.NewGuid(),
                    UserName = "Admin",
                    FullName = "Administrator Account",
                    PhoneNumber = "01221936850",
                    NationalId = "30405242500959",
                    PasswordHash = _hasher.HashData("Admin@123"),
                    Type = _hasher.HashComparison(AccountType.Admin.ToString()),
                    TypeChipher = _ciphers.Encrypt(AccountType.Admin.ToString()),
                    Activate = true,
                    Login = false,
                    Blocked = false,
                    FailedLoginAttempts = 0,
                    JoinDate = DateTime.UtcNow
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();
            }
            catch
            {
            }
        }
    }
}