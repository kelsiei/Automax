using System.Security.Cryptography;
using System.Text;

namespace CarCareTracker.Helper;

public interface IPasswordHelper
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string storedHash);
}

public class PasswordHelper : IPasswordHelper
{
    private const int SaltSize = 16; // 128-bit
    private const int SubkeyLength = 32; // 256-bit
    private const int Iterations = 100_000;
    private const string Prefix = "PBKDF2$";

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return string.Empty;
        }

        var salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        var subkey = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            SubkeyLength);

        var saltB64 = Convert.ToBase64String(salt);
        var subkeyB64 = Convert.ToBase64String(subkey);

        return $"{Prefix}{Iterations}${saltB64}${subkeyB64}";
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
        {
            return false;
        }

        if (storedHash.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return VerifyPbkdf2(password ?? string.Empty, storedHash);
        }

        // Legacy SHA256 verification for backward compatibility
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hashBytes = sha.ComputeHash(bytes);
        var legacyHash = Convert.ToBase64String(hashBytes);

        return string.Equals(legacyHash, storedHash, StringComparison.Ordinal);
    }

    private bool VerifyPbkdf2(string password, string storedHash)
    {
        // Expected format: PBKDF2$<iterations>$<saltBase64>$<subkeyBase64>
        var parts = storedHash.Split('$');
        if (parts.Length != 4)
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expectedSubkey;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedSubkey = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualSubkey = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedSubkey.Length);

        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
    }
}
