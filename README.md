// existing content ...

## TracingServiceTests
The `TracingServiceTests` class provides a comprehensive set of unit tests for the `TracingService` class, ensuring correct behavior in tracing gRPC calls, protocol translation, authentication, and error handling within the gRPC web bridge. These tests validate the creation and configuration of activities, tags, and status codes. Here's an example of how to use some of its public members:
```csharp
var tests = new TracingServiceTests();
tests.Dispose(); // Ensure proper cleanup

var sut = new TracingServiceTests();
using var activity = sut._sut.StartGrpcCallActivity("UserService", "GetUser");
sut._exported.Should().BeEmpty(); // Verify no activities are exported yet

sut._tracerProvider.ForceFlush();
``` 
