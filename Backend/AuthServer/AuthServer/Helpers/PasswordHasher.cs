namespace AuthServer.Helpers
{
    internal class PasswordHasher
    {
        internal static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        internal static bool VerifyPassword(byte[] password, string hashedPassword)
        {
            string passwordAsString = System.Text.Encoding.UTF8.GetString(password);
            return BCrypt.Net.BCrypt.Verify(passwordAsString, hashedPassword);
        }
        internal static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
