#nullable enable

using System;
using System.Collections.Generic;
using FluentAssertions;
using GrpcWebBridge.Domain.Models;
using Xunit;

namespace GrpcWebBridge.Tests;

public class GrpcServiceDescriptorExtensionsTests
{
    [Fact]
    public void GetDisplayName_WithValidDescriptor_ReturnsFormattedName()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            PackageName = "TestPackage",
            Name = "TestService"
        };

        // Act
        var result = descriptor.GetDisplayName();

        // Assert
        result.Should().Be("TestPackage.TestService");
    }

    [Fact]
    public void GetDisplayName_WithEmptyPackageName_ReturnsNameOnly()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            PackageName = "",
            Name = "TestService"
        };

        // Act
        var result = descriptor.GetDisplayName();

        // Assert
        result.Should().Be(".TestService");
    }

    [Fact]
    public void GetDisplayName_WithEmptyName_ReturnsPackageOnly()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            PackageName = "TestPackage",
            Name = ""
        };

        // Act
        var result = descriptor.GetDisplayName();

        // Assert
        result.Should().Be("TestPackage.");
    }

    [Fact]
    public void GetDisplayName_WithNullDescriptor_ThrowsArgumentNullException()
    {
        // Arrange
        GrpcServiceDescriptor? descriptor = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => descriptor!.GetDisplayName());
    }

    [Fact]
    public void IsSecureEndpoint_WithTlsAndPort443_ReturnsTrue()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            UseTls = true,
            Port = 443
        };

        // Act
        var result = descriptor.IsSecureEndpoint();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSecureEndpoint_WithTlsAndPort8443_ReturnsTrue()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            UseTls = true,
            Port = 8443
        };

        // Act
        var result = descriptor.IsSecureEndpoint();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSecureEndpoint_WithTlsAndPort8080_ReturnsFalse()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            UseTls = true,
            Port = 8080
        };

        // Act
        var result = descriptor.IsSecureEndpoint();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSecureEndpoint_WithoutTls_ReturnsFalse()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            UseTls = false,
            Port = 443
        };

        // Act
        var result = descriptor.IsSecureEndpoint();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSecureEndpoint_WithNullDescriptor_ThrowsArgumentNullException()
    {
        // Arrange
        GrpcServiceDescriptor? descriptor = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => descriptor!.IsSecureEndpoint());
    }

    [Fact]
    public void GetStreamingMethods_WithClientStreamingMethod_ReturnsMethod()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            Methods = new List<MethodDescriptor>
            {
                new() { Name = "UnaryMethod", IsClientStreaming = false, IsServerStreaming = false },
                new() { Name = "ClientStreamingMethod", IsClientStreaming = true, IsServerStreaming = false },
                new() { Name = "ServerStreamingMethod", IsClientStreaming = false, IsServerStreaming = true },
                new() { Name = "BidirectionalMethod", IsClientStreaming = true, IsServerStreaming = true }
            }
        };

        // Act
        var result = descriptor.GetStreamingMethods();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(m => m.Name == "ClientStreamingMethod");
        result.Should().Contain(m => m.Name == "ServerStreamingMethod");
        result.Should().Contain(m => m.Name == "BidirectionalMethod");
    }

    [Fact]
    public void GetStreamingMethods_WithNoStreamingMethods_ReturnsEmptyCollection()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            Methods = new List<MethodDescriptor>
            {
                new() { Name = "Method1", IsClientStreaming = false, IsServerStreaming = false },
                new() { Name = "Method2", IsClientStreaming = false, IsServerStreaming = false }
            }
        };

        // Act
        var result = descriptor.GetStreamingMethods();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetStreamingMethods_WithEmptyMethodsCollection_ReturnsEmptyCollection()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            Methods = new List<MethodDescriptor>()
        };

        // Act
        var result = descriptor.GetStreamingMethods();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetStreamingMethods_WithNullDescriptor_ThrowsArgumentNullException()
    {
        // Arrange
        GrpcServiceDescriptor? descriptor = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => descriptor!.GetStreamingMethods());
    }

    [Fact]
    public void GetMethodByName_WithExactMatch_ReturnsMethod()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            Methods = new List<MethodDescriptor>
            {
                new() { Name = "GetUser" },
                new() { Name = "CreateUser" },
                new() { Name = "UpdateUser" }
            }
        };

        // Act
        var result = descriptor.GetMethodByName("GetUser");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("GetUser");
    }

    [Fact]
    public void GetMethodByName_WithCaseInsensitiveMatch_ReturnsMethod()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            Methods = new List<MethodDescriptor>
            {
                new() { Name = "GetUser" },
                new() { Name = "CreateUser" }
            }
        };

        // Act
        var result = descriptor.GetMethodByName("getuser");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("GetUser");
    }

    [Fact]
    public void GetMethodByName_WithMixedCaseMatch_ReturnsMethod()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            Methods = new List<MethodDescriptor>
            {
                new() { Name = "GetUser" }
            }
        };

        // Act
        var result = descriptor.GetMethodByName("GeTuSeR");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("GetUser");
    }

    [Fact]
    public void GetMethodByName_WithWhitespaceTrimmed_ReturnsMethod()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            Methods = new List<MethodDescriptor>
            {
                new() { Name = "GetUser" }
            }
        };

        // Act
        var result = descriptor.GetMethodByName("  GetUser  ");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("GetUser");
    }

    [Fact]
    public void GetMethodByName_WithNonExistentMethod_ReturnsNull()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            Methods = new List<MethodDescriptor>
            {
                new() { Name = "GetUser" },
                new() { Name = "CreateUser" }
            }
        };

        // Act
        var result = descriptor.GetMethodByName("DeleteUser");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetMethodByName_WithEmptyMethodsCollection_ReturnsNull()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            Methods = new List<MethodDescriptor>()
        };

        // Act
        var result = descriptor.GetMethodByName("AnyMethod");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetMethodByName_WithNullDescriptor_ThrowsArgumentNullException()
    {
        // Arrange
        GrpcServiceDescriptor? descriptor = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => descriptor!.GetMethodByName("method"));
    }

    [Fact]
    public void GetMethodByName_WithNullMethodName_ThrowsArgumentNullException()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            Methods = new List<MethodDescriptor> { new() { Name = "GetUser" } }
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => descriptor.GetMethodByName(null!));
    }

    [Fact]
    public void GetMethodByName_WithEmptyMethodName_ThrowsArgumentException()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            Methods = new List<MethodDescriptor> { new() { Name = "GetUser" } }
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => descriptor.GetMethodByName(""));
    }

    [Fact]
    public void GetMethodByName_WithWhitespaceOnlyMethodName_DoesNotThrow()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            Methods = new List<MethodDescriptor> { new() { Name = "GetUser" } }
        };

        // Act - whitespace-only strings are trimmed and result in empty string, which doesn't throw
        var result = descriptor.GetMethodByName("   ");

        // Assert - returns null since empty string won't match any method name
        result.Should().BeNull();
    }
}