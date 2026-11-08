using RaceDay.Api.Services;
using Xunit;

namespace RaceDay.Tests;

public class PasswordServiceTests
{
    private readonly PasswordService _service = new();

    [Fact]
    public void Hash_DoesNotContainPlainPassword()
    {
        const string password = "SafePassword123!";
        string hash = _service.Hash(password);
        Assert.DoesNotContain(password, hash);
    }

    [Fact]
    public void Verify_ReturnsTrueForCorrectPassword()
    {
        string hash = _service.Hash("SafePassword123!");
        Assert.True(_service.Verify("SafePassword123!", hash));
    }

    [Fact]
    public void Verify_ReturnsFalseForIncorrectPassword()
    {
        string hash = _service.Hash("SafePassword123!");
        Assert.False(_service.Verify("WrongPassword123!", hash));
    }
}
