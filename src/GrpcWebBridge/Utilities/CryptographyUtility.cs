#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Cryptography;
using System.Text;

namespace GrpcWebBridge.Utilities;

/// <summary>
/// Cryptographic utilities for hashing, encryption, and token generation.
/// Provides secure methods for password hashing, data encryption, and random token creation.
/// </summary>
public static class CryptographyUtility
{
    private const int HashIterations = 10000;
    private const int SaltSize = 16; // 128 bits
    private const int KeySize = 32; // 256 bits

    /// <summary>
    /// Generates a secure hash of the input string using PBKDF2.
    /// Includes salt and iteration count for strong protection against rainbow table attacks.
    /// </summary>
    public static string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        using (var rng = RandomNumberGenerator.Create())
        {
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, HashIterations, HashAlgorithmName.SHA256))
            {
                var hash = pbkdf2.GetBytes(KeySize);
                var hashWithSalt = new byte[SaltSize + hash.Length];
                Array.Copy(salt, 0, hashWithSalt, 0, SaltSize);
                Array.Copy(hash, 0, hashWithSalt, SaltSize, hash.Length);

                return Convert.ToBase64String(hashWithSalt);
            }
        }
    }

    /// <summary>
    /// Verifies a password against a previously generated hash.
    /// Uses constant-time comparison to prevent timing attacks.
    /// </summary>
    public static bool VerifyPassword(string password, string hash)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        ArgumentException.ThrowIfNullOrEmpty(hash);

        try
        {
            var hashBytes = Convert.FromBase64String(hash);
            if (hashBytes.Length != SaltSize + KeySize)
                return false;

            var salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, HashIterations, HashAlgorithmName.SHA256))
            {
                var computedHash = pbkdf2.GetBytes(KeySize);
                var storedHash = new byte[KeySize];
                Array.Copy(hashBytes, SaltSize, storedHash, 0, KeySize);

                // Constant-time comparison to prevent timing attacks
                return ConstantTimeComparison(computedHash, storedHash);
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates a random token suitable for authentication or CSRF protection.
    /// </summary>
    public static string GenerateToken(int length = 32)
    {
        if (length < 16)
            throw new ArgumentOutOfRangeException(nameof(length), length, "Token length must be at least 16 bytes");

        using (var rng = RandomNumberGenerator.Create())
        {
            var tokenBytes = new byte[length];
            rng.GetBytes(tokenBytes);
            return Convert.ToBase64String(tokenBytes);
        }
    }

    /// <summary>
    /// Generates a random API key with alphanumeric characters.
    /// More suitable for human-readable tokens.
    /// </summary>
    public static string GenerateApiKey(int length = 32)
    {
        if (length < 16)
            throw new ArgumentOutOfRangeException(nameof(length), length, "API key length must be at least 16 characters");

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var key = new StringBuilder();

        using (var rng = RandomNumberGenerator.Create())
        {
            var buffer = new byte[length];
            rng.GetBytes(buffer);

            foreach (var b in buffer)
            {
                key.Append(chars[b % chars.Length]);
            }
        }

        return key.ToString();
    }

    /// <summary>
    /// Computes SHA256 hash of a string.
    /// Useful for checksums and data integrity verification.
    /// </summary>
    public static string ComputeSha256(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);

        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hashedBytes);
        }
    }

    /// <summary>
    /// Computes HMAC-SHA256 of a string with a secret key.
    /// Useful for message authentication and integrity verification.
    /// </summary>
    public static string ComputeHmacSha256(string input, string secretKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        ArgumentException.ThrowIfNullOrEmpty(secretKey);

        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash);
        }
    }

    /// <summary>
    /// Encrypts a string using AES-256-GCM.
    /// Returns Base64-encoded ciphertext with IV and authentication tag.
    /// </summary>
    public static string EncryptAes256(string plaintext, string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(plaintext);
        ArgumentException.ThrowIfNullOrEmpty(key);

        if (key.Length < 32)
            throw new ArgumentException("Key must be at least 32 characters", nameof(key));

        try
        {
            var keyBytes = Encoding.UTF8.GetBytes(key[..32]); // Use first 32 chars as 256-bit key
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var iv = new byte[12]; // 96-bit IV for GCM
            var tag = new byte[16]; // 128-bit auth tag

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }

            using (var cipher = new AesGcm(keyBytes))
            {
                var ciphertext = new byte[plaintextBytes.Length];
                cipher.Encrypt(iv, plaintextBytes, ciphertext, tag);

                // Combine IV + ciphertext + tag
                var result = new byte[iv.Length + ciphertext.Length + tag.Length];
                Array.Copy(iv, 0, result, 0, iv.Length);
                Array.Copy(ciphertext, 0, result, iv.Length, ciphertext.Length);
                Array.Copy(tag, 0, result, iv.Length + ciphertext.Length, tag.Length);

                return Convert.ToBase64String(result);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Encryption failed", ex);
        }
    }

    /// <summary>
    /// Decrypts an AES-256-GCM encrypted string.
    /// </summary>
    public static string DecryptAes256(string ciphertext, string key)
    {
        if (string.IsNullOrEmpty(ciphertext))
            throw new ArgumentException("Ciphertext cannot be null or empty", nameof(ciphertext));

        if (string.IsNullOrEmpty(key) || key.Length < 32)
            throw new ArgumentException("Key must be at least 32 characters", nameof(key));

        try
        {
            var keyBytes = Encoding.UTF8.GetBytes(key[..32]);
            var result = Convert.FromBase64String(ciphertext);

            const int ivLength = 12;
            const int tagLength = 16;
            var decryptedLength = result.Length - ivLength - tagLength;

            if (decryptedLength <= 0)
                throw new InvalidOperationException("Invalid ciphertext format");

            var iv = new byte[ivLength];
            var encryptedData = new byte[decryptedLength];
            var tag = new byte[tagLength];

            Array.Copy(result, 0, iv, 0, ivLength);
            Array.Copy(result, ivLength, encryptedData, 0, decryptedLength);
            Array.Copy(result, ivLength + decryptedLength, tag, 0, tagLength);

            using (var cipher = new AesGcm(keyBytes))
            {
                var plaintext = new byte[encryptedData.Length];
                cipher.Decrypt(iv, encryptedData, tag, plaintext);
                return Encoding.UTF8.GetString(plaintext);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Decryption failed", ex);
        }
    }

    /// <summary>
    /// Constant-time byte array comparison to prevent timing attacks.
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static bool ConstantTimeComparison(byte[] a, byte[] b)
    {
        // Fix: handle null array edge cases
        if (a is null || b is null)
            return false;

        if (a.Length != b.Length)
            return false;

        int result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }
}
