# ServiceRegistrationException

The `ServiceRegistrationException` class represents errors encountered during the registration of a service within the `grpc-web-bridge` framework. It provides contextual information, such as the target service name and its associated endpoint, to facilitate debugging and error handling during service initialization.

## API

### Properties

*   `public string? ServiceName { get; set; }`
    The name of the service that failed to register.

*   `public string? ServiceEndpoint { get; set; }`
    The configuration endpoint that was being targeted when the registration failed.

### Constructors

*   `public ServiceRegistrationException() : base()`
    Initializes a new instance of the `ServiceRegistrationException` class.

*   `public ServiceRegistrationException(string message) : base(message, "SERVICE_REGISTRATION_ERROR")`
    Initializes a new instance of the `ServiceRegistrationException` class with a specified error message.

*   `public ServiceRegistrationException(...)`
    Additional constructor overloads are provided to support initialization with varying levels of error context and inner exception details.

### Methods

*   `public override string ToString()`
    Returns a string representation of the exception, including the `ServiceName` and `ServiceEndpoint` if they have been set, in addition to the standard exception message.

## Usage

### Throwing the Exception

```csharp
public void RegisterService(string name, string endpoint)
{
    if (string.IsNullOrEmpty(endpoint))
    {
        throw new ServiceRegistrationException("Endpoint cannot be empty.")
        {
            ServiceName = name,
            ServiceEndpoint = endpoint
        };
    }
    // ... registration logic
}
```

### Catching the Exception

```csharp
try
{
    serviceManager.Register("OrderService", "http://localhost:5000");
}
catch (ServiceRegistrationException ex)
{
    Console.WriteLine($"Registration failed for {ex.ServiceName} at {ex.ServiceEndpoint}.");
    // Handle specific failure...
}
```

## Notes

*   **Edge Cases:** If `ServiceName` or `ServiceEndpoint` properties are not explicitly set after instantiation, they will be `null`. Ensure null checks are performed before accessing these properties to avoid `NullReferenceException` in consuming code.
*   **Thread Safety:** This exception class is designed to be immutable once thrown, making it thread-safe for reading. Like standard .NET exceptions, it is intended to be thrown and caught within the execution flow of a single thread and should not be shared across threads.
