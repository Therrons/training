using System;
using System.Security.Cryptography;

namespace fraud_poc_project.Services
{
    // Hashes passwords with PBKDF2-SHA256 and a random salt.
    // Stored format: PBKDF2-SHA256$<iterations>$<base64 salt>$<base64 key>
    public static class PasswordHasher
    {
        private const string Prefix = "PBKDF2-SHA256";
        private const int Iterations = 310000;
        private const int SaltSize = 16;
        private const int KeySize = 32;

        public static string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
        }

        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            var parts = storedHash.Split('$');
            if (parts.Length != 4 || parts[0] != Prefix || !int.TryParse(parts[1], out var iterations))
            {
                return false;
            }

            byte[] salt;
            byte[] expected;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expected = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }
}
