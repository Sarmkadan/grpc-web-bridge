# BridgeController
The `BridgeController` type is a central component in the `grpc-web-bridge` project, responsible for managing the invocation of gRPC methods and handling the associated data exchange. It provides a set of methods and properties that allow for flexible and controlled interaction with gRPC services, including support for streaming, batch operations, and customizable headers and timeouts.

## API
The `BridgeController` type exposes the following public members:
* `public BridgeController`: The constructor for creating a new instance of `BridgeController`.
* `public async Task<IActionResult> InvokeMethod`: Invokes a gRPC method asynchronously, returning an `IActionResult` that represents the outcome of the invocation. Parameters include `ServiceId`, `MethodName`, `Payload`, `Headers`, and `TimeoutMs`. Throws if the invocation fails.
* `public async Task StreamMessages`: Streams messages from a gRPC method invocation. Parameters include `ServiceId`, `MethodName`, and `InitialMessage`.
* `public async Task<IActionResult> BatchInvoke`: Invokes a batch of gRPC methods asynchronously, returning an `IActionResult` that represents the outcome of the batch invocation. Parameters include `Operations`, which is a list of `BatchOperation` objects containing `Id`, `ServiceId`, `MethodName`, `Payload`, `Headers`, and `OperationId`.
* `public string ServiceId`: Gets or sets the identifier of the gRPC service.
* `public string MethodName`: Gets or sets the name of the gRPC method.
* `public object? Payload`: Gets or sets the payload data for the gRPC method invocation.
* `public Dictionary<string, string>? Headers`: Gets or sets the headers for the gRPC method invocation.
* `public int? TimeoutMs`: Gets or sets the timeout in milliseconds for the gRPC method invocation.
* `public string Id`: Gets or sets the identifier for the batch operation.
* `public string OperationId`: Gets or sets the identifier for the specific operation within a batch.
* `public bool Success`: Indicates whether the invocation or batch operation was successful.

## Usage
Here are two examples of using the `BridgeController` type:
```csharp
// Example 1: Invoking a gRPC method
var controller = new BridgeController();
controller.ServiceId = "my-service";
controller.MethodName = "my-method";
controller.Payload = new { foo = "bar" };
var result = await controller.InvokeMethod();
Console.WriteLine(result);

// Example 2: Invoking a batch of gRPC methods
var controller = new BridgeController();
var operations = new List<BatchOperation>
{
    new BatchOperation { Id = "op1", ServiceId = "my-service", MethodName = "my-method1", Payload = new { foo = "bar1" } },
    new BatchOperation { Id = "op2", ServiceId = "my-service", MethodName = "my-method2", Payload = new { foo = "bar2" } }
};
var result = await controller.BatchInvoke(operations);
Console.WriteLine(result);
```

## Notes
When using the `BridgeController` type, consider the following:
* The `InvokeMethod` and `BatchInvoke` methods are asynchronous and may throw exceptions if the underlying gRPC invocation fails.
* The `StreamMessages` method is designed for streaming scenarios and may not be suitable for all use cases.
* The `TimeoutMs` property can be used to customize the timeout for gRPC method invocations, but be cautious not to set it too low, as this may lead to premature timeouts.
* The `BridgeController` type is not thread-safe by default, so ensure proper synchronization when accessing its members from multiple threads.
* When working with batch operations, be aware that the order of operations within the batch may affect the overall outcome, and consider using the `OperationId` property to track specific operations within the batch.
