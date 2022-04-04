namespace Mue.Server.Core.Utils;

public static class Security
{
    public static string HashPassword(string password)
    {
        return Sodium.PasswordHash.ArgonHashString(password);
    }

    public static bool ComparePasswords(string storedPassword, string providedPassword)
    {
        return Sodium.PasswordHash.ArgonHashStringVerify(storedPassword, providedPassword);
    }
}
