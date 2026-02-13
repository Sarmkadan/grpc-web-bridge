# GrpcMethodExtensions

The `GrpcMethodExtensions` class provides a set of static utility methods designed to analyze and transform gRPC method descriptors into C#-specific representations. It facilitates code generation and reflection scenarios by exposing metadata such as streaming capabilities, parameter counts, and formatted signatures suitable for C# syntax and XML documentation standards.

## API

### ToCSharpSignature
Generates a string representation of the gRPC method formatted as a valid C# method signature.
*   **Purpose**: Converts the method descriptor into a syntax-compliant C# declaration string.
*   **Parameters**: Accepts the target gRPC method descriptor instance (implicit via extension method syntax).
*   **Return Value**: A `string` containing the formatted signature.
*   **Exceptions**: Throws an exception if the method descriptor is null or contains invalid types that cannot be mapped to C# primitives or generated message classes.

### IsStreaming
Determines whether the gRPC method involves any streaming communication pattern.
*   **Purpose**: Checks if the method is server-streaming, client-streaming, or duplex streaming.
*   **Parameters**: Accepts the target gRPC method descriptor instance.
*   **Return Value**: A `bool` value; `true` if any streaming flag is set on the method, otherwise `false`.
*   **Exceptions**: Throws an exception if the method descriptor is null.

### GetTotalParameterCount
Calculates the total number of parameters required to invoke the method in a generated C# client or server context.
*   **Purpose**: Returns the count of arguments, typically including the request object and call options/context depending on the generation strategy.
*   **Parameters**: Accepts the target gRPC method descriptor instance.
*   **Return Value**: An `int` representing the total parameter count.
*   **Exceptions**: Throws an exception if the method descriptor is null.

### ToXmlDocumentation
Generates an XML documentation comment block for the method, suitable for insertion into generated source files.
*   **Purpose**: Creates a formatted string containing `<summary>`, `<param>`, and `<returns>` tags based on the method's metadata.
*   **Parameters**: Accepts the target gRPC method descriptor instance.
*   **Return Value**: A `string` containing the full XML documentation block.
*   **Exceptions**: Throws an exception if the method descriptor is null or if internal formatting rules fail due to missing metadata.

## Usage

The following examples demonstrate how to use `GrpcMethodExtensions` to inspect a method descriptor and generate corresponding C# code artifacts.

**Example 1: Inspecting Streaming Capabilities and Parameter Count**
This example checks if a method is streaming and retrieves the parameter count to determine the appropriate invocation strategy.

```csharp
using Grpc.Web.Bridge; // Hypothetical namespace for the project
using Grpc.Core;

public void AnalyzeMethod(MethodDescriptor request, response)
{
    // Check if the method involves any streaming
    if (request.IsStreaming())
    {
        Console.WriteLine("Streaming detected. Use async enumerator or client stream.");
    }
    else
    {
        Console.WriteLine("Unary call detected.");
    }

    // Get the total number of parameters for the generated wrapper
    int paramCount = request.GetTotalParameterCount();
    Console.WriteLine($"Generated method will require {paramCount} parameters.");
}
```

**Example 2: Generating Signature and Documentation**
This example generates the C# signature and XML documentation for a method to be included in a dynamically generated class.

```csharp
using Grpc.Web.Bridge;
using Grpc.Core;
using System.Text;

public string GenerateMethodStub(MethodDescriptor method)
{
    var sb = new StringBuilder();

    // Append XML documentation
    sb.AppendLine(method.ToXmlDocumentation());
    
    // Append the C# signature
    string signature = method.ToCSharpSignature();
    sb.AppendLine($"{signature}");
    sb.AppendLine("{");
    sb.AppendLine("    // Implementation placeholder");
    sb.AppendLine("    throw new NotImplementedException();");
    sb.AppendLine("}");

    return sb.ToString();
}
```

## Notes

*   **Null Safety**: All extension methods assume the input `MethodDescriptor` instance is not null. Passing a null reference will result in a `NullReferenceException` or a specific argument validation exception depending on the runtime configuration.
*   **Thread Safety**: As this class contains only stateless static methods that operate solely on their input parameters, it is fully thread-safe. Multiple threads may safely call these methods concurrently on the same or different descriptors.
*   **Descriptor Validity**: These methods rely on the integrity of the underlying `MethodDescriptor`. If the descriptor was constructed manually with inconsistent state (e.g., mismatched request/response types), the generated signatures or parameter counts may not reflect compilable C# code.
*   **Formatting Consistency**: The `ToCSharpSignature` and `ToXmlDocumentation` methods adhere to standard C# coding conventions. Changes to the underlying gRPC type names will directly reflect in the output strings; no sanitization of invalid C# identifiers is performed beyond standard mapping rules.
