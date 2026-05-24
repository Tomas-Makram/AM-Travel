using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace BusinessLayer.Services
{
    public interface IDataCiphers
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }

    // [Header] + [IV] + [Encrypted Data] + [Footer]
    public class DataCiphers : IDataCiphers
    {
        private readonly IDataProtector _protector;

        public DataCiphers(IDataProtectionProvider provider, IOptions<DataProtectionSettings> dataProtectionOptions)
        {
            _protector = provider.CreateProtector(dataProtectionOptions.Value.UserDataPurpose);
        }

        public string Encrypt(string plainText)
        {
            return _protector.Protect(plainText);
        }

        public string Decrypt(string cipherText)
        {
            return _protector.Unprotect(cipherText);
        }
    }
}