using BenchmarkDotNet.Attributes;
using GrpcWebBridge.Integration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Http;

namespace GrpcWebBridge.Benchmarks;

[MemoryDiagnoser]
public class HttpClientFactoryBenchmarks
{
    private HttpClientFactory _factory = null!;
    
    [Params(10, 100)]
    public int ClientCount;

    [GlobalSetup]
    public void Setup()
    {
        _factory = new HttpClientFactory(NullLogger<HttpClientFactory>.Instance);
        
        for (int i = 0; i < ClientCount; i++)
        {
            _factory.GetClient($"client-{i}");
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _factory.Dispose();
    }

    [Benchmark]
    public HttpClient GetExistingClient()
    {
        return _factory.GetClient("client-0");
    }

    [Benchmark]
    public HttpClient GetNewClient()
    {
        return _factory.GetClient($"new-client-{Guid.NewGuid()}");
    }

    [Benchmark]
    public HttpClient GetClientForUri()
    {
        return _factory.GetClientForUri("http://example.com");
    }

    [Benchmark]
    public void RegisterClient()
    {
        using var client = new HttpClient();
        _factory.RegisterClient($"registered-{Guid.NewGuid()}", client);
    }
}
