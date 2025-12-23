# ConfigurationException

`ConfigurationException` is a specialized exception type used to signal configuration-related errors in the `grpc-web-bridge` project. It provides additional context about the configuration key and value that caused the failure, aiding in diagnostics and debugging.

## API

### `public string? ConfigurationKey`
Gets the configuration key associated with the exception. This property may be `null` if the key is not available or not applicable.

### `public string? ConfigurationValue`
Gets the configuration value associated with the exception. This property may be `null` if the value is not available or not applicable.

### `public ConfigurationException() : base()`
Initializes a new instance of the `ConfigurationException` class with default values for all properties.

### `public ConfigurationException(string message) : base(message)`
Initializes a new instance of the `ConfigurationException` class with a specified error message.

**Parameters**
- `message` (string): The message that describes the error.

### `public ConfigurationException(string message, Exception? innerException)`
Initializes a new instance of the `ConfigurationException` class with a specified error message and a reference to the inner exception that is the cause of this exception.

**Parameters**
- `message` (string): The message that describes the error.
- `innerException` (Exception?): The exception that is the cause of the current exception, or `null` if no inner exception is specified.

### `public ConfigurationException(string message, string? configurationKey, string? configurationValue)`
Initializes a new instance of the `ConfigurationException` class with a specified error message, configuration key, and configuration value.

**Parameters**
- `message` (string): The message that describes the error.
- `configurationKey` (string?): The configuration key associated with the error.
- `configurationValue` (string?): The configuration value associated with the error.

### `public override string ToString()`
Returns a string representation of the exception, including the error message, configuration key, and configuration value (if available).

**Returns**
- (string): A string representation of the exception.

## Usage

### Example 1: Basic Usage
