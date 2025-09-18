#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Utilities;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class CryptographyUtilityTests
{
    // ─────────────────────────────────────────────────────────────────────
    // HashPassword / VerifyPassword
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void HashPassword_WithValidPassword_ReturnsDifferentStringEachCall()
    {
        var hash1 = CryptographyUtility.HashPassword("secret");
        var hash2 = CryptographyUtility.HashPassword("secret");

        hash1.Should().NotBeNullOrEmpty();
        hash2.Should().NotBeNullOrEmpty();
        hash1.Should().NotBe(hash2, "each call uses a random salt");
    }

    [Fact]
    public void HashPassword_WithEmptyPassword_ThrowsArgumentException()
    {
        var act = () => CryptographyUtility.HashPassword(string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        const string password = "my-super-secret-password";
        var hash = CryptographyUtility.HashPassword(password);

        CryptographyUtility.VerifyPassword(password, hash).Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ReturnsFalse()
    {
        var hash = CryptographyUtility.HashPassword("correct-password");

        CryptographyUtility.VerifyPassword("wrong-password", hash).Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithEmptyPassword_ReturnsFalse()
    {
        var hash = CryptographyUtility.HashPassword("some-password");

        CryptographyUtility.VerifyPassword(string.Empty, hash).Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithEmptyHash_ReturnsFalse()
    {
        CryptographyUtility.VerifyPassword("password", string.Empty).Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithCorruptedHash_ReturnsFalse()
    {
        CryptographyUtility.VerifyPassword("password", "not-a-valid-base64-hash!!!").Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────
    // GenerateToken
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateToken_WithDefaultLength_ReturnsNonEmptyBase64String()
    {
        var token = CryptographyUtility.GenerateToken();

        token.Should().NotBeNullOrEmpty();
        var bytes = Convert.FromBase64String(token);
        bytes.Should().HaveCount(32);
    }

    [Fact]
    public void GenerateToken_WithCustomLength_ReturnsCorrectByteLength()
    {
        var token = CryptographyUtility.GenerateToken(64);
        var bytes = Convert.FromBase64String(token);
        bytes.Should().HaveCount(64);
    }

    [Fact]
    public void GenerateToken_TwoCallsWithSameLength_ReturnDifferentValues()
    {
        var t1 = CryptographyUtility.GenerateToken(32);
        var t2 = CryptographyUtility.GenerateToken(32);
        t1.Should().NotBe(t2);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    public void GenerateToken_WithLengthBelow16_ThrowsArgumentOutOfRangeException(int length)
    {
        var act = () => CryptographyUtility.GenerateToken(length);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // GenerateApiKey
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateApiKey_WithDefaultLength_ReturnsAlphanumericString()
    {
        var key = CryptographyUtility.GenerateApiKey();

        key.Should().HaveLength(32);
        key.Should().MatchRegex("^[A-Za-z0-9]+$");
    }

    [Fact]
    public void GenerateApiKey_WithCustomLength_ReturnsCorrectLength()
    {
        var key = CryptographyUtility.GenerateApiKey(48);
        key.Should().HaveLength(48);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void GenerateApiKey_WithLengthBelow16_ThrowsArgumentOutOfRangeException(int length)
    {
        var act = () => CryptographyUtility.GenerateApiKey(length);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // ComputeSha256
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ComputeSha256_WithKnownInput_ReturnsExpectedHexString()
    {
        // SHA-256("hello") = 2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824
        var result = CryptographyUtility.ComputeSha256("hello");
        result.Should().BeEquivalentTo("2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824");
    }

    [Fact]
    public void ComputeSha256_SameInputTwice_ProducesSameHash()
    {
        var h1 = CryptographyUtility.ComputeSha256("consistent");
        var h2 = CryptographyUtility.ComputeSha256("consistent");
        h1.Should().Be(h2);
    }

    [Fact]
    public void ComputeSha256_WithEmptyInput_ThrowsArgumentException()
    {
        var act = () => CryptographyUtility.ComputeSha256(string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // ComputeHmacSha256
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ComputeHmacSha256_SameInputAndKey_ProducesSameHash()
    {
        var h1 = CryptographyUtility.ComputeHmacSha256("message", "secret-key");
        var h2 = CryptographyUtility.ComputeHmacSha256("message", "secret-key");
        h1.Should().Be(h2);
    }

    [Fact]
    public void ComputeHmacSha256_DifferentKeys_ProduceDifferentHashes()
    {
        var h1 = CryptographyUtility.ComputeHmacSha256("message", "key-one");
        var h2 = CryptographyUtility.ComputeHmacSha256("message", "key-two");
        h1.Should().NotBe(h2);
    }

    [Fact]
    public void ComputeHmacSha256_WithEmptyInput_ThrowsArgumentException()
    {
        var act = () => CryptographyUtility.ComputeHmacSha256(string.Empty, "key");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ComputeHmacSha256_WithEmptyKey_ThrowsArgumentException()
    {
        var act = () => CryptographyUtility.ComputeHmacSha256("message", string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // EncryptAes256 / DecryptAes256
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void EncryptAes256_WithValidInput_ReturnsBase64Ciphertext()
    {
        const string key = "this-is-a-32-character-key!!!!!!";
        var ciphertext = CryptographyUtility.EncryptAes256("hello world", key);

        ciphertext.Should().NotBeNullOrEmpty();
        var act = () => Convert.FromBase64String(ciphertext);
        act.Should().NotThrow("result should be valid base64");
    }

    [Fact]
    public void DecryptAes256_AfterEncrypt_RecoverOriginalPlaintext()
    {
        const string key = "this-is-a-32-character-key!!!!!!";
        const string original = "sensitive data to encrypt";

        var ciphertext = CryptographyUtility.EncryptAes256(original, key);
        var plaintext = CryptographyUtility.DecryptAes256(ciphertext, key);

        plaintext.Should().Be(original);
    }

    [Fact]
    public void EncryptAes256_WithShortKey_ThrowsArgumentException()
    {
        var act = () => CryptographyUtility.EncryptAes256("data", "short");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EncryptAes256_WithEmptyPlaintext_ThrowsArgumentException()
    {
        const string key = "this-is-a-32-character-key!!!!!";
        var act = () => CryptographyUtility.EncryptAes256(string.Empty, key);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DecryptAes256_WithTamperedCiphertext_ThrowsInvalidOperationException()
    {
        const string key = "this-is-a-32-character-key!!!!!!";
        var ciphertext = CryptographyUtility.EncryptAes256("data", key);

        // Flip a byte in the ciphertext
        var bytes = Convert.FromBase64String(ciphertext);
        bytes[20] ^= 0xFF;
        var tampered = Convert.ToBase64String(bytes);

        var act = () => CryptographyUtility.DecryptAes256(tampered, key);
        act.Should().Throw<InvalidOperationException>();
    }
}
