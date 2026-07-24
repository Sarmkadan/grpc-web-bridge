#nullable enable
using System;
using FluentAssertions;
using GrpcWebBridge.Domain.Exceptions;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class ProtocolExceptionValidationTests
{
    [Fact] public void Validate_WithNullProtocolException_ThrowsArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => ((ProtocolException)null!).Validate());

    [Fact] public void Validate_WithValidProtocolException_ReturnsEmptyList()
    {
        var validException = new ProtocolException
        {
            SourceFormat = "JSON",
            TargetFormat = "Protobuf",
            RequestId = "req-123"
        };
        var errors = validException.Validate();
        errors.Should().BeEmpty();
    }

    [Fact] public void Validate_WithNullSourceFormat_ReturnsError()
    {
        var exception = new ProtocolException { SourceFormat = null, TargetFormat = "Protobuf" };
        var errors = exception.Validate();
        errors.Should().HaveCount(1);
        errors[0].Should().Be("SourceFormat is required.");
    }

    [Fact] public void Validate_WithEmptySourceFormat_ReturnsError()
    {
        var exception = new ProtocolException { SourceFormat = string.Empty, TargetFormat = "Protobuf" };
        var errors = exception.Validate();
        errors.Should().HaveCount(1);
        errors[0].Should().Be("SourceFormat is required.");
    }

    [Fact] public void Validate_WithWhitespaceSourceFormat_ReturnsNoError()
    {
        var exception = new ProtocolException { SourceFormat = "   ", TargetFormat = "Protobuf" };
        var errors = exception.Validate();
        errors.Should().BeEmpty();
    }

    [Fact] public void Validate_WithNullTargetFormat_ReturnsError()
    {
        var exception = new ProtocolException { SourceFormat = "JSON", TargetFormat = null };
        var errors = exception.Validate();
        errors.Should().HaveCount(1);
        errors[0].Should().Be("TargetFormat is required.");
    }

    [Fact] public void Validate_WithEmptyTargetFormat_ReturnsError()
    {
        var exception = new ProtocolException { SourceFormat = "JSON", TargetFormat = string.Empty };
        var errors = exception.Validate();
        errors.Should().HaveCount(1);
        errors[0].Should().Be("TargetFormat is required.");
    }

    [Fact] public void Validate_WithWhitespaceTargetFormat_ReturnsNoError()
    {
        var exception = new ProtocolException { SourceFormat = "JSON", TargetFormat = "   " };
        var errors = exception.Validate();
        errors.Should().BeEmpty();
    }

    [Fact] public void Validate_WithEmptyRequestId_ReturnsError()
    {
        var exception = new ProtocolException
        {
            SourceFormat = "JSON",
            TargetFormat = "Protobuf",
            RequestId = string.Empty
        };
        var errors = exception.Validate();
        errors.Should().HaveCount(1);
        errors[0].Should().Be("RequestId must be null or a non-empty string.");
    }

    [Fact] public void Validate_WithWhitespaceRequestId_ReturnsNoError()
    {
        var exception = new ProtocolException
        {
            SourceFormat = "JSON",
            TargetFormat = "Protobuf",
            RequestId = "   "
        };
        var errors = exception.Validate();
        errors.Should().BeEmpty();
    }

    [Fact] public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        var exception = new ProtocolException
        {
            SourceFormat = null,
            TargetFormat = string.Empty,
            RequestId = string.Empty
        };
        var errors = exception.Validate();
        errors.Should().HaveCount(3);
        errors.Should().Contain("SourceFormat is required.");
        errors.Should().Contain("TargetFormat is required.");
        errors.Should().Contain("RequestId must be null or a non-empty string.");
    }

    [Fact] public void IsValid_WithNullProtocolException_ThrowsArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => ((ProtocolException)null!).IsValid());

    [Fact] public void IsValid_WithValidProtocolException_ReturnsTrue()
    {
        var validException = new ProtocolException
        {
            SourceFormat = "JSON",
            TargetFormat = "Protobuf",
            RequestId = "req-123"
        };
        var isValid = validException.IsValid();
        isValid.Should().BeTrue();
    }

    [Fact] public void IsValid_WithInvalidProtocolException_ReturnsFalse()
    {
        var invalidException = new ProtocolException { SourceFormat = null, TargetFormat = "Protobuf" };
        var isValid = invalidException.IsValid();
        isValid.Should().BeFalse();
    }

    [Fact] public void IsValid_WithEmptyRequestId_ReturnsFalse()
    {
        var exception = new ProtocolException
        {
            SourceFormat = "JSON",
            TargetFormat = "Protobuf",
            RequestId = string.Empty
        };
        var isValid = exception.IsValid();
        isValid.Should().BeFalse();
    }

    [Fact] public void IsValid_WithNullRequestId_ReturnsTrue()
    {
        var exception = new ProtocolException
        {
            SourceFormat = "JSON",
            TargetFormat = "Protobuf",
            RequestId = null
        };
        var isValid = exception.IsValid();
        isValid.Should().BeTrue();
    }

    [Fact] public void EnsureValid_WithNullProtocolException_ThrowsArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => ((ProtocolException)null!).EnsureValid());

    [Fact] public void EnsureValid_WithValidProtocolException_DoesNotThrow()
    {
        var validException = new ProtocolException
        {
            SourceFormat = "JSON",
            TargetFormat = "Protobuf",
            RequestId = "req-123"
        };
        Action act = () => validException.EnsureValid();
        act.Should().NotThrow();
    }

    [Fact] public void EnsureValid_WithInvalidProtocolException_ThrowsArgumentException()
    {
        var invalidException = new ProtocolException { SourceFormat = null, TargetFormat = "Protobuf" };
        Action act = () => invalidException.EnsureValid();
        act.Should().Throw<ArgumentException>().WithMessage("*SourceFormat is required*");
    }

    [Fact] public void EnsureValid_WithMultipleErrors_ThrowsArgumentExceptionWithAllErrors()
    {
        var invalidException = new ProtocolException
        {
            SourceFormat = null,
            TargetFormat = string.Empty,
            RequestId = string.Empty
        };
        Action act = () => invalidException.EnsureValid();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*SourceFormat is required*TargetFormat is required*RequestId must be null or a non-empty string*");
    }

    [Fact] public void Validate_ReturnsReadOnlyList()
    {
        var exception = new ProtocolException { SourceFormat = "JSON", TargetFormat = "Protobuf" };
        var errors = exception.Validate();
        errors.Should().BeAssignableTo<IReadOnlyList<string>>();
        errors.Should().NotBeNull();
    }

    [Fact] public void Validate_WithAllPropertiesSet_ReturnsEmptyList()
    {
        var exception = new ProtocolException
        {
            SourceFormat = "JSON",
            TargetFormat = "Protobuf",
            RequestId = "req-123456789"
        };
        var errors = exception.Validate();
        errors.Should().BeEmpty();
    }
}