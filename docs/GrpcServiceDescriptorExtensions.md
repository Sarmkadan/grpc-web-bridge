# GrpcServiceDescriptorExtensions

Static extension-method class in `GrpcWebBridge.Domain.Models` (`src/GrpcWebBridge/Domain/Models/GrpcServiceDescriptorExtensions.cs`). It adds read-only helpers to `GrpcServiceDescriptor`:

- `GetDisplayName()` builds a `Package.Name` label;
- `IsSecureEndpoint()` checks whether the service runs over TLS on a standard HTTPS port;
- `GetStreamingMethods()` lists methods that stream in either direction;
- `GetMethodByName()` finds a method with a case-insensitive name match.

None of the methods change the descriptor. They use only the BCL (`System`, `System.Collections.Generic`, `System.Linq`), so no NuGet package is needed.

## Overview

A `GrpcServiceDescriptor` is metadata about a service, built from reflection. Its `Methods` collection holds one `MethodDescriptor` per RPC. The helpers here answer the questions a caller usually asks about that metadata: what to call the service, whether the connection is secure, which methods stream, and whether a given method exists.

The examples below use this descriptor, which is also used in the examples further down:

```csharp
using System;
using System.Collections.Generic;
using GrpcWebBridge.Domain.Models;

var orders = new GrpcServiceDescriptor
{
    Name = "OrderService",
    PackageName = "shop.v1",
    FullName = "shop.v1.OrderService",
    Endpoint = "orders.internal",
    Port = 443,
    UseTls = true,
    Methods =
    [
        new MethodDescriptor
        {
            Name = "GetOrder",
            FullName = "shop.v1.OrderService/GetOrder",
            ServiceFullName = "shop.v1.OrderService",
            MethodType = "Unary",
            InputMessageType = "shop.v1.GetOrderRequest",
            OutputMessageType = "shop.v1.Order",
            TimeoutMilliseconds = 5000
        },
        new MethodDescriptor
        {
            Name = "WatchOrders",
            FullName = "shop.v1.OrderService/WatchOrders",
            ServiceFullName = "shop.v1.OrderService",
            MethodType = "ServerStreaming",
            IsServerStreaming = true,
            InputMessageType = "shop.v1.WatchOrdersRequest",
            OutputMessageType = "shop.v1.OrderEvent",
            TimeoutMilliseconds = 0
        },
        new MethodDescriptor
        {
            Name = "SyncOrders",
            FullName = "shop.v1.OrderService/SyncOrders",
            ServiceFullName = "shop.v1.OrderService",
            MethodType = "BidirectionalStreaming",
            IsClientStreaming = true,
            IsServerStreaming = true,
            InputMessageType = "shop.v1.OrderUpdate",
            OutputMessageType = "shop.v1.OrderAck",
            TimeoutMilliseconds = 0
        }
    ]
};
```

---

## API

### `public static string GetDisplayName(this GrpcServiceDescriptor descriptor)`

Returns the service name prefixed with its package, in the form `{PackageName}.{Name}`. The result is built by plain concatenation, so it is not checked against `FullName`.

- **Parameters**
  - `descriptor`: The service descriptor.
- **Returns**
  - The string `PackageName + "." + Name`.
- **Throws**
  - `ArgumentNullException`: If `descriptor` is `null` (parameter name `descriptor`).

**Notes**

- If `PackageName` is empty (the default), the result starts with a dot, for example `.OrderService`. Check `PackageName` first if a service may have no package.
- `GetDisplayName` does not use `FullName`. For a service declared without a package, `FullName` and `GetDisplayName()` can differ.

**Examples**

```csharp
orders.GetDisplayName();   // "shop.v1.OrderService"

var noPackage = new GrpcServiceDescriptor { Name = "Ping" };
noPackage.GetDisplayName();  // ".Ping"
```

---

### `public static bool IsSecureEndpoint(this GrpcServiceDescriptor descriptor)`

Reports whether the service endpoint should be treated as secure. It returns `true` only when both conditions hold:

1. `UseTls` is `true`; and
2. `Port` is `443` or `8443`.

A TLS service on any other port returns `false`, and so does a plain-text service on port 443. The method does not open a connection and does not check certificates.

- **Parameters**
  - `descriptor`: The service descriptor.
- **Returns**
  - `true` if TLS is enabled and the port is 443 or 8443; otherwise `false`.
- **Throws**
  - `ArgumentNullException`: If `descriptor` is `null` (parameter name `descriptor`).

**Examples**

