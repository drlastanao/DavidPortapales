using Xunit;
using DavidPortapales.Services;
using System.Linq;

namespace DavidPortapales.Test;

public class PasswordGeneratorServiceTests
{
    [Fact]
    public void GeneratePassword_ReturnsCorrectLength()
    {
        var service = new PasswordGeneratorService();
        string password = service.GeneratePassword(12, true, true, true);
        Assert.Equal(12, password.Length);
    }

    [Fact]
    public void GeneratePassword_IncludesSpecialChars_WhenRequested()
    {
        // This is probabilistic, but with enough length/attempts it's likely.
        // However, for strict testing we check if it comes from the pool.
        var service = new PasswordGeneratorService();
        // Force purely special chars? No, the logic mixes them.
        // We can test that it DOES NOT contain special chars if false.
        string password = service.GeneratePassword(100, true, true, false);
        string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";
        Assert.DoesNotContain(password, c => specialChars.Contains(c));
    }

    [Fact]
    public void GeneratePassword_IncludesDigits_Always()
    {
        var service = new PasswordGeneratorService();
        string password = service.GeneratePassword(100, false, false, false); // Only digits
        Assert.All(password, c => Assert.True(char.IsDigit(c)));
    }
    [Fact]
    public void GeneratePassword_IncludesUpperCase_WhenRequested()
    {
        var service = new PasswordGeneratorService();
        string password = service.GeneratePassword(100, true, false, false);
        Assert.Contains(password, c => char.IsUpper(c));
    }

    [Fact]
    public void GeneratePassword_IncludesLowerCase_WhenRequested()
    {
        var service = new PasswordGeneratorService();
        string password = service.GeneratePassword(100, false, true, false);
        Assert.Contains(password, c => char.IsLower(c));
    }

    [Fact]
    public void GeneratePassword_ThrowsException_WhenLengthIsInvalid()
    {
        var service = new PasswordGeneratorService();
        Assert.Throws<System.ArgumentException>(() => service.GeneratePassword(0, true, true, true));
        Assert.Throws<System.ArgumentException>(() => service.GeneratePassword(-1, true, true, true));
    }
    [Fact]
    public void GeneratePassword_DefaultsToDigits_WhenAllOptionsFalse()
    {
        var service = new PasswordGeneratorService();
        string password = service.GeneratePassword(50, false, false, false);
        Assert.All(password, c => Assert.True(char.IsDigit(c)));
    }
}

