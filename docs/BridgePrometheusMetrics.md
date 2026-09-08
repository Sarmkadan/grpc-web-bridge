# BridgePrometheusMetrics
The `BridgePrometheusMetrics` class defines the opt-in Prometheus metrics used to monitor gRPC-Web bridge calls and streams. Its static collectors are registered with the default Prometheus registry when the class is initialized.

## API
* `Counter grpcweb_bridge_requests_total`: Counts completed gRPC-Web bridge requests. Labels: `service`, `method`, `grpc_status`.
* `Histogram grpcweb_bridge_request_duration_seconds`: Observes gRPC-Web bridge request durations in seconds. Labels: `service`, `method`. Buckets: `0.005`, `0.01`, `0.025`, `0.05`, `0.1`, `0.25`, `0.5`, `1`, `2.5`, `5`, `10` seconds.
* `Gauge grpcweb_bridge_active_streams`: Tracks the number of currently active server-streaming or bidirectional gRPC-Web streams. Labels: none.
* `Counter grpcweb_bridge_stream_errors_total`: Counts gRPC-Web bridge stream errors. Labels: `service`, `method`.
* `public static void RecordCall(string service, string method, string grpcStatus, double durationSeconds)`: Increments `grpcweb_bridge_requests_total` using the supplied service, method, and gRPC status labels, then observes the duration in `grpcweb_bridge_request_duration_seconds` using the service and method labels.
* `public static void RecordStreamError(string service, string method)`: Increments `grpcweb_bridge_stream_errors_total` using the supplied service and method labels.

## Usage
Register the metric definitions during service configuration and expose the Prometheus scrape endpoint in the application pipeline:
```csharp
services.AddGrpcWebBridgePrometheus();

// After building the application:
app.MapMetrics();
```

Record completed calls and stream errors with the public methods, and update the active-stream gauge around the lifetime of a stream:
```csharp
BridgePrometheusMetrics.RecordCall(
    service: "Greeter",
    method: "SayHello",
    grpcStatus: "OK",
    durationSeconds: 0.042);

BridgePrometheusMetrics.ActiveStreams.Inc();
try
{
    // Process the stream.
}
catch
{
    BridgePrometheusMetrics.RecordStreamError("Greeter", "StreamHellos");
    throw;
}
finally
{
    BridgePrometheusMetrics.ActiveStreams.Dec();
}
```

## Notes
`DependencyInjection.AddGrpcWebBridgePrometheus(IServiceCollection services)` throws `ArgumentNullException` when `services` is `null`. It then accesses `RequestsTotal`, `RequestDuration`, `ActiveStreams`, and `StreamErrorsTotal` to trigger static initialization and register all four collectors with the default Prometheus registry before the first request. It does not add a separate service registration and returns the same `IServiceCollection` for chaining. Call `app.MapMetrics()` separately to expose the `/metrics` endpoint.

`RecordCall` rejects a null or empty `service`, `method`, or `grpcStatus`. `RecordStreamError` rejects a null or empty `service` or `method`. Neither method updates `grpcweb_bridge_active_streams`; callers increment and decrement that gauge directly.
