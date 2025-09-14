using System.Security.Cryptography;
using System.Text;

namespace tryout.Services
{
    public class Passwordcrypt
    {
        public static void CreatePasswordHash(string password, out string passwordHashBase64, out string passwordSaltBase64)
        {
            using var hmac = new HMACSHA512();
            var salt = hmac.Key; // per-user salt
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            passwordSaltBase64 = Convert.ToBase64String(salt);
            passwordHashBase64 = Convert.ToBase64String(hash);
        }

        public static bool VerifyPasswordHash(string password, string storedHashBase64, string storedSaltBase64)
        {
            var salt = Convert.FromBase64String(storedSaltBase64);
            using var hmac = new HMACSHA512(salt);
            var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            var stored = Convert.FromBase64String(storedHashBase64);
            return computed.SequenceEqual(stored);
        }
    }
}