```csharp
orders.IsSecureEndpoint();                       // true  (UseTls = true, Port = 443)

var onCustomPort = new GrpcServiceDescriptor { UseTls = true, Port = 50051 };
onCustomPort.IsSecureEndpoint();                 // false (TLS, but not a standard HTTPS port)

var plainText = new GrpcServiceDescriptor { UseTls = false, Port = 443 };
plainText.IsSecureEndpoint();                    // false (port matches, TLS is off)
```

---

### `public static IEnumerable<MethodDescriptor> GetStreamingMethods(this GrpcServiceDescriptor descriptor)`

Returns the methods where `IsClientStreaming` or `IsServerStreaming` is `true`. This covers client-streaming, server-streaming, and bidirectional methods. Unary methods are excluded.

The result is a lazy LINQ `Where` query over `descriptor.Methods`. Nothing is evaluated until the sequence is enumerated, and each enumeration re-reads the current `Methods` collection. Call `.ToList()` if you need a snapshot. The XML summary calls the result a read-only collection, but it is an `IEnumerable<MethodDescriptor>`. The returned sequence is not a materialized list.

- **Parameters**
  - `descriptor`: The service descriptor.
- **Returns**
  - A lazily evaluated sequence of the streaming methods, in the order they appear in `Methods`. The sequence is empty when there are none.
- **Throws**
  - `ArgumentNullException`: If `descriptor` is `null` (parameter name `descriptor`). The check runs immediately, when the method is called, not when the sequence is enumerated.

**Examples**

```csharp
foreach (var method in orders.GetStreamingMethods())
{
    Console.WriteLine($"{method.Name} ({method.MethodType})");
}
// WatchOrders (ServerStreaming)
// SyncOrders (BidirectionalStreaming)

var streaming = orders.GetStreamingMethods().ToList();   // materialize once
int count = streaming.Count;                             // 2
```

---

### `public static MethodDescriptor? GetMethodByName(this GrpcServiceDescriptor descriptor, string methodName)`

Finds the first method whose `Name` equals `methodName`, using `StringComparison.OrdinalIgnoreCase`. Leading and trailing whitespace in `methodName` is removed before the comparison. If several methods share a name, the first one in `Methods` is returned.

- **Parameters**
  - `descriptor`: The service descriptor.
  - `methodName`: The method name to find. Matched against `MethodDescriptor.Name`, not `FullName`. Surrounding whitespace is ignored.
- **Returns**
  - The matching `MethodDescriptor`, or `null` if no method matches.
- **Throws**
  - `ArgumentNullException`: If `descriptor` is `null` (parameter name `descriptor`).
  - `ArgumentException`: If `methodName` is `null` or the empty string `""`. Whitespace-only input such as `"   "` does not throw. It is trimmed to `""` and returns `null`.

**Notes**

- The XML doc on this method says whitespace-only input throws `ArgumentException`. The code only checks for `null` or empty (`ArgumentException.ThrowIfNullOrEmpty`), so whitespace-only input returns `null`. Treat the code as the source of truth for now.
- `GetMethodByName` takes a short name, such as `"getorder"`. To look up a full RPC name such as `shop.v1.OrderService/GetOrder`, compare against `FullName` yourself.

**Examples**

```csharp
var get = orders.GetMethodByName("GetOrder");
// get.FullName == "shop.v1.OrderService/GetOrder"

orders.GetMethodByName("  getorder  ")?.Name;   // "GetOrder"  (case and whitespace ignored)
orders.GetMethodByName("DeleteOrder");          // null        (no such method)
orders.GetMethodByName("   ");                  // null        (trimmed to "", no throw)
orders.GetMethodByName("");                     // throws ArgumentException
```

---

## Usage in the bridge

Typical combinations:

```csharp
// Decide whether to use a secure channel for a service.
var channelUri = orders.IsSecureEndpoint()
    ? $"https://{orders.Endpoint}:{orders.Port}"
    : $"http://{orders.Endpoint}:{orders.Port}";

// Reject a streaming call on a method that is not a stream.
var method = orders.GetMethodByName(requestedMethod);
if (method is null)
{
    throw new InvalidOperationException($"Unknown method: {requestedMethod}");
}
if (!(method.IsClientStreaming || method.IsServerStreaming))
{
    // Handle as a unary call.
}

// Build a log label for a service.
logger.LogInformation("Routing to {Service}", orders.GetDisplayName());
```

## Tests

Tests for these methods are in `tests/grpc-web-bridge.Tests/GrpcServiceDescriptorExtensionsTests.cs`.
