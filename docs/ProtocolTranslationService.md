# ProtocolTranslationService

The `ProtocolTranslationService` is a core utility within the `grpc-web-bridge` project responsible for mediating communication between HTTP/JSON-based clients and gRPC/Protobuf-based backend services. It handles the bidirectional translation of request and response payloads, manages metadata mapping between HTTP headers and gRPC metadata contexts, and orchestrates the invocation of gRPC methods while ensuring proper error handling and response formatting.

## API

### Constructor
**`public ProtocolTranslationService()`**
Initializes a new instance of the `ProtocolTranslationService`. This constructor sets up the necessary internal state for performing protocol translations. It does not take any parameters and does not throw exceptions under normal initialization conditions.

### TranslateHttpToGrpc
**`public GrpcRequest TranslateHttpToGrpc(...)`**
Converts an incoming HTTP request representation into a standardized `GrpcRequest` object suitable for gRPC invocation.
*   **Purpose**: Parses HTTP headers, query parameters, and body content to construct a valid gRPC request context.
*   **Parameters**: Accepts the necessary HTTP context data (implementation dependent on specific overload or internal state).
*   **Return Value**: Returns a `GrpcRequest` object containing the mapped method name, metadata, and serialized payload.
*   **Throws**: Throws an exception if the HTTP request lacks required gRPC method identifiers or contains malformed headers that prevent valid mapping.

### TranslateGrpcToHttp
**`public byte[] TranslateGrpcToHttp(...)`**
Serializes a gRPC response into a byte array formatted for HTTP transmission.
*   **Purpose**: Encodes the gRPC status, trailers, and message payload into a format compatible with HTTP clients (often gRPC-Web encoding).
*   **Parameters**: Accepts the gRPC response data to be encoded.
*   **Return Value**: Returns a `byte[]` representing the encoded HTTP response body.
*   **Throws**: May throw if the response object is null or if the serialization process encounters an invalid state.

### ConvertProtobufToJson
**`public byte[] ConvertProtobufToJson(...)`**
Transforms a binary Protobuf message into its JSON representation.
*   **Purpose**: Enables interoperability with clients that expect JSON payloads instead of binary Protobuf.
*   **Parameters**: Accepts a byte array or object representing the Protobuf message.
*   **Return Value**: Returns a `byte[]` containing the UTF-8 encoded JSON string.
*   **Throws**: Throws a serialization exception if the input byte array is not a valid Protobuf message for the expected schema.

### ConvertJsonToProtobuf
**`public byte[] ConvertJsonToProtobuf(...)`**
Parses a JSON payload and converts it into a binary Protobuf message.
*   **Purpose**: Allows JSON-based requests to be processed by gRPC services requiring binary Protobuf input.
*   **Parameters**: Accepts a `byte[]` containing the JSON data.
*   **Return Value**: Returns a `byte[]` representing the serialized Protobuf message.
*   **Throws**: Throws if the JSON structure does not match the expected Protobuf schema or if the JSON is malformed.

### ValidateRequest
**`public void ValidateRequest(...)`**
Performs validation checks on an incoming request before translation or invocation.
*   **Purpose**: Ensures the request contains all mandatory fields, valid content types, and acceptable metadata.
*   **Parameters**: Accepts the request object or context to validate.
*   **Return Value**: Returns `void`.
*   **Throws**: Throws an `ArgumentException` or a custom validation exception if the request fails integrity checks.

### TranslateMetadata
**`public Dictionary<string, string> TranslateMetadata(...)`**
Maps metadata between HTTP headers and gRPC metadata formats.
*   **Purpose**: Converts header keys and values, handling specific prefixes (e.g., `grpc-`) and case sensitivity requirements.
*   **Parameters**: Accepts the source metadata collection (headers or gRPC metadata).
*   **Return Value**: Returns a `Dictionary<string, string>` containing the translated key-value pairs.
*   **Throws**: Generally does not throw unless the input collection is null.

### TranslateAndInvokeAsync
**`public async Task<GrpcResponse> TranslateAndInvokeAsync(...)`**
Orchestrates the full lifecycle of a request: validation, translation, gRPC invocation, and response translation.
*   **Purpose**: The primary entry point for processing a bridged request asynchronously.
*   **Parameters**: Accepts the incoming HTTP request context.
*   **Return Value**: Returns a `Task<GrpcResponse>` which resolves to the final processed response.
*   **Throws**: Propagates exceptions from the underlying gRPC call or translation steps if the operation fails.

### CreateErrorResponse
**`public GrpcResponse CreateErrorResponse(...)`**
Generates a standardized error response object.
*   **Purpose**: Constructs a `GrpcResponse` indicating failure, populated with appropriate status codes and error messages.
*   **Parameters**: Accepts error details such as status code, message, and optional exception data.
*   **Return Value**: Returns a `GrpcResponse` object configured as an error.
*   **Throws**: Does not throw; intended to safely generate error states.

### AsBytes
**`public static byte[] AsBytes(...)`**
A utility helper to convert various data types into a byte array.
*   **Purpose**: Provides a consistent mechanism for serializing strings or other primitive data into bytes for transport.
*   **Parameters**: Accepts the data to be converted (e.g., string, stream).
*   **Return Value**: Returns a `byte[]`.
*   **Throws**: May throw if the input type is unsupported or conversion fails.

## Usage

### Example 1: Manual Payload Conversion
This example demonstrates converting a JSON payload received from a web client into a Protobuf byte array, validating it, and then preparing a binary response.

```csharp
using System;
using System.Text;
using GrpcWebBridge;

public class PayloadHandler
{
    private readonly ProtocolTranslationService _translator;

    public PayloadHandler()
    {
        _translator = new ProtocolTranslationService();
    }

    public byte[] ProcessJsonRequest(string jsonInput)
    {
        // Convert incoming JSON string to bytes
        var jsonBytes = Encoding.UTF8.GetBytes(jsonInput);
        
        // Translate JSON to Protobuf binary format
        var protobufBytes = _translator.ConvertJsonToProtobuf(jsonBytes);
        
        // Validate the resulting request structure (hypothetical context)
        // _translator.ValidateRequest(protobufBytes); 

        // Simulate processing and convert response back to JSON for the client
        // var responseProtobuf = InvokeGrpcMethod(protobufBytes); 
        // return _translator.ConvertProtobufToJson(responseProtobuf);
        
        return protobufBytes;
    }
}
```

### Example 2: Full Request Orchestration
This example utilizes the high-level asynchronous method to handle the entire translation and invocation flow, including error handling.

```csharp
using System;
using System.Threading.Tasks;
using GrpcWebBridge;

public class BridgeController
{
    private readonly ProtocolTranslationService _service;

    public BridgeController()
    {
        _service = new ProtocolTranslationService();
    }

    public async Task<byte[]> HandleIncomingRequestAsync(object httpContext)
    {
        try
        {
            // Execute the full translation and gRPC invocation pipeline
            var response = await _service.TranslateAndInvokeAsync(httpContext);
            
            // Translate the gRPC response back to HTTP-compatible bytes
            return _service.TranslateGrpcToHttp(response);
        }
        catch (Exception ex)
        {
            // Create a standardized error response
            var errorResponse = _service.CreateErrorResponse(
                statusCode: "INTERNAL", 
                message: ex.Message
            );
            
            return _service.TranslateGrpcToHttp(errorResponse);
        }
    }
}
```

## Notes

*   **Thread Safety**: The `ProtocolTranslationService` instance methods (e.g., `ConvertJsonToProtobuf`, `TranslateMetadata`) rely on internal state or serializers that may not be thread-safe. It is recommended to instantiate a new service per request or ensure external synchronization if sharing an instance across concurrent threads. The static method `AsBytes` is generally safe for concurrent use provided the inputs are immutable.
*   **Serialization Edge Cases**: When using `ConvertJsonToProtobuf` or `ConvertProtobufToJson`, ensure the JSON structure strictly adheres to the Protobuf schema definition. Mismatched field types or missing required fields will result in runtime serialization exceptions.
*   **Metadata Mapping**: The `TranslateMetadata` method handles key normalization. Be aware that HTTP/2 headers are case-insensitive, whereas gRPC metadata keys are typically lowercased. The dictionary returned should be treated as the authoritative source for the gRPC call context.
*   **Error Handling**: The `CreateErrorResponse` method is designed to never throw. It should be used in catch blocks to guarantee that a valid `GrpcResponse` is always returned to the client, even when critical failures occur during translation or invocation.
*   **Byte Array Management**: Methods returning `byte[]` allocate new memory on the heap. In high-throughput scenarios, consider the impact of frequent allocations and whether pooling strategies are applicable in the calling code.
