# CryptographyUtilityTests

`CryptographyUtilityTests` is the unit test suite for the `CryptographyUtility` class within the `grpc-web-bridge` project. It validates the correctness, edge-case handling, and consistency of cryptographic helper methods, including password hashing, password verification, token generation, API key generation, SHA-256 hashing, and HMAC-SHA-256 computation. The tests ensure that the underlying utility behaves predictably under both normal and exceptional conditions.

## API

### HashPassword_WithValidPassword_ReturnsDifferentStringEachCall
Verifies that hashing a valid, non-empty password produces a non-empty string and that consecutive calls with the same input yield different outputs due to salting.  
**Parameters:** None (test method).  
**Returns:** `void`.  
**Throws:** Test fails if the returned string is null or empty, or if two hashes of the same password are identical.

### HashPassword_WithEmptyPassword_ThrowsArgumentException
Ensures that passing an empty string to the password hashing method throws an `ArgumentException`.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Test expects the underlying method to throw `ArgumentException`; the test itself fails if no exception or a different exception type is raised.

### VerifyPassword_WithCorrectPassword_ReturnsTrue
Confirms that verifying a password against its own hash returns `true`.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Test fails if the verification result is `false`.

### VerifyPassword_WithWrongPassword_ReturnsFalse
Confirms that verifying an incorrect password against a hash returns `false`.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Test fails if the verification result is `true`.

### VerifyPassword_WithEmptyPassword_ReturnsFalse
Ensures that verifying an empty string against a valid hash returns `false`.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Test fails if the verification result is `true`.

### VerifyPassword_WithEmptyHash_ReturnsFalse
Ensures that verifying a valid password against an empty hash string returns `false`.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Test fails if the verification result is `true`.

### VerifyPassword_WithCorruptedHash_ReturnsFalse
Ensures that verifying a password against a tampered or malformed hash returns `false` rather than throwing or returning `true`.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Test fails if the verification result is `true` or if an unexpected exception occurs.

### GenerateToken_WithDefaultLength_ReturnsNonEmptyBase64String
Validates that generating a token with the default length produces a non-null, non-empty Base64 string.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Test fails if the result is null or empty.

### GenerateToken_WithCustomLength_ReturnsCorrectByteLength
Verifies that requesting a token of a specific byte length produces a Base64 string whose decoded byte array matches the requested length.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Test fails if the decoded byte length differs from the requested length.

### GenerateToken_TwoCallsWithSameLength_ReturnDifferentValues
Ensures that two consecutive token generations with the same length produce different values, confirming randomness.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Test fails if the two generated tokens are identical.

### GenerateToken_WithLengthBelow16_ThrowsArgumentOutOfRangeException
Ensures that requesting a token length less than 16 bytes throws an `ArgumentOutOfRangeException`.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Test expects `ArgumentOutOfRangeException`; fails if no exception or a different exception type is raised.

### GenerateApiKey_WithDefaultLength_ReturnsAlphanumericString
Validates that generating an API key with the default length returns a non-null, non-empty string consisting only of alphanumeric characters.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Test fails if the result is null, empty, or contains non-alphanumeric characters.

### GenerateApiKey_WithCustomLength_ReturnsCorrectLength
Verifies that requesting an API key of a specific length returns a string of exactly that length.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Test fails if the string length does not match the requested length.

### GenerateApiKey_WithLengthBelow16_ThrowsArgumentOutOfRangeException
Ensures that requesting an API key length less than 16 characters throws an `ArgumentOutOfRangeException`.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Test expects `ArgumentOutOfRangeException`; fails if no exception or a different exception type is raised.

### ComputeSha256_WithKnownInput_ReturnsExpectedHexString
Validates that computing the SHA-256 hash of a known input produces the expected, pre-computed hexadecimal string.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Test fails if the computed hash does not match the expected value.

### ComputeSha256_SameInputTwice_ProducesSameHash
Ensures that hashing the same input twice yields identical hexadecimal strings.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Test fails if the two hashes differ.

