# ValidationUtility

A static utility class providing common validation and sanitization methods for strings, collections, and numeric ranges. Designed for input validation in gRPC-web bridge services and related components.

## API

### `ValidateNotEmpty(string? input)`

Validates that a string input is neither `null` nor empty (after trimming whitespace).

- **Parameters**
  - `input` – The string to validate.
- **Returns**
  - `(bool Valid, string? Error)` – `Valid` is `true` if the input is non-empty; otherwise `false`. `Error` contains a descriptive message if validation fails.
- **Throws**
  - `ArgumentNullException` – If `input` is `null`.

### `ValidateStringLength(string? input, int minLength = 0, int maxLength = int.MaxValue)`

Validates that a string's length falls within a specified inclusive range.

- **Parameters**
  - `input` – The string to validate.
  - `minLength` – Minimum allowed length (inclusive). Defaults to `0`.
  - `maxLength` – Maximum allowed length (inclusive). Defaults to `int.MaxValue`.
- **Returns**
  - `(bool Valid, string? Error)` – `Valid` is `true` if the string length is within bounds; otherwise `false`. `Error` contains a descriptive message if validation fails.
- **Throws**
  - `ArgumentOutOfRangeException` – If `minLength` > `maxLength`.
  - `ArgumentNullException` – If `input` is `null`.

### `ValidateEmail(string? input)`

Validates that a string conforms to a basic email format.

- **Parameters**
  - `input` – The email address to validate.
- **Returns**
  - `(bool Valid, string? Error)` – `Valid` is `true` if the input matches a basic email pattern; otherwise `false`. `Error` contains a descriptive message if validation fails.
- **Throws**
  - `ArgumentNullException` – If `input` is `null`.

### `ValidateUrl(string? input)`

Validates that a string conforms to a basic URL format (supports `http`, `https`, and relative paths).

- **Parameters**
  - `input` – The URL to validate.
- **Returns**
  - `(bool Valid, string? Error)` – `Valid` is `true` if the input matches a basic URL pattern; otherwise `false`. `Error` contains a descriptive message if validation fails.
- **Throws**
  - `ArgumentNullException` – If `input` is `null`.

### `ValidateIpAddress(string? input)`

Validates that a string represents a valid IPv4 or IPv6 address.

- **Parameters**
  - `input` – The IP address string to validate.
- **Returns**
  - `(bool Valid, string? Error)` – `Valid` is `true` if the input is a valid IP address; otherwise `false`. `Error` contains a descriptive message if validation fails.
- **Throws**
  - `ArgumentNullException` – If `input` is `null`.

### `ValidateServiceId(string? input)`

Validates that a string is a valid service identifier (alphanumeric with optional hyphens and underscores, 1–128 characters).

- **Parameters**
  - `input` – The service identifier to validate.
- **Returns**
  - `(bool Valid, string? Error)` – `Valid` is `true` if the input matches the service ID pattern; otherwise `false`. `Error` contains a descriptive message if validation fails.
- **Throws**
  - `ArgumentNullException` – If `input` is `null`.

### `ValidateMethodName(string? input)`

Validates that a string is a valid method name (alphanumeric with optional dots and underscores, 1–128 characters).

- **Parameters**
  - `input` – The method name to validate.
- **Returns**
  - `(bool Valid, string? Error)` – `Valid` is `true` if the input matches the method name pattern; otherwise `false`. `Error` contains a descriptive message if validation fails.
- **Throws**
  - `ArgumentNullException` – If `input` is `null`.

### `ValidateRange(double value, double min, double max)`

Validates that a numeric value falls within a specified inclusive range.

- **Parameters**
  - `value` – The value to validate.
  - `min` – Minimum allowed value (inclusive).
  - `max` – Maximum allowed value (inclusive).
- **Returns**
  - `(bool Valid, string? Error)` – `Valid` is `true` if the value is within bounds; otherwise `false`. `Error` contains a descriptive message if validation fails.
- **Throws**
  - `ArgumentOutOfRangeException` – If `min` > `max`.

### `ValidatePort(int port)`

Validates that an integer represents a valid TCP/UDP port number (1–65535).

- **Parameters**
  - `port` – The port number to validate.
- **Returns**
  - `(bool Valid, string? Error)` – `Valid` is `true` if the port is within the valid range; otherwise `false`. `Error` contains a descriptive message if validation fails.

### `ValidateNotEmpty<T>(IEnumerable<T>? items)`

Validates that a collection is neither `null` nor empty.

- **Parameters**
  - `items` – The collection to validate.
- **Returns**
  - `(bool Valid, string? Error)` – `Valid` is `true` if the collection is non-empty; otherwise `false`. `Error` contains a descriptive message if validation fails.
- **Throws**
  - `ArgumentNullException` – If `items` is `null`.

### `ValidateRequiredKeys(IDictionary<string, object?>? dictionary, IEnumerable<string> requiredKeys)`

Validates that a dictionary contains all specified required keys.

- **Parameters**
  - `dictionary` – The dictionary to validate.
  - `requiredKeys` – Keys that must be present in the dictionary.
- **Returns**
  - `(bool Valid, string? Error)` – `Valid` is `true` if all required keys are present; otherwise `false`. `Error` contains a descriptive message if validation fails.
- **Throws**
  - `ArgumentNullException` – If `dictionary` or `requiredKeys` is `null`.

### `ValidatePattern(string? input, string pattern, RegexOptions options = RegexOptions.None)`

Validates that a string matches a specified regular expression pattern.

- **Parameters**
  - `input` – The string to validate.
  - `pattern` – The regular expression pattern to match.
  - `options` – Regex options (e.g., case-insensitive). Defaults to `None`.
- **Returns**
  - `(bool Valid, string? Error)` – `Valid` is `true` if the input matches the pattern; otherwise `false`. `Error` contains a descriptive message if validation fails.
- **Throws**
  - `ArgumentException` – If `pattern` is `null` or invalid.
  - `ArgumentNullException` – If `input` is `null`.

### `ValidateJwtFormat(string? token)`

Validates that a string is a well-formed JWT (JSON Web Token) with three dot-separated base64url-encoded segments.

- **Parameters**
  - `token` – The JWT string to validate.
- **Returns**
  - `(bool Valid, string? Error)` – `Valid` is `true` if the input is a valid JWT format; otherwise `false`. `Error` contains a descriptive message if validation fails.
- **Throws**
  - `ArgumentNullException` – If `token` is `null`.

### `SanitizeInput(string? input)`

Sanitizes user input by trimming whitespace and escaping HTML special characters.

- **Parameters**
  - `input` – The string to sanitize.
- **Returns**
  - `string` – The sanitized string. Returns `string.Empty` if `input` is `null`.
- **Throws**
  - None.

## Usage
