using System.Collections.Generic;
using GrpcWebBridge.Domain.Models;
using Xunit;

namespace GrpcWebBridge.Tests;

public class GrpcServiceDescriptorTests
{
    [Fact]
    public void Constructor_WithAllPropertiesSet_ShouldAssignValuesCorrectly()
    {
        // Arrange
        var method1 = new MethodDescriptor
        {
            Name = "UnaryMethod",
            FullName = "pkg.Service/UnaryMethod",
            ServiceFullName = "pkg.Service",
            MethodType = "Unary",
            IsClientStreaming = false,
            IsServerStreaming = false,
            InputMessageType = "Request1",
            OutputMessageType = "Response1",
            IsDeprecated = false,
            Description = "A simple unary method",
            TimeoutMilliseconds = 5000
        };

        var method2 = new MethodDescriptor
        {
            Name = "StreamingMethod",
            FullName = "pkg.Service/StreamingMethod",
            ServiceFullName = "pkg.Service",
            MethodType = "ServerStreaming",
            IsClientStreaming = false,
            IsServerStreaming = true,
            InputMessageType = "Request2",
            OutputMessageType = "Response2",
            IsDeprecated = true,
            Description = null,
            TimeoutMilliseconds = 10000
        };

        var descriptor = new GrpcServiceDescriptor
        {
            FullName = "pkg.Service",
            Name = "Service",
            PackageName = "pkg",
            Description = "Test service description",
            Endpoint = "localhost",
            Port = 50051,
            UseTls = true,
            Methods = new List<MethodDescriptor> { method1, method2 }
        };

        // Act & Assert
        Assert.Equal("pkg.Service", descriptor.FullName);
        Assert.Equal("Service", descriptor.Name);
        Assert.Equal("pkg", descriptor.PackageName);
        Assert.Equal("Test service description", descriptor.Description);
        Assert.Equal("localhost", descriptor.Endpoint);
        Assert.Equal(50051, descriptor.Port);
        Assert.True(descriptor.UseTls);
        Assert.Equal(2, descriptor.Methods.Count);
        Assert.Contains(method1, descriptor.Methods);
        Assert.Contains(method2, descriptor.Methods);
    }

    [Fact]
    public void Constructor_WithNullDescriptionAndEmptyMethods_ShouldHandleDefaults()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            FullName = "pkg.EmptyService",
            Name = "EmptyService",
            PackageName = "pkg",
            Description = null,
            Endpoint = "127.0.0.1",
            Port = 0,
            UseTls = false,
            Methods = new List<MethodDescriptor>()
        };

        // Act & Assert
        Assert.Equal("pkg.EmptyService", descriptor.FullName);
        Assert.Equal("EmptyService", descriptor.Name);
        Assert.Equal("pkg", descriptor.PackageName);
        Assert.Null(descriptor.Description);
        Assert.Equal("127.0.0.1", descriptor.Endpoint);
        Assert.Equal(0, descriptor.Port); // boundary value
        Assert.False(descriptor.UseTls);
        Assert.Empty(descriptor.Methods);
    }

    [Fact]
    public void MethodDescriptor_DefaultValues_ShouldBeInitializedCorrectly()
    {
        // Arrange
        var method = new MethodDescriptor
        {
            Name = "DefaultMethod",
            FullName = "pkg.Service/DefaultMethod",
            ServiceFullName = "pkg.Service",
            MethodType = "Unary",
            InputMessageType = "Req",
            OutputMessageType = "Res"
            // Booleans and nullable fields left at defaults
        };

        // Act & Assert
        Assert.Equal("DefaultMethod", method.Name);
        Assert.Equal("pkg.Service/DefaultMethod", method.FullName);
        Assert.Equal("pkg.Service", method.ServiceFullName);
        Assert.Equal("Unary", method.MethodType);
        Assert.False(method.IsClientStreaming);
        Assert.False(method.IsServerStreaming);
        Assert.Equal("Req", method.InputMessageType);
        Assert.Equal("Res", method.OutputMessageType);
        Assert.False(method.IsDeprecated);
        Assert.Null(method.Description);
        Assert.Equal(0, method.TimeoutMilliseconds);
    }

    [Fact]
    public void MethodsCollection_ShouldBeReadOnlyFromConsumerPerspective()
    {
        // Arrange
        var mutableList = new List<MethodDescriptor>
        {
            new MethodDescriptor { Name = "M1", FullName = "svc/M1" }
        };

        var descriptor = new GrpcServiceDescriptor
        {
            FullName = "svc",
            Name = "svc",
            PackageName = "pkg",
            Endpoint = "host",
            Port = 1234,
            UseTls = false,
            Methods = mutableList
        };

        // Act
        // The property type is IReadOnlyCollection, so we cannot call Add directly.
        // Verify that the reference returned is the same instance we supplied.
        Assert.Same(mutableList, descriptor.Methods);

        // Mutating the original list after construction should be reflected,
        // which demonstrates that the descriptor does not create a defensive copy.
        mutableList.Add(new MethodDescriptor { Name = "M2", FullName = "svc/M2" });

        Assert.Equal(2, descriptor.Methods.Count);
    }

    [Fact]
    public void Descriptor_WithOnlyRequiredProperties_ShouldUseDefaultValues()
    {
        // Arrange
        var descriptor = new GrpcServiceDescriptor
        {
            FullName = "pkg.MinService",
            Name = "MinService",
            PackageName = "pkg",
            Endpoint = "example.com",
            Port = 80,
            UseTls = false
            // Description and Methods left as defaults
        };

        // Act & Assert
        Assert.Equal(string.Empty, descriptor.Description);
        Assert.Empty(descriptor.Methods);
    }
}
