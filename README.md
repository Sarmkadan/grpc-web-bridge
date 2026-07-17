// existing content ...

## ProtocolTranslationServiceTests
The `ProtocolTranslationServiceTests` class provides a set of unit tests for the `ProtocolTranslationService` class, ensuring correct behavior in translating HTTP requests to gRPC requests, handling metadata, and creating error responses. Here's an example of how to use some of its public members:
```csharp
var tests = new ProtocolTranslationServiceTests();
var payload = "{}".AsBytes();
var grpcRequest = tests._service.TranslateHttpToGrpc("TestService", "TestMethod", payload, SerializationFormat.Json);
grpcRequest.ServiceName.Should().Be("TestService");
grpcRequest.MethodName.Should().Be("TestMethod");

var protobuf = tests._service.ConvertJsonToProtobuf(payload);
protobuf.Should().BeEmpty();

var response = tests._service.CreateErrorResponse(Guid.NewGuid().ToString(), GrpcStatusCode.NotFound, "Service not found");
response.Status.Should().Be(GrpcStatusCode.NotFound);
response.StatusMessage.Should().Be("Service not found");
```
