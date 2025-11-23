using CarCareTracker.Helper;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace CarCareTracker.Tests.Helper;

public class PasswordHelperTests
{
    private readonly PasswordHelper _helper = new();

    [Fact]
    public void HashPassword_ReturnsPbkdf2Format()
    {
        var hash = _helper.HashPassword("Password123!");

        Assert.False(string.IsNullOrEmpty(hash));
        Assert.StartsWith("PBKDF2$", hash);
    }

    [Fact]
    public void HashPassword_ProducesDifferentHashesForSamePassword()
    {
        var hashA = _helper.HashPassword("Password123!");
        var hashB = _helper.HashPassword("Password123!");

        Assert.NotEqual(hashA, hashB);
        Assert.StartsWith("PBKDF2$", hashA);
        Assert.StartsWith("PBKDF2$", hashB);
    }

    [Fact]
    public void VerifyPassword_WorksForPbkdf2Hashes()
    {
        var password = "P@ssw0rd!";
        var hash = _helper.HashPassword(password);

        Assert.True(_helper.VerifyPassword(password, hash));
        Assert.False(_helper.VerifyPassword("wrong", hash));
    }

    [Fact]
    public void EmptyPassword_HashIsEmpty_AndVerifyFails()
    {
        var hash = _helper.HashPassword(string.Empty);

        Assert.Equal(string.Empty, hash);
        Assert.False(_helper.VerifyPassword("non-empty", hash));
    }

    [Fact]
    public void VerifyPassword_SupportsLegacySha256()
    {
        var password = "Legacy123";
        var legacyHash = ComputeLegacySha256(password);

        Assert.True(_helper.VerifyPassword(password, legacyHash));
        Assert.False(_helper.VerifyPassword("wrong", legacyHash));
    }

    [Fact]
    public void VerifyPassword_ReturnsFalseForNullOrEmptyStoredHash()
    {
        Assert.False(_helper.VerifyPassword("any", null!));
        Assert.False(_helper.VerifyPassword("any", string.Empty));
    }

    [Fact]
    public void VerifyPassword_MalformedPbkdf2_ReturnsFalse()
    {
        Assert.False(_helper.VerifyPassword("any", "PBKDF2$not-enough-parts"));
        Assert.False(_helper.VerifyPassword("any", "PBKDF2$baditer$###$###"));
        Assert.False(_helper.VerifyPassword("any", "PBKDF2$100000$badbase64$alsobad"));
    }

    private static string ComputeLegacySha256(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hashBytes = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hashBytes);
    }
}
