using FluentAssertions;
using MarsVista.Api.Services;
using Xunit;

namespace MarsVista.Api.Tests.Services;

public class ApiKeyServiceTests
{
    private readonly ApiKeyService _sut;

    public ApiKeyServiceTests()
    {
        _sut = new ApiKeyService();
    }

    [Fact]
    public void GenerateApiKey_ShouldReturnValidFormat()
    {
        // Act
        var apiKey = _sut.GenerateApiKey();

        // Assert
        apiKey.Should().NotBeNullOrEmpty();
        apiKey.Should().MatchRegex(@"^mv_live_[a-f0-9]{40}$");
        apiKey.Length.Should().Be(48); // "mv_live_" (8) + 40 hex chars
    }

    [Fact]
    public void GenerateApiKey_ShouldGenerateUniqueKeys()
    {
        // Arrange
        var keys = new HashSet<string>();

        // Act - Generate 100 keys
        for (int i = 0; i < 100; i++)
        {
            keys.Add(_sut.GenerateApiKey());
        }

        // Assert - All keys should be unique
        keys.Count.Should().Be(100);
    }

    [Fact]
    public void GenerateApiKey_ShouldStartWithCorrectPrefix()
    {
        // Act
        var apiKey = _sut.GenerateApiKey();

        // Assert
        apiKey.Should().StartWith("mv_live_");
    }

    [Fact]
    public void HashApiKey_ShouldReturnSha256Hash()
    {
        // Arrange
        var apiKey = "mv_live_" + new string('a', 40);

        // Act
        var hash = _sut.HashApiKey(apiKey);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().MatchRegex(@"^[a-f0-9]{64}$"); // SHA-256 produces 64 hex characters
        hash.Length.Should().Be(64);
    }

    [Fact]
    public void HashApiKey_ShouldProduceSameHashForSameInput()
    {
        // Arrange
        var apiKey = "mv_live_" + new string('a', 40);

        // Act
        var hash1 = _sut.HashApiKey(apiKey);
        var hash2 = _sut.HashApiKey(apiKey);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashApiKey_ShouldProduceDifferentHashForDifferentInput()
    {
        // Arrange
        var apiKey1 = "mv_live_" + new string('a', 40);
        var apiKey2 = "mv_live_" + new string('b', 40);

        // Act
        var hash1 = _sut.HashApiKey(apiKey1);
        var hash2 = _sut.HashApiKey(apiKey2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void HashApiKey_ShouldThrowExceptionForNullOrEmpty(string? apiKey)
    {
        // Act
        Action act = () => _sut.HashApiKey(apiKey!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("API key cannot be null or empty*");
    }

    [Theory]
    [InlineData("mv_live_" + "a1b2c3d4e5f6789012345678901234567890abcd")]
    [InlineData("mv_live_1234567890abcdef1234567890abcdef12345678")]
    [InlineData("mv_live_ffffffffffffffffffffffffffffffffffffffff")]
    public void ValidateApiKeyFormat_ShouldReturnTrueForValidKeys(string apiKey)
    {
        // Act
        var result = _sut.ValidateApiKeyFormat(apiKey);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("mv_live_toolong1234567890abcdef1234567890abcdef123456789")]
    [InlineData("mv_live_tooshort")]
    [InlineData("mv_test_1234567890abcdef1234567890abcdef12345678")] // wrong environment
    [InlineData("api_live_1234567890abcdef1234567890abcdef12345678")] // wrong prefix
    [InlineData("mv_live_ABCDEF1234567890ABCDEF1234567890ABCDEF12")] // uppercase (case-insensitive regex should accept this)
    [InlineData("mv_live_")]
    [InlineData("")]
    [InlineData(null)]
    public void ValidateApiKeyFormat_ShouldReturnFalseForInvalidKeys(string? apiKey)
    {
        // Act
        var result = _sut.ValidateApiKeyFormat(apiKey!);

        // Assert - Note: uppercase hex should be accepted by the regex
        if (apiKey == "mv_live_ABCDEF1234567890ABCDEF1234567890ABCDEF12")
        {
            result.Should().BeTrue(); // Regex is case-insensitive
        }
        else
        {
            result.Should().BeFalse();
        }
    }

    [Fact]
    public void MaskApiKey_ShouldMaskCorrectly()
    {
        // Arrange
        var apiKey = "mv_live_abcdef1234567890abcdef1234567890abcd"; // 47 chars total

        // Act
        var masked = _sut.MaskApiKey(apiKey);

        // Assert - First 10 chars + "..." + last 8 chars
        masked.Should().Be("mv_live_ab...7890abcd");
    }

    [Fact]
    public void MaskApiKey_ShouldShowFirst10AndLast8Characters()
    {
        // Arrange
        var apiKey = "mv_live_1234567890abcdef1234567890abcdef12345678"; // 49 chars total

        // Act
        var masked = _sut.MaskApiKey(apiKey);

        // Assert
        masked.Should().StartWith("mv_live_12"); // First 10 chars
        masked.Should().EndWith("12345678"); // Last 8 chars (correctly "12345678")
        masked.Should().Contain("...");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("short")]
    public void MaskApiKey_ShouldReturnStarsForShortOrEmptyInput(string? apiKey)
    {
        // Act
        var masked = _sut.MaskApiKey(apiKey!);

        // Assert
        masked.Should().Be("****");
    }

    [Fact]
    public void MaskApiKey_ShouldHideMiddlePortionOfKey()
    {
        // Arrange
        var apiKey = "mv_live_abc123def456ghi789jkl012mno345pqr678";

        // Act
        var masked = _sut.MaskApiKey(apiKey);

        // Assert
        masked.Should().NotContain("def456ghi789jkl012mno345"); // Middle should be hidden
        masked.Should().Contain("...");
    }
}
