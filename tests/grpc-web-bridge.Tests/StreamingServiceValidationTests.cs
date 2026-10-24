#nullable enable

using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Unit tests for StreamingServiceValidation covering validation of a StreamingService
/// and the streams it tracks, plus null handling and exception behaviour.
/// </summary>
public sealed class StreamingServiceValidationTests
{
    private const string InvalidMethodTypeProblem = "MethodType has invalid value: 999";

    private static StreamingService CreateService() =>
        new StreamingService(NullLogger<StreamingService>.Instance);

    [Fact]
    public void Validate_WithEmptyService_ReturnsEmptyListAndIsValid()
    {
        var service = CreateService();

        service.Validate().Should().BeEmpty();
        service.IsValid().Should().BeTrue();

        Action act = () => service.EnsureValid();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithStreamForEveryDefinedMethodType_ReturnsEmptyList()
    {
        var service = CreateService();
        foreach (var methodType in Enum.GetValues<MethodType>())
        {
            service.CreateStream($"stream-{methodType}", methodType);
        }

        service.Validate().Should().BeEmpty();
        service.IsValid().Should().BeTrue();

        Action act = () => service.EnsureValid();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithUndefinedStreamMethodType_ReportsStreamProblem()
    {
        var service = CreateService();
        var stream = service.CreateStream("bad-stream", MethodType.Unary);
        stream.MethodType = (MethodType)999;

        var problems = service.Validate();

        problems.Should().ContainSingle().Which.Should().Be(InvalidMethodTypeProblem);
        service.IsValid().Should().BeFalse();
    }

    [Fact]
    public void Validate_And_EnsureValid_WithNullService_ThrowsArgumentNullException()
    {
        StreamingService? service = null;

        Action validate = () => service.Validate();
        Action ensureValid = () => service.EnsureValid();

        validate.Should().Throw<ArgumentNullException>();
        ensureValid.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsValid_WithNullService_ReturnsFalseWithoutThrowing()
    {
        StreamingService? service = null;

        // Doc comment says this throws, but the implementation short-circuits on null and returns false.
        service.IsValid().Should().BeFalse();
    }

    [Fact]
    public void EnsureValid_WithInvalidStream_ThrowsArgumentExceptionWithProblems()
    {
        var service = CreateService();
        service.CreateStream("bad-stream", MethodType.Unary).MethodType = (MethodType)999;

        Action act = () => service.EnsureValid();

        var exception = act.Should().Throw<ArgumentException>().Which;
        exception.Message.Should().Contain("StreamingService validation failed")
            .And.Contain(InvalidMethodTypeProblem);
    }
}
