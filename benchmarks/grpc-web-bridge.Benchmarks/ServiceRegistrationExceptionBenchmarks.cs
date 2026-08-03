using BenchmarkDotNet.Attributes;
using GrpcWebBridge.Domain.Exceptions;

namespace GrpcWebBridge.Benchmarks;

[MemoryDiagnoser]
public class ServiceRegistrationExceptionBenchmarks
{
    private string _message = null!;
    private string _serviceName = null!;
    private string _serviceUrl = null!;
    private Exception _innerException = null!;
    private ServiceRegistrationException _exceptionWithAllParams = null!;

    [Params(10, 100, 1000)]
    public int MessageLength;

    [GlobalSetup]
    public void Setup()
    {
        _message = new string('x', MessageLength);
        _serviceName = "ServiceName";
        _serviceUrl = "http://example.com";
        _innerException = new InvalidOperationException("inner exception");

        _exceptionWithAllParams = new ServiceRegistrationException(_serviceName, _serviceUrl, _message);
    }

    [Benchmark]
    public ServiceRegistrationException DefaultConstructor() => new ServiceRegistrationException();

    [Benchmark]
    public ServiceRegistrationException ConstructorWithMessage() => new ServiceRegistrationException(_message);

    [Benchmark]
    public ServiceRegistrationException ConstructorWithMessageAndInner() => new ServiceRegistrationException(_message, _innerException);

    [Benchmark]
    public ServiceRegistrationException ConstructorWithAllParams() => new ServiceRegistrationException(_serviceName, _serviceUrl, _message);

    [Benchmark]
    public string ToStringMethod() => _exceptionWithAllParams.ToString();
}