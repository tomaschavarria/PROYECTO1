using System.Security.Cryptography;
using System.Text;

namespace PROYECTO1
{
    public static class SeguridadHelper
    {
        // Genera un hash SHA-256 irreversible de 64 caracteres en minúsculas
        public static string HashSHA256(string textoPlano)
        {
            if (string.IsNullOrEmpty(textoPlano)) return string.Empty;

            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(textoPlano));
            var sb = new StringBuilder();

            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}