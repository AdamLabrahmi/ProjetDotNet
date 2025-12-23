using BCrypt.Net;

namespace ProjetDotNet.Helpers
{
    public static class PasswordHelper
    {
        // Hash du mot de passe
        public static string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Vérification du mot de passe
        public static bool Verify(string motDePasseHash, string password)
        {
            return BCrypt.Net.BCrypt.Verify(password, motDePasseHash);
        }
    }
}
