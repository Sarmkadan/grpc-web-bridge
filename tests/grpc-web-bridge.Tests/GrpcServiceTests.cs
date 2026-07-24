#nullable enable

using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class GrpcServiceTests
{
    [Fact]
    public void Constructor_Parameterless_CreatesValidInstance()
    {
        // Act
        var service = new GrpcService();

        // Assert
        service.Should().NotBeNull();
        service.Id.Should().NotBeNullOrEmpty();
        service.Name.Should().BeEmpty();
        service.PackageName.Should().Be(Constants.ServiceRegistry.DefaultNamespace);
        service.FullName.Should().BeEmpty();
        service.Description.Should().BeNull();
        service.Endpoint.Should().BeEmpty();
        service.Port.Should().Be(50051);
        service.UseTls.Should().BeFalse();
        service.Status.Should().Be(ServiceStatus.Serving);
        service.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        service.UpdatedAt.Should().BeNull();
        service.Metadata.Should().BeEmpty();
        service.Methods.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithParameters_CreatesValidInstance()
    {
        // Arrange
        const string name = "TestService";
        const string packageName = "Test.Package";
        const string endpoint = "localhost";
        const int port = 8080;

        // Act
        var service = new GrpcService(name, packageName, endpoint, port);

        // Assert
        service.Should().NotBeNull();
        service.Id.Should().NotBeNullOrEmpty();
        service.Name.Should().Be(name);
        service.PackageName.Should().Be(packageName);
        service.FullName.Should().Be($"{packageName}.{name}");
        service.Description.Should().BeNull();
        service.Endpoint.Should().Be(endpoint);
        service.Port.Should().Be(port);
        service.UseTls.Should().BeFalse();
        service.Status.Should().Be(ServiceStatus.Serving);
        service.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        service.UpdatedAt.Should().BeNull();
        service.Metadata.Should().BeEmpty();
        service.Methods.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "package", "endpoint", 50051)]
    [InlineData("", "package", "endpoint", 50051)]
    [InlineData("   ", "package", "endpoint", 50051)]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name, string packageName, string endpoint, int port)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new GrpcService(name!, packageName, endpoint, port));
    }

    [Theory]
    [InlineData("name", null, "endpoint", 50051)]
    [InlineData("name", "", "endpoint", 50051)]
    [InlineData("name", "   ", "endpoint", 50051)]
    public void Constructor_WithInvalidPackageName_ThrowsArgumentException(string name, string? packageName, string endpoint, int port)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new GrpcService(name, packageName!, endpoint, port));
    }

    [Theory]
    [InlineData("name", "package", null, 50051)]
    [InlineData("name", "package", "", 50051)]
    [InlineData("name", "package", "   ", 50051)]
    public void Constructor_WithInvalidEndpoint_ThrowsArgumentException(string name, string packageName, string? endpoint, int port)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new GrpcService(name, packageName, endpoint!, port));
    }

    [Theory]
    [InlineData("name", "package", "endpoint", 0)]
    [InlineData("name", "package", "endpoint", -1)]
    [InlineData("name", "package", "endpoint", 65536)]
    public void Constructor_WithInvalidPort_ThrowsArgumentException(string name, string packageName, string endpoint, int port)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new GrpcService(name, packageName, endpoint, port));
    }

    [Fact]
    public void AddMethod_WithValidMethod_AddsMethodAndUpdatesTimestamp()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);
        var method = new GrpcMethod("TestMethod", "Test.Package.TestMethod", MethodType.Unary, "RequestType", "ResponseType");

        // Act
        service.AddMethod(method);

        // Assert
        service.Methods.Should().HaveCount(1);
        service.Methods.Should().Contain(method);
        service.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AddMethod_NullMethod_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.AddMethod(null!));
    }

    [Fact]
    public void AddMethod_DuplicateMethod_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);
        var method1 = new GrpcMethod("TestMethod", "Test.Package.TestMethod", MethodType.Unary, "RequestType", "ResponseType");
        var method2 = new GrpcMethod("AnotherMethod", "Test.Package.TestMethod", MethodType.Unary, "RequestType2", "ResponseType2");
        service.AddMethod(method1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.AddMethod(method2));
    }

    [Fact]
    public void GetMethod_ExistingMethod_ReturnsMethod()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);
        var method = new GrpcMethod("TestMethod", "Test.Package.TestMethod", MethodType.Unary, "RequestType", "ResponseType");
        service.AddMethod(method);

        // Act
        var result = service.GetMethod("TestMethod");

        // Assert
        result.Should().BeSameAs(method);
    }

    [Fact]
    public void GetMethod_ByFullName_ReturnsMethod()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);
        var method = new GrpcMethod("TestMethod", "Test.Package.TestMethod", MethodType.Unary, "RequestType", "ResponseType");
        service.AddMethod(method);

        // Act
        var result = service.GetMethod("Test.Package.TestMethod");

        // Assert
        result.Should().BeSameAs(method);
    }

    [Fact]
    public void GetMethod_NonExistentMethod_ReturnsNull()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);

        // Act
        var result = service.GetMethod("NonExistentMethod");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void HasMethod_ExistingMethod_ReturnsTrue()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);
        var method = new GrpcMethod("TestMethod", "Test.Package.TestMethod", MethodType.Unary, "RequestType", "ResponseType");
        service.AddMethod(method);

        // Act
        var result = service.HasMethod("TestMethod");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasMethod_NonExistentMethod_ReturnsFalse()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);

        // Act
        var result = service.HasMethod("NonExistentMethod");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveMethod_ExistingMethod_RemovesMethodAndUpdatesTimestamp()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);
        var method = new GrpcMethod("TestMethod", "Test.Package.TestMethod", MethodType.Unary, "RequestType", "ResponseType");
        service.AddMethod(method);

        // Act
        service.RemoveMethod("TestMethod");

        // Assert
        service.Methods.Should().BeEmpty();
        service.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RemoveMethod_NonExistentMethod_DoesNotThrow()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);

        // Act
        var act = () => service.RemoveMethod("NonExistentMethod");

        // Assert
        act.Should().NotThrow();
        service.Methods.Should().BeEmpty();
    }

    [Fact]
    public void SetMetadata_WithValidKeyValue_SetsMetadataAndUpdatesTimestamp()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);

        // Act
        service.SetMetadata("key1", "value1");
        service.SetMetadata("key2", "value2");

        // Assert
        service.Metadata.Should().HaveCount(2);
        service.Metadata["key1"].Should().Be("value1");
        service.Metadata["key2"].Should().Be("value2");
        service.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetMetadata_WithInvalidKey_ThrowsArgumentException(string? key)
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.SetMetadata(key!, "value"));
    }

    [Fact]
    public void GetMetadata_ExistingKey_ReturnsValue()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);
        service.SetMetadata("key1", "value1");

        // Act
        var result = service.GetMetadata("key1");

        // Assert
        result.Should().Be("value1");
    }

    [Fact]
    public void GetMetadata_NonExistentKey_ReturnsNull()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);

        // Act
        var result = service.GetMetadata("nonExistentKey");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Validate_ValidService_DoesNotThrow()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);
        var method = new GrpcMethod("TestMethod", "Test.Package.TestMethod", MethodType.Unary, "RequestType", "ResponseType");
        service.AddMethod(method);

        // Act
        var act = () => service.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);
        service.Name = "";
        var method = new GrpcMethod("TestMethod", "Test.Package.TestMethod", MethodType.Unary, "RequestType", "ResponseType");
        service.AddMethod(method);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.Validate());
    }

    [Fact]
    public void Validate_EmptyPackageName_ThrowsArgumentException()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);
        service.PackageName = "";
        var method = new GrpcMethod("TestMethod", "Test.Package.TestMethod", MethodType.Unary, "RequestType", "ResponseType");
        service.AddMethod(method);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.Validate());
    }

    [Fact]
    public void Validate_EmptyEndpoint_ThrowsArgumentException()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);
        service.Endpoint = "";
        var method = new GrpcMethod("TestMethod", "Test.Package.TestMethod", MethodType.Unary, "RequestType", "ResponseType");
        service.AddMethod(method);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.Validate());
    }

    [Fact]
    public void Validate_InvalidPort_ThrowsArgumentException()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);
        service.Port = 0;
        var method = new GrpcMethod("TestMethod", "Test.Package.TestMethod", MethodType.Unary, "RequestType", "ResponseType");
        service.AddMethod(method);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.Validate());
    }

    [Fact]
    public void Validate_NoMethods_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.Validate());
    }

    [Fact]
    public void ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 8080);
        service.FullName = "Test.Package.TestService";

        // Act
        var result = service.ToString();

        // Assert
        result.Should().Be("Test.Package.TestService (localhost:8080)");
    }

    [Fact]
    public void Equals_SameId_ReturnsTrue()
    {
        // Arrange
        var service1 = new GrpcService("TestService", "Test.Package", "localhost", 50051);
        var service2 = new GrpcService("DifferentName", "Different.Package", "different", 9000);
        service2.Id = service1.Id; // Same ID

        // Act
        var result = service1.Equals(service2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        // Arrange
        var service1 = new GrpcService("TestService", "Test.Package", "localhost", 50051);
        var service2 = new GrpcService("TestService", "Test.Package", "localhost", 50051);

        // Act
        var result = service1.Equals(service2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ReturnsConsistentValue()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051);
        var originalHash = service.GetHashCode();

        // Act
        var newService = new GrpcService("DifferentName", "Different.Package", "different", 9000);
        newService.Id = service.Id; // Same ID
        var sameHash = newService.GetHashCode();

        // Assert
        originalHash.Should().Be(sameHash);
    }

    [Fact]
    public void Property_Setters_WorkCorrectly()
    {
        // Arrange
        var service = new GrpcService();

        // Act
        service.Name = "UpdatedName";
        service.PackageName = "Updated.Package";
        service.FullName = "Updated.Package.UpdatedName";
        service.Description = "Test description";
        service.Endpoint = "updated.localhost";
        service.Port = 9090;
        service.UseTls = true;
        service.Status = ServiceStatus.NotServing;
        service.UpdatedAt = DateTime.UtcNow.AddHours(-1);
        service.Metadata.Add("customKey", "customValue");

        // Assert
        service.Name.Should().Be("UpdatedName");
        service.PackageName.Should().Be("Updated.Package");
        service.FullName.Should().Be("Updated.Package.UpdatedName");
        service.Description.Should().Be("Test description");
        service.Endpoint.Should().Be("updated.localhost");
        service.Port.Should().Be(9090);
        service.UseTls.Should().BeTrue();
        service.Status.Should().Be(ServiceStatus.NotServing);
        service.UpdatedAt.Should().NotBeNull();
        service.Metadata.Should().ContainKey("customKey").WhoseValue.Should().Be("customValue");
    }
}
