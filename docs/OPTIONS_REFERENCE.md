# Options Reference

Every `*Options` type in `src/GrpcWebBridge`: each property, its type, and its default value as set in the source.

Defaults that come from a named constant show the resolved value. Where a constant is the source of truth, its name is in parentheses.

| Class | Property | Type | Default |
|-------|----------|------|---------|
| `GrpcWebBridgeOptions` (`Configuration/GrpcWebBridgeOptions.cs`) | `Configuration` | `BridgeConfiguration` | `new()` |
| `GrpcWebBridgeOptions.Configuration` (`BridgeConfiguration`) | `InstanceId` | `string` | `Guid.NewGuid().ToString("N")` |
| `GrpcWebBridgeOptions.Configuration` | `InstanceName` | `string?` | `null` |
| `GrpcWebBridgeOptions.Configuration` | `Environment` | `string` | `"Production"` |
| `GrpcWebBridgeOptions.Configuration` | `EnableLogging` | `bool` | `true` |
| `GrpcWebBridgeOptions.Configuration` | `EnableSwagger` | `bool` | `true` |
| `GrpcWebBridgeOptions.Configuration` | `EnableMetrics` | `bool` | `true` |
| `GrpcWebBridgeOptions.Configuration` | `EnableCors` | `bool` | `true` |
| `GrpcWebBridgeOptions.Configuration` | `RequireAuthentication` | `bool` | `false` |
| `GrpcWebBridgeOptions.Configuration` | `RequireAuthenticationForDetailedHealth` | `bool` | `true` |
| `GrpcWebBridgeOptions.Configuration` | `MaxStreamCount` | `int` | `10000` (`Constants.Streaming.MaxStreamCount`) |
| `GrpcWebBridgeOptions.Configuration` | `StreamIdleTimeoutSeconds` | `int` | `300` (`Constants.Streaming.StreamIdleTimeoutSeconds`) |
| `GrpcWebBridgeOptions.Configuration` | `StreamHeartbeatIntervalSeconds` | `int` | `30` (`Constants.Streaming.StreamHeartbeatIntervalSeconds`) |
| `GrpcWebBridgeOptions.Configuration` | `MaxMessageSize` | `int` | `4194304` bytes, 4 MB (`Constants.Grpc.MaxMessageSize`) |
| `GrpcWebBridgeOptions.Configuration` | `DefaultTimeoutMilliseconds` | `int` | `30000` (`Constants.Grpc.DefaultTimeout`) |
| `GrpcWebBridgeOptions.Configuration` | `CompressResponses` | `bool` | `true` |
| `GrpcWebBridgeOptions.Configuration` | `CompressionLevel` | `int` | `6` |
| `GrpcWebBridgeOptions.Configuration` | `AllowedOrigins` | `List<string>` | `["*"]` |
| `GrpcWebBridgeOptions.Configuration` | `AllowedMethods` | `List<string>` | `["GET", "POST", "PUT", "DELETE", "OPTIONS"]` |
| `GrpcWebBridgeOptions.Configuration` | `CustomHeaders` | `Dictionary<string, string>` | empty |
| `GrpcWebBridgeOptions.Configuration` | `ServiceDefaults` | `Dictionary<string, object>` | empty |
| `GrpcWebBridgeOptions.Configuration` | `CreatedAt` | `DateTime` | `DateTime.UtcNow` |
| `GrpcWebBridgeOptions.Configuration` | `UpdatedAt` | `DateTime?` | `null` |
| `FlowControlOptions` (`Streaming/FlowControlOptions.cs`) | `InitialWindowSize` | `int` | `64` |
| `FlowControlOptions` | `MaxWindowSize` | `int` | `256` |
| `FlowControlOptions` | `InboundChannelCapacity` | `int` | `128` |
| `FlowControlOptions` | `OutboundChannelCapacity` | `int` | `128` |
| `FlowControlOptions` | `BackpressureThreshold` | `double` | `0.85` |
| `FlowControlOptions` | `CreditReplenishmentBatch` | `int` | `16` |
| `FlowControlOptions` | `Mode` | `FlowControlMode` | `FlowControlMode.CreditWindow` |
| `FlowControlOptions` | `MaxProducerWaitTime` | `TimeSpan?` | `30` seconds |
| `FlowControlOptions` | `AdaptiveAdjustmentInterval` | `TimeSpan` | `5` seconds |
| `FlowControlOptions` | `EmitBackpressureEvents` | `bool` | `true` |
| `RateLimitingOptions` (`Middleware/RateLimitingMiddleware.cs`) | `RequestsPerSecond` | `int` | `100` (`DefaultRequestsPerSecond`) |
| `RateLimitingOptions` | `WindowSizeSeconds` | `int` | `1` (`DefaultWindowSizeSeconds`) |
| `RateLimitingOptions` | `RetryAfterSeconds` | `int` | `60` (`DefaultRetryAfterSeconds`) |
| `RateLimitingOptions` | `EnableGlobalLimit` | `bool` | `true` |
| `RateLimitingOptions` | `GlobalRequestsPerSecond` | `int` | `10000` (`DefaultGlobalRequestsPerSecond`) |
| `RateLimitingOptions` | `ExemptPaths` | `IReadOnlyList<string>` | `["/health", "/swagger"]` |
| `MaxBodySizeOptions` (`Middleware/MaxBodySizeMiddleware.cs`) | `MaxRequestBodySizeBytes` | `long` | `4194304` bytes, 4 MB (`DefaultMaxRequestBodySizeBytes`) |
| `HealthCheckOptions` (`BackgroundWorkers/HealthCheckWorker.cs`) | `CheckIntervalSeconds` | `int` | `30` (`DefaultCheckIntervalSeconds`) |
| `HealthCheckOptions` | `CheckTimeoutMs` | `int` | `5000` (`DefaultCheckTimeoutMs`) |
| `HealthCheckOptions` | `InitialDelaySeconds` | `int` | `10` (`DefaultInitialDelaySeconds`) |
| `MetricsCollectionOptions` (`BackgroundWorkers/MetricsCollectionWorker.cs`) | `CollectionIntervalSeconds` | `int` | `30` |
| `MetricsCollectionOptions` | `MaxSnapshotsToKeep` | `int` | `1000` (`Constants.ServiceRegistry.MaxCachedServices`) |
| `MetricsCollectionOptions` | `CpuAlertThresholdPercent` | `double` | `80` |
| `MetricsCollectionOptions` | `MemoryAlertThresholdMb` | `double` | `1024` |
| `MetricsCollectionOptions` | `ErrorRateAlertThresholdPercent` | `double` | `5` |
| `StreamCleanupOptions` (`BackgroundWorkers/StreamCleanupWorker.cs`) | `CleanupIntervalSeconds` | `int` | `60` |
| `StreamCleanupOptions` | `IdleTimeoutDuration` | `TimeSpan` | `300` seconds (`Constants.Streaming.StreamIdleTimeoutSeconds`) |
| `StreamCleanupOptions` | `StaleStreamDuration` | `TimeSpan` | `30` seconds (`Constants.Streaming.StreamHeartbeatIntervalSeconds`) |
| `StreamCleanupOptions` | `GcTriggerThreshold` | `int` | `10` |
| `StreamingWorkerSupervisorOptions` (`BackgroundWorkers/StreamingWorkerSupervisor.cs`) | `MonitoringIntervalSeconds` | `int` | `10` (`DefaultMonitoringIntervalSeconds`) |
| `StreamingWorkerSupervisorOptions` | `MaxConsecutiveFailures` | `int` | `3` (`DefaultMaxConsecutiveFailures`) |
| `StreamingWorkerSupervisorOptions` | `HeartbeatTimeoutSeconds` | `int` | `60` (`DefaultHeartbeatTimeoutSeconds`) |
| `StreamingWorkerSupervisorOptions` | `InitialDelaySeconds` | `int` | `15` (`DefaultInitialDelaySeconds`) |
| `StreamingWorkerSupervisorOptions` | `ErrorDelaySeconds` | `int` | `5` (`DefaultErrorDelaySeconds`) |
| `StreamingWorkerSupervisorOptions` | `RestartDelayMs` | `int` | `100` (`DefaultRestartDelayMs`) |
| `CacheManagerOptions` (`Caching/CacheManager.cs`) | `DefaultTtl` | `TimeSpan` | `5` minutes (`DefaultTtlMinutes`) |
| `CacheManagerOptions` | `MaxEntries` | `int` | `10000` (`DefaultMaxEntries`) |
| `CacheManagerOptions` | `EnableStatistics` | `bool` | `true` |
| `HttpClientFactoryOptions` (`Integration/HttpClientFactory.cs`) | `RequestTimeoutMs` | `int` | `30000` (`DefaultRequestTimeoutMs`) |
| `HttpClientFactoryOptions` | `MaxConnectionsPerServer` | `int` | `10` (`DefaultMaxConnectionsPerServer`) |
| `HttpClientFactoryOptions` | `UseCookies` | `bool` | `false` |
| `HttpClientFactoryOptions` | `AllowAutoRedirect` | `bool` | `true` |
| `HttpClientFactoryOptions` | `AllowInsecureHttps` | `bool` | `false` |
| `HttpClientFactoryOptions` | `PooledConnectionLifetimeMs` | `int` | `120000` (`DefaultPooledConnectionLifetimeMs`) |
| `ServiceDiscoveryOptions` (`Integration/ServiceDiscoveryClient.cs`) | `DiscoveryServiceUrl` | `string` | `"http://localhost:8500"` |
| `ServiceDiscoveryOptions` | `RegistrationTtlSeconds` | `int` | `30` |
| `ServiceDiscoveryOptions` | `HeartbeatIntervalSeconds` | `int` | `10` |
| `ServiceDiscoveryOptions` | `EnableAutoRefresh` | `bool` | `true` |
| `ServiceDiscoveryOptions` | `RefreshIntervalSeconds` | `int` | `60` |
| `WebhookPublisherOptions` (`Integration/WebhookPublisher.cs`) | `MaxRetries` | `int` | `3` |
| `WebhookPublisherOptions` | `FailureThresholdForDisable` | `int` | `10` |
| `StreamDiagnosticsOptions` (`Streaming/StreamDiagnosticsService.cs`) | `CollectionInterval` | `TimeSpan` | `60` seconds |
| `StreamDiagnosticsOptions` | `StaleStreamThreshold` | `TimeSpan` | `5` minutes |
| `StreamDiagnosticsOptions` | `BackpressureWarnThreshold` | `double` | `0.10` |
| `EventBus.DispatchOptions` (`Events/EventBus.cs`) | `MaxQueueSize` | `int` | `1024` (`EventBus.DefaultMaxQueueSize`) |
| `EventBus.DispatchOptions` | `FullQueuePolicy` | `DispatchQueueFullPolicy` | `DispatchQueueFullPolicy.DropOldest` |
| `EventBus.DispatchOptions` | `MaxWaitTime` | `TimeSpan` | `5` seconds |
| `EventBus.DispatchOptions` | `UseAsyncDispatch` | `bool` | `false` |

## Notes

- `GrpcWebBridgeOptions` has one settable property, `Configuration`. Its values are set through the fluent `With*` methods (for example `WithDevelopment()`, `WithMaxStreamCount(...)`), which write into the nested `BridgeConfiguration`. The defaults listed above are the `BridgeConfiguration` initializers. `WithDevelopment()`, `WithProduction()` and `WithTesting()` override several of them.
- `FlowControlOptions`, `StreamDiagnosticsOptions` and `EventBus.DispatchOptions` are immutable records (`init` properties). The rest are mutable classes.
- `FlowControlMode` has two members: `CreditWindow` (default) and `Adaptive`.
- `DispatchQueueFullPolicy` has two members: `DropOldest` (default) and `Block`.
- `StreamingWorkerSupervisorOptions`, `HealthCheckOptions` and `RateLimitingOptions` expose their default values as public `const` fields (`Default*`), which can be used in code instead of the literals above.
