# GrpcConnectionManagerExtensions
The `GrpcConnectionManagerExtensions` class provides a set of extension methods for managing and monitoring gRPC connections. It offers various metrics and information about the connections, such as connection counts, request counts, bytes sent and received, and connection durations. These methods can be used to monitor the performance and health of gRPC connections in an application.

## API
The following members are available:
* `GetAllMetrics`: Returns an enumerable collection of all connection metrics.
* `GetActiveConnectionCount`: Returns the number of active connections.
* `GetTotalRequestCount`: Returns the total number of requests made.
* `GetTotalBytesSent`: Returns the total number of bytes sent.
* `GetTotalBytesReceived`: Returns the total number of bytes received.
* `GetAverageConnectionDuration`: Returns the average duration of all connections.
* `GetMostRecentlyUsed`: Returns the most recently used connection metrics, or `null` if no connections have been used.
* `GetOldestConnection`: Returns the oldest connection metrics, or `null` if no connections exist.
* `GetMostActiveConnection`: Returns the most active connection metrics, or `null` if no connections exist.
* `GetHighestThroughputConnection`: Returns the connection metrics with the highest throughput, or `null` if no connections exist.
* `GetAllConnectionAddresses`: Returns an enumerable collection of all connection addresses.
* `GetAllServiceNames`: Returns an enumerable collection of all service names.
* `GetMetricsByService`: Returns a dictionary of connection metrics grouped by service name.
* `GetConnectionDuration`: Returns the duration of a connection.
* `GetRequestCount`: Returns the number of requests made on a connection.
* `GetBytesSent`: Returns the number of bytes sent on a connection.
* `GetBytesReceived`: Returns the number of bytes received on a connection.
* `GetLastUsedAt`: Returns the date and time a connection was last used.
* `GetCreatedAt`: Returns the date and time a connection was created.
* `IsServiceConnected`: Returns a boolean indicating whether a service is connected.

## Usage
Here are two examples of using the `GrpcConnectionManagerExtensions` class:
```csharp
// Example 1: Get the total request count and average connection duration
var totalRequestCount = GrpcConnectionManagerExtensions.GetTotalRequestCount();
var averageConnectionDuration = GrpcConnectionManagerExtensions.GetAverageConnectionDuration();
Console.WriteLine($"Total requests: {totalRequestCount}, Average connection duration: {averageConnectionDuration}");

// Example 2: Get the metrics for the most recently used connection
var mostRecentlyUsedConnection = GrpcConnectionManagerExtensions.GetMostRecentlyUsed();
if (mostRecentlyUsedConnection != null)
{
    Console.WriteLine($"Most recently used connection: {mostRecentlyUsedConnection}");
    Console.WriteLine($"Request count: {GrpcConnectionManagerExtensions.GetRequestCount(mostRecentlyUsedConnection)}");
    Console.WriteLine($"Bytes sent: {GrpcConnectionManagerExtensions.GetBytesSent(mostRecentlyUsedConnection)}");
    Console.WriteLine($"Bytes received: {GrpcConnectionManagerExtensions.GetBytesReceived(mostRecentlyUsedConnection)}");
}
```

## Notes
Note that the `GetMostRecentlyUsed`, `GetOldestConnection`, `GetMostActiveConnection`, and `GetHighestThroughputConnection` methods may return `null` if no connections have been used or exist. Additionally, the `GetMetricsByService` method returns a dictionary that may be empty if no connections exist for a particular service. The `IsServiceConnected` method may throw an exception if the service is not found. The `GrpcConnectionManagerExtensions` class is designed to be thread-safe, but it is still important to ensure that the underlying connections are properly synchronized to avoid concurrency issues.
