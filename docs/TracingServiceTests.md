# TracingServiceTests

The `TracingServiceTests` class contains unit tests for the `TracingService` component in the `grpc-web-bridge` project. It validates that the tracing infrastructure correctly creates activities, sets expected tags, and handles constructor arguments.

## API

### Dispose
**Purpose:** Releases any resources held by the test instance, typically used to clean up after each test.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Does not throw under normal operation; any exceptions would propagate from underlying disposable resources.

### BridgeActivitySource_Name_IsGrpcWebBridge
**Purpose:** Verifies that the `BridgeActivitySource` static property returns an `ActivitySource` whose `Name` is exactly `"GrpcWebBridge"`.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Throws an assertion exception if the name differs.

### BridgeActivitySource_Source_HasExpectedName
**Purpose:** Confirms that the `ActivitySource` returned by `BridgeActivitySource` possesses the expected name used throughout the library.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Throws an assertion exception if the source name is incorrect.

### Constructor_WithNullLogger_Throws
**Purpose:** Ensures that constructing a `TracingService` with a `null` logger argument results in an exception.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Expects an `ArgumentNullException` (or similar) to be thrown by the constructor.

### Constructor_WithValidLogger_CreatesInstance
**Purpose:** Checks that supplying a non‑null logger yields a successfully instantiated `TracingService` object.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Throws an assertion exception if the instance is `null` after construction.

### StartGrpcCallActivity_WhenListenerActive_ReturnsNonNullActivity
**Purpose:** When an `ActivityListener` is active, validates that `StartGrpcCallActivity` returns a non‑null `Activity`.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Throws an assertion exception if the returned activity is `null`.

### StartGrpcCallActivity_SetsRpcServiceTag
**Purpose:** Asserts that the activity created by `StartGrpcCallActivity` contains a tag with key `rpc.service` set to the expected service name.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Throws an assertion exception if the tag is missing or has an incorrect value.

### StartGrpcCallActivity_SetsRpcMethodTag
**Purpose:** Verifies that the activity produced by `StartGrpcCallActivity` includes an `rpc.method` tag matching the supplied method name.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Throws an assertion exception if the tag is absent or incorrect.

### StartGrpcCallActivity_SetsRpcSystemToGrpc
**Purpose:** Confirms that the `rpc.system` tag on the activity is set to `"grpc"` for gRPC calls.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Throws an assertion exception if the tag is not `"grpc"`.

### StartGrpcCallActivity_SetsInstanceTag
**Purpose:** Ensures that the activity includes an `instance` tag reflecting the host instance identifier.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Throws an assertion exception if the tag is missing or does not match the expected instance value.

### StartGrpcCallActivity_UnaryCall_HasClientKind
**Purpose:** Checks that for a unary call the activity’s `Kind` property is set to `ActivityKind.Client`.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Throws an assertion exception if the kind is not `Client`.

### StartGrpcCallActivity_StreamingStreamingCall_SetsStreamingTag
**Purpose:** Validates that a streaming call results in the activity having a `messaging.system` tag set to `"grpc"` and a `messaging.message_type` tag indicating streaming.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Throws an assertion exception if the streaming tags are absent or incorrect.

### StartGrpcCallActivity_UnaryCall_SetsStreamingTagFalse
**Purpose:** Ensures that for a unary call the streaming‑related tags are either absent or explicitly set to indicate a non‑streaming message.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Throws an assertion exception if streaming tags are present or incorrectly valued.

### StartProtocolTranslationActivity_ReturnsNonNullActivity
**Purpose:** Confirms that `StartProtocolTranslationActivity` returns a valid `Activity` instance when invoked.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Throws an assertion exception if the returned activity is `null`.

### StartProtocolTranslationActivity_SetsSourceProtocolTag
**Purpose:** Asserts that the activity created by `StartProtocolTranslationActivity` contains a tag indicating the source protocol (e.g., `"http"`).  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Throws an assertion exception if the source protocol tag is missing or incorrect.

### StartProtocolTranslationActivity_SetsTargetProtocolTag
**Purpose:** Verifies that the activity includes a tag specifying the target protocol (e.g., `"grpc"`).  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Throws an assertion exception if the target protocol tag is absent or wrong.

### StartAuthenticationActivity_ReturnsNonNullActivity
**Purpose:** Checks that `StartAuthenticationActivity` yields a non‑null `Activity`.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Throws an assertion exception if the activity is `null`.

### StartAuthenticationActivity_SetsSchemeTag
**Purpose:** Ensures that the authentication activity contains a tag representing the authentication scheme (e.g., `"Bearer"`).  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Throws an assertion exception if the scheme tag is missing or does not match the expected value.

### SetGrpcStatus_WithOkStatus_SetsOkStatusCode
**Purpose:** Verifies that calling `SetGrpcStatus` with a successful status code results in the activity’s status being set to `Ok`.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Throws an assertion exception if the activity status is not `Ok`.

## Usage

```csharp
// Example 1: Verifying constructor validation
using Xunit;
using GrpcWebBridge.Tracing; // namespace containing TracingService

public class CustomTests
{
    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var tracerTests = new TracingServiceTests();
        // The test method itself performs the assertion
        Assert is called test
        // The test method encapsulates the expectation
        // No additional code needed here
    }
}
```

```csharp
// Example 2: Testing activity creation for a unary gRPC call
using Xunit;
using GrpcWebBridge.Tracing;
using System.Diagnostics;

public class ActivityTests
{
    [Fact]
    public void StartGrpcCallActivity_UnaryCall_HasClientKind()
    {
        var tester = new TracingServiceTests();
        tester.StartGrpcCallActivity_UnaryCall_HasClientKind();
        // The method asserts internally; a passing test means no exception was thrown
    }
}
```

## Notes

- The test methods are designed to be run in isolation; each method creates its own dependencies and does not rely on shared mutable state.
- Passing `null` for the logger argument to the `TracingService` constructor is the only scenario that triggers an `ArgumentNullException`; all other constructor usages are expected to succeed.
- Activity tags asserted by the tests are case‑sensitive and must match the exact strings defined in the instrumentation code.
- The `TracingService` itself is thread‑safe; however, the test class does not guarantee thread safety when its instances are reused across concurrent test executions. It is recommended to instantiate a new `TracingServiceTests` object per test method or test class.
- The `Dispose` method should be called after each test if the test allocates unmanaged resources; in the current implementation it primarily serves to satisfy the `IDisposable` pattern and has no observable side effects.
