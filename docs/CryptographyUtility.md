# CryptographyUtility

A static utility class providing common cryptographic operations used for password hashing, token generation, API key management, and data encryption/decryption in the `grpc-web-bridge` project. These methods abstract low-level cryptographic operations with consistent defaults and error handling.

## API

### `public static string HashPassword(string password)`

Hashes a plaintext password using PBKDF2 with HMAC-SHA256 and a random salt. The salt is prepended to the resulting hash in the format `algorithm:salt:hash`.

- **Parameters**
  - `password` – The plaintext password to hash. Must not be null or empty.

- **Return value**
  - A string containing the algorithm identifier, base64-encoded salt, and base64-encoded hash, separated by colons.

- **Exceptions**
  - Throws `ArgumentNullException` if `password` is null.
  - Throws `ArgumentException` if `password` is empty.

---

### `public static bool VerifyPassword(string hashedPassword, string password)`

Verifies a plaintext password against a previously hashed password.

- **Parameters**
  - `hashedPassword` – The stored password hash in the format `algorithm:salt:hash`.
  - `password` – The plaintext password to verify.

- **Return value**
  - `true` if the password matches the hash; otherwise, `false`.

- **Exceptions**
  - Throws `ArgumentNullException` if either parameter is null.
  - Throws `FormatException` if `hashedPassword` is malformed.

---

### `public static string GenerateToken(int length = 32)`

Generates a cryptographically secure random token of the specified length.

- **Parameters**
  - `length` – The desired length of the token in bytes. Defaults to 32.

- **Return value**
  - A base64-encoded string of random bytes.

- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `length` is less than 1.

---

### `public static string GenerateApiKey(int length = 32)`

Generates a cryptographically secure API key of the specified length.

- **Parameters**
  - `length` – The desired length of the API key in bytes. Defaults to 32.

- **Return value**
  - A base64-encoded string of random bytes.

- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `length` is less than 1.

---

### `public static string ComputeSha256(string input)`

Computes the SHA-256 hash of the input string, returning the hash as a hexadecimal string.

- **Parameters**
  - `input` – The string to hash. Must not be null.

- **Return value**
  - A 64-character hexadecimal string representing the SHA-256 hash.

- **Exceptions**
  - Throws `ArgumentNullException` if `input` is null.

---
### `public static string ComputeHmacSha256(string input, string key)`

Computes the HMAC-SHA256 of the input string using the provided key, returning the hash as a hexadecimal string.

- **Parameters**
  - `input` – The string to authenticate.
  - `key` – The secret key used for HMAC. Must not be null.

- **Return value**
  - A 64-character hexadecimal string representing the HMAC-SHA256 digest.

- **Exceptions**
  - Throws `ArgumentNullException` if either parameter is null.

---
### `public static string EncryptAes256(string plaintext, string key)`

Encrypts a plaintext string using AES-256 in CBC mode with PKCS7 padding. The IV is prepended to the ciphertext in the output.

- **Parameters**
  - `plaintext` – The text to encrypt. Must not be null.
  - `key` – The 32-byte AES key, base64-encoded. Must not be null or empty.

- **Return value**
  - A base64-encoded string containing IV (16 bytes) + ciphertext.

- **Exceptions**
  - Throws `ArgumentNullException` if either parameter is null.
  - Throws `CryptographicException` if key size is invalid or encryption fails.

---
### `public static string DecryptAes256(string ciphertext, string key)`

Decrypts a ciphertext string encrypted with `EncryptAes256`.

- **Parameters**
  - `ciphertext` – The base64-encoded ciphertext including IV. Must not be null or empty.
  - `key` – The 32-byte AES key, base64-encoded. Must not be null or empty.

- **Return value**
  - The decrypted plaintext string.

- **Exceptions**
  - Throws `ArgumentNullException` if either parameter is null.
  - Throws `FormatException` if `ciphertext` is malformed.
  - Throws `CryptographicException` if decryption fails (e.g., wrong key or corrupted data).

## Usage
