#nullable enable
// =============================================================================
// Author: Automated Generation
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class StreamingExceptionExtensionsTests
{
    private readonly StreamingException _exception;

    public StreamingExceptionExtensionsTests()
    {
        _exception = new StreamingException("Test message");
    }

    // ... rest of the file ...
}