### ComputeSha256_WithEmptyInput_ThrowsArgumentException
Ensures that passing an empty input to the SHA-256 computation throws an `ArgumentException`.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Test expects `ArgumentException`; fails if no exception or a different exception type is raised.

### ComputeHmacSha256_SameInputAndKey_ProducesSameHash
Validates that computing an HMAC-SHA-256 with the same input and key twice produces identical hexadecimal strings.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Test fails if the two HMACs differ.

### ComputeHmacSha256_DifferentKeys_ProduceDifferentHashes
Ensures that computing HMAC-SHA-256 with the same input but different keys yields different hexadecimal strings.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Test fails if the two HMACs are identical.

### ComputeHmacSha256_WithEmptyInput_ThrowsArgumentException
Ensures that passing an empty input to the HMAC-SHA-256 computation throws an `ArgumentException`.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Test expects `ArgumentException`; fails if no exception or a different exception type is raised.

## Usage

### Example 1: Running a subset of cryptography tests with xUnit
```csharp
using Xunit;

public class CryptographyValidationTests
{
    private readonly CryptographyUtilityTests _tests = new CryptographyUtilityTests();

    [Fact]
    public void ValidatePasswordHashingAndVerification()
    {
        // Run the relevant test methods directly (or via test runner)
        _tests.HashPassword_WithValidPassword_ReturnsDifferentStringEachCall();
        _tests.HashPassword_WithEmptyPassword_ThrowsArgumentException();
        _tests.VerifyPassword_WithCorrectPassword_ReturnsTrue();
        _tests.VerifyPassword_WithWrongPassword_ReturnsFalse();
        _tests.VerifyPassword_WithEmptyPassword_ReturnsFalse();
        _tests.VerifyPassword_WithEmptyHash_ReturnsFalse();
        _tests.VerifyPassword_WithCorruptedHash_ReturnsFalse();
    }
}
```

### Example 2: Validating token and API key generation constraints
```csharp
using Xunit;

public class TokenAndApiKeyTests
{
    private readonly CryptographyUtilityTests _tests = new CryptographyUtilityTests();

    [Fact]
    public void EnsureTokenAndApiKeyGenerationMeetSpecifications()
    {
        _tests.GenerateToken_WithDefaultLength_ReturnsNonEmptyBase64String();
        _tests.GenerateToken_WithCustomLength_ReturnsCorrectByteLength();
        _tests.GenerateToken_TwoCallsWithSameLength_ReturnDifferentValues();
        _tests.GenerateToken_WithLengthBelow16_ThrowsArgumentOutOfRangeException();

        _tests.GenerateApiKey_WithDefaultLength_ReturnsAlphanumericString();
        _tests.GenerateApiKey_WithCustomLength_ReturnsCorrectLength();
        _tests.GenerateApiKey_WithLengthBelow16_ThrowsArgumentOutOfRangeException();
    }
}
```

## Notes

- **Edge cases for empty inputs:** Multiple test methods explicitly verify that empty strings or byte arrays cause `ArgumentException` (for hashing, HMAC, and password hashing). Verification methods treat empty passwords and empty hashes as mismatches, returning `false` rather than throwing.
- **Corrupted hash handling:** The `VerifyPassword_WithCorruptedHash_ReturnsFalse` test implies that the underlying verification implementation is resilient to malformed inputs and does not leak information through exceptions.
- **Randomness and uniqueness:** Token and API key generation tests assert that consecutive calls produce distinct values. While this holds under normal conditions, it is a probabilistic guarantee; test failures due to accidental collisions are extremely unlikely but theoretically possible.
- **Length constraints:** Both token and API key generation enforce a minimum length of 16 (bytes for tokens, characters for API keys). Requests below this threshold consistently throw `ArgumentOutOfRangeException`.
- **Thread safety:** The test suite does not explicitly cover concurrent invocation. The underlying `CryptographyUtility` methods should be assumed safe for concurrent use only if their implementation uses thread-local or properly synchronized primitives; the tests provide no evidence either way.
