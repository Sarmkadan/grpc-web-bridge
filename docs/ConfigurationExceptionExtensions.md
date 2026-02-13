# ConfigurationExceptionExtensions

Provides a set of fluent extension methods for enriching and inspecting `ConfigurationException` instances, allowing developers to attach contextual information such as messages, keys, and values, as well as to query and format the exception for logging or display purposes.

## API

### WithMessage
```csharp
public static ConfigurationException WithMessage(this ConfigurationException exception, string message)
```
**Purpose** – Returns a new `ConfigurationException` whose `Message` property is replaced with the supplied `message`, preserving the original inner exception and any existing key/value data.  
**Parameters**  
- `exception`: The exception to modify; must not be `null`.  
- `message`: The replacement message; may be `null` or empty.  
**Return value** – A `ConfigurationException` instance with the updated message.  
**Exceptions** – Throws `ArgumentNullException` if `exception` is `null`.

### WithKey
```csharp
public static ConfigurationException WithKey(this ConfigurationException exception, string key)
```
**Purpose** – Returns a new `ConfigurationException` that records the specified `key` as part of its data collection, useful for identifying the configuration entry that caused the failure.  
**Parameters**  
- `exception`: The exception to modify; must not be `null`.  
- `key`: The configuration key associated with the error; may be `null`.  
**Return value** – A `ConfigurationException` instance with the key stored in its `Data` dictionary under the well‑known key `"ConfigKey"`.  
**Exceptions** – Throws `ArgumentNullException` if `exception` is `null`.

### WithValue
```csharp
public static ConfigurationException WithValue(this ConfigurationException exception, object value)
```
**Purpose** – Returns a new `ConfigurationException` that records the supplied `value` (the offending configuration value) in its data collection.  
**Parameters**  
- `exception`: The exception to modify; must not be `null`.  
- `value`: The configuration value that triggered the exception; may be `null`.  
**Return value** – A `ConfigurationException` instance with the value stored in its `Data` dictionary under the well‑known key `"ConfigValue"`.  
**Exceptions** – Throws `ArgumentNullException` if `exception` is `null`.

### WithKeyValue
```csharp
public static ConfigurationException WithKeyValue(this ConfigurationException exception, string key, object value)
```
**Purpose** – Convenience overload that attaches both a key and a value to the exception in a single call.  
**Parameters**  
- `exception`: The exception to modify; must not be `null`.  
- `key`: The configuration key; may be `null`.  
- `value`: The configuration value; may be `null`.  
**Return value** – A `ConfigurationException` instance with both `key` and `value` stored in its `Data` dictionary under `"ConfigKey"` and `"ConfigValue"` respectively.  
**Exceptions** – Throws `ArgumentNullException` if `exception` is `null`.

### HasKey
```csharp
public static bool HasKey(this ConfigurationException exception, string key)
```
**Purpose** – Determines whether the exception’s data contains a stored key that matches the supplied `key`.  
**Parameters**  
- `exception`: The exception to inspect; must not be `null`.  
- `key`: The key to look for; may be `null`.  
**Return value** – `true` if the exception’s `Data` contains an entry with key `"ConfigKey"` whose value equals `key`; otherwise `false`.  
**Exceptions** – Throws `ArgumentNullException` if `exception` is `null`.

### GetFormattedMessage
```csharp
public static string GetFormattedMessage(this ConfigurationException exception)
```
**Purpose** – Produces a human‑readable string that combines the exception’s `Message` with any stored key and value, suitable for logging or user‑facing error display.  
**Parameters**  
- `exception`: The exception to format; must not be `null`.  
**Return value** – A string formatted as:  
`"{Message} (Key: {Key}, Value: {Value})"` where missing components are omitted.  
**Exceptions** – Throws `ArgumentNullException` if `exception` is `null`.

## Usage

### Example 1: Building a detailed configuration error
```csharp
try
{
    var value = config.GetInt("timeout");
}
catch (ConfigurationException ex)
{
    var detailed = ex
        .WithMessage("The timeout setting must be a positive integer.")
        .WithKey("timeout")
        .WithValue(config["timeout"]);

    throw detailed; // Preserves stack trace while adding context
}
```

### Example 2: Checking for a specific mis‑configured key before logging
```csharp
if (ex.HasKey("timeout"))
{
    logger.Error(GetFormattedMessage(ex));
}
else
{
    logger.Error(ex.Message);
}
```

## Notes

- All extension methods are pure; they do not mutate the original `ConfigurationException` instance but return a new instance with the requested data added.  
- The methods are safe to call concurrently on distinct exception instances because they only read from and write to the exception’s `Data` dictionary, which is not shared across instances.  
- Passing `null` for the `exception` argument results in an `ArgumentNullException`; callers must ensure the source exception is non‑null before invoking any of these members.  
- If `WithKey` or `WithValue` are called multiple times on the same exception chain, later calls overwrite the previously stored `"ConfigKey"` or `"ConfigValue"` entry.  
- `GetFormattedMessage` gracefully handles missing key or value entries by omitting the corresponding portion from the formatted string.  
- No static state is maintained; therefore, the type itself is thread‑safe for simultaneous invocation from multiple threads.
