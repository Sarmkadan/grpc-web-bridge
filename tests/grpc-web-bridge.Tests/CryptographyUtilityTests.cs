#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Utilities;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Tests for the CryptographyUtility class.
/// </summary>
public sealed class CryptographyUtilityTests
{
    /// <summary>
    /// Tests that password hashes use a random salt.
    /// </summary>
    [Fact]
    public void HashPassword_WithSamePassword_ReturnsDistinctHashes()
    {
        // Arrange
        const string password = "correct horse battery staple";

        // Act
        var firstHash = CryptographyUtility.HashPassword(password);
        var secondHash = CryptographyUtility.HashPassword(password);

        // Assert
        firstHash.Should().NotBe(secondHash);
    }

    /// <summary>
    /// Tests password verification with the correct password.
    /// </summary>
    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        const string password = "a-secure-password";
        var hash = CryptographyUtility.HashPassword(password);

        // Act
        var result = CryptographyUtility.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests password verification with an incorrect password.
    /// </summary>
    [Fact]
    public void VerifyPassword_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        var hash = CryptographyUtility.HashPassword("correct-password");

        // Act
        var result = CryptographyUtility.VerifyPassword("wrong-password", hash);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that a null or empty password cannot be hashed.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void HashPassword_WithNullOrEmptyPassword_ThrowsArgumentException(string? password)
    {
        // Arrange & Act
        var act = () => CryptographyUtility.HashPassword(password!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that generated tokens have the requested byte length and are unique.
    /// </summary>
    [Fact]
    public void GenerateToken_WithRequestedLength_ReturnsDistinctTokensOfExpectedLength()
    {
        // Arrange
        const int length = 48;

        // Act
        var firstToken = CryptographyUtility.GenerateToken(length);
        var secondToken = CryptographyUtility.GenerateToken(length);

        // Assert
        Convert.FromBase64String(firstToken).Should().HaveCount(length);
        Convert.FromBase64String(secondToken).Should().HaveCount(length);
        firstToken.Should().NotBe(secondToken);
    }

    /// <summary>
    /// Tests that generated API keys have the requested character length and are unique.
    /// </summary>
    [Fact]
    public void GenerateApiKey_WithRequestedLength_ReturnsDistinctKeysOfExpectedLength()
    {
        // Arrange
        const int length = 48;

        // Act
        var firstKey = CryptographyUtility.GenerateApiKey(length);
        var secondKey = CryptographyUtility.GenerateApiKey(length);

        // Assert
        firstKey.Should().HaveLength(length);
        secondKey.Should().HaveLength(length);
        firstKey.Should().NotBe(secondKey);
    }

    /// <summary>
    /// Tests that AES-256 encryption and decryption preserve the original plaintext.
    /// </summary>
    [Fact]
    public void DecryptAes256_AfterEncryptAes256_ReturnsOriginalPlaintext()
    {
        // Arrange
        const string plaintext = "Sensitive data with Unicode: åß中";
        const string key = "0123456789abcdef0123456789abcdef";

        // Act
        var ciphertext = CryptographyUtility.EncryptAes256(plaintext, key);
        var decrypted = CryptographyUtility.DecryptAes256(ciphertext, key);

        // Assert
        ciphertext.Should().NotBe(plaintext);
        decrypted.Should().Be(plaintext);
    }
}
