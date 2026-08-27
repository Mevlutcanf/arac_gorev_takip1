using System.Security.Cryptography;

namespace AracGorevFormu.Services
{
    /// <summary>
    /// .NET'in yerleşik Rfc2898DeriveBytes (PBKDF2) sınıfını kullanarak
    /// dışarıdan paket gerektirmeden güvenli şifre hashleme sağlar.
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;

        public static (string hash, string salt) Hashle(string sifre)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(sifre, saltBytes, Iterations, HashAlgorithmName.SHA256, KeySize);
            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }

        public static bool Dogrula(string sifre, string hash, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(sifre, saltBytes, Iterations, HashAlgorithmName.SHA256, KeySize);
            var computedHash = Convert.ToBase64String(hashBytes);
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(computedHash),
                Convert.FromBase64String(hash));
        }
    }
}
