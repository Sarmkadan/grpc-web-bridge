# JSON extensions catalog

This catalog covers every `*JsonExtensions.cs` class under `src/GrpcWebBridge`. Signatures are reproduced as declared in the source. The target column distinguishes serialization and deserialization targets where they differ.

| Extension class | Target type | Declared signatures |
| --- | --- | --- |
| `StreamCleanupWorkerJsonExtensions` | `StreamCleanupWorker` | `public static string ToJson(this StreamCleanupWorker value, bool indented = false)`<br>`public static StreamCleanupWorker? FromJson(string json)`<br>`public static bool TryFromJson(string json, out StreamCleanupWorker? value)` |
| `CacheManagerJsonExtensions` | `CacheManager` | `public static string ToJson(this CacheManager value, bool indented = false)`<br>`public static CacheManager? FromJson(string json)`<br>`public static bool TryFromJson(string json, [NotNullWhen(true)] out CacheManager? value)` |
| `DependencyInjectionJsonExtensions` | `GrpcWebBridgeOptions` | `public static string ToJson(this GrpcWebBridgeOptions value, bool indented = false)`<br>`public static GrpcWebBridgeOptions? FromJson(string? json)`<br>`public static bool TryFromJson(string? json, out GrpcWebBridgeOptions? value)` |
| `StreamingExceptionJsonExtensions` | `StreamingException` | `public static string ToJson(this StreamingException value, bool indented = false)`<br>`public static StreamingException? FromJson(string json)`<br>`public static bool TryFromJson(string json, out StreamingException? value)` |
| `BridgeConfigurationJsonExtensions` | `BridgeConfiguration` | `public static string ToJson(this BridgeConfiguration value, bool indented = false)`<br>`public static BridgeConfiguration? FromJson(string json)`<br>`public static bool TryFromJson(string json, out BridgeConfiguration? value)` |
| `GrpcRequestJsonExtensions` | `GrpcRequest` | `public static string ToJson(this GrpcRequest value, bool indented = false)`<br>`public static GrpcRequest? FromJson(string json)`<br>`public static bool TryFromJson(string json, out GrpcRequest? value)` |
| `EventBusJsonExtensions` | `EventBus` | `public static string ToJson(this EventBus value, bool indented = false)`<br>`public static EventBus? FromJson(string json)`<br>`public static bool TryFromJson(string json, out EventBus? value)` |
| `CsvFormatterJsonExtensions` | `CsvFormatter` | `public static string ToJson(this CsvFormatter value, bool indented = false)`<br>`public static CsvFormatter? FromJson(string json)`<br>`public static bool TryFromJson(string json, out CsvFormatter? value)` |
| `JsonFormatterJsonExtensions` | `JsonFormatter` | `public static string ToJson(this JsonFormatter value, bool indented = false)`<br>`public static JsonFormatter? FromJson(string json)`<br>`public static bool TryFromJson(string json, out JsonFormatter? value)` |
| `RequestContextManagerJsonExtensions` | `RequestContextManager` for `ToJson`; `RequestContext` for `FromJson` and `TryFromJson` | `public static string ToJson(this RequestContextManager value, bool indented = false)`<br>`public static RequestContext? FromJson(string json)`<br>`public static bool TryFromJson(string json, out RequestContext? value)` |
| `ContentTypeValidationMiddlewareJsonExtensions` | `ContentTypeValidationMiddleware` | `public static string ToJson(this ContentTypeValidationMiddleware value, bool indented = false)`<br>`public static ContentTypeValidationMiddleware? FromJson(string json)`<br>`public static bool TryFromJson(string json, out ContentTypeValidationMiddleware? value)` |
| `AuthenticationServiceJsonExtensions` | `AuthenticationService` | `public static string ToJson(this AuthenticationService value, bool indented = false)`<br>`public static AuthenticationService? FromJson(string? json)`<br>`public static bool TryFromJson(string? json, out AuthenticationService? value)` |
| `StreamDiagnosticsOptionsJsonExtensions` | `StreamDiagnosticsOptions` | `public static string ToJson(this StreamDiagnosticsOptions value, bool indented = false)`<br>`public static StreamDiagnosticsOptions? FromJson(string json)`<br>`public static bool TryFromJson(string json, out StreamDiagnosticsOptions? value)` |
| `CacheUtilityJsonExtensions` | `CacheStatistics` | `public static string ToJson(this CacheStatistics value, bool indented = false)`<br>`public static CacheStatistics? FromJson(string json)`<br>`public static bool TryFromJson(string json, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out CacheStatistics? value)` |
| `DateTimeUtilityJsonExtensions` | `DateTime`, `DateTime?`, and `DateTimePeriod` for `ToJson`; `DateTime` for `FromJson` and `TryFromJson` | `public static string ToJson(this DateTime value, bool indented = false)`<br>`public static DateTime? FromJson(this string json)`<br>`public static bool TryFromJson(this string json, out DateTime? value)`<br>`public static string ToJson(this DateTime? value, bool indented = false)`<br>`public static string ToJson(this DateTimePeriod value, bool indented = false)` |

## Shared usage example

The same pattern applies across the catalog: `ToJson` is called as an extension on the target value, while `FromJson` and `TryFromJson` are called on the declaring class unless their signatures include `this string json`.

```csharp
using GrpcWebBridge.Domain.Models;

BridgeConfiguration configuration = GetConfiguration();

string json = configuration.ToJson(indented: true);
BridgeConfiguration? restored = BridgeConfigurationJsonExtensions.FromJson(json);

if (BridgeConfigurationJsonExtensions.TryFromJson(json, out BridgeConfiguration? parsed))
{
    // Use parsed.
}
```
