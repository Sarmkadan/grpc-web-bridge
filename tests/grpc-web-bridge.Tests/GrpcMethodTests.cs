#nullable enable

using FluentAssertions;
using GrpcWebBridge.Domain.Models;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class GrpcMethodTests
{
    [Fact]
    public void Constructor_Parameterless_CreatesInstanceWithDefaultValues()
    {
        // Act
        var method = new GrpcMethod();

        // Assert
        method.Name.Should().BeEmpty();
        method.FullName.Should().BeEmpty();
        method.Type.Should().Be(Domain.MethodType.Unary);
        method.InputMessageType.Should().BeEmpty();
        method.OutputMessageType.Should().BeEmpty();
        method.IsDeprecated.Should().BeFalse();
        method.Description.Should().BeNull();
        method.TimeoutMilliseconds.Should().Be(Domain.Constants.Grpc.DefaultTimeout);
        method.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        method.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        const string name = "GetUser";
        const string fullName = "user.UserService.GetUser";
        var type = Domain.MethodType.Unary;
        const string inputMessage = "UserRequest";
        const string outputMessage = "UserResponse";

        // Act
        var method = new GrpcMethod(name, fullName, type, inputMessage, outputMessage);

        // Assert
        method.Name.Should().Be(name);
        method.FullName.Should().Be(fullName);
        method.Type.Should().Be(type);
        method.InputMessageType.Should().Be(inputMessage);
        method.OutputMessageType.Should().Be(outputMessage);
        method.IsDeprecated.Should().BeFalse();
        method.Description.Should().BeNull();
        method.TimeoutMilliseconds.Should().Be(Domain.Constants.Grpc.DefaultTimeout);
        method.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        method.UpdatedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyName_ThrowsArgumentException(string? invalidName)
    {
        // Arrange
        var fullName = "test.FullName";
        var type = Domain.MethodType.Unary;
        const string inputMessage = "Input";
        const string outputMessage = "Output";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new GrpcMethod(invalidName!, fullName, type, inputMessage, outputMessage));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyFullName_ThrowsArgumentException(string? invalidFullName)
    {
        // Arrange
        const string name = "TestMethod";
        var type = Domain.MethodType.Unary;
        const string inputMessage = "Input";
        const string outputMessage = "Output";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new GrpcMethod(name, invalidFullName!, type, inputMessage, outputMessage));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyInputMessage_ThrowsArgumentException(string? invalidInputMessage)
    {
        // Arrange
        const string name = "TestMethod";
        const string fullName = "test.FullName";
        var type = Domain.MethodType.Unary;
        const string outputMessage = "Output";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new GrpcMethod(name, fullName, type, invalidInputMessage!, outputMessage));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyOutputMessage_ThrowsArgumentException(string? invalidOutputMessage)
    {
        // Arrange
        const string name = "TestMethod";
        const string fullName = "test.FullName";
        var type = Domain.MethodType.Unary;
        const string inputMessage = "Input";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new GrpcMethod(name, fullName, type, inputMessage, invalidOutputMessage!));
    }

    [Fact]
    public void AddInputParameter_WithValidParameter_AddsParameterAndUpdatesTimestamp()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );
        var parameter = new MethodParameter("id", "string", 1);

        // Act
        method.AddInputParameter(parameter);

        // Assert
        method.InputParameters.Should().HaveCount(1);
        method.InputParameters.First().Should().BeSameAs(parameter);
        method.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AddInputParameter_NullParameter_ThrowsArgumentNullException()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => method.AddInputParameter(null!));
    }

    [Fact]
    public void AddInputParameter_DuplicateParameter_ThrowsInvalidOperationException()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );
        var parameter = new MethodParameter("id", "string", 1);
        method.AddInputParameter(parameter);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => method.AddInputParameter(parameter));
    }

    [Fact]
    public void AddOutputParameter_WithValidParameter_AddsParameterAndUpdatesTimestamp()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );
        var parameter = new MethodParameter("result", "bool", 1);

        // Act
        method.AddOutputParameter(parameter);

        // Assert
        method.OutputParameters.Should().HaveCount(1);
        method.OutputParameters.First().Should().BeSameAs(parameter);
        method.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AddOutputParameter_NullParameter_ThrowsArgumentNullException()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => method.AddOutputParameter(null!));
    }

    [Fact]
    public void AddOutputParameter_DuplicateParameter_ThrowsInvalidOperationException()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );
        var parameter = new MethodParameter("status", "string", 1);
        method.AddOutputParameter(parameter);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => method.AddOutputParameter(parameter));
    }

    [Fact]
    public void RemoveInputParameter_WithExistingParameter_RemovesParameterAndUpdatesTimestamp()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );
        var parameter = new MethodParameter("id", "string", 1);
        method.AddInputParameter(parameter);

        // Act
        method.RemoveInputParameter("id");

        // Assert
        method.InputParameters.Should().BeEmpty();
        method.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RemoveInputParameter_WithNonExistingParameter_DoesNothing()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );

        // Act
        method.RemoveInputParameter("nonExistent");

        // Assert
        method.InputParameters.Should().BeEmpty();
        method.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Validate_WithValidMethod_DoesNotThrow()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );
        method.TimeoutMilliseconds = 5000;

        // Act
        var act = () => method.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var method = new GrpcMethod(
            "",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );

        // Act & Assert
        var act = () => method.Validate();
        act.Should().Throw<ArgumentException>("Method name cannot be empty");
    }

    [Fact]
    public void Validate_WithEmptyFullName_ThrowsArgumentException()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );

        // Act & Assert
        var act = () => method.Validate();
        act.Should().Throw<ArgumentException>("Method full name cannot be empty");
    }

    [Fact]
    public void Validate_WithEmptyInputMessage_ThrowsArgumentException()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "",
            "OutputMessage"
        );

        // Act & Assert
        var act = () => method.Validate();
        act.Should().Throw<ArgumentException>("Input message type cannot be empty");
    }

    [Fact]
    public void Validate_WithEmptyOutputMessage_ThrowsArgumentException()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            ""
        );

        // Act & Assert
        var act = () => method.Validate();
        act.Should().Throw<ArgumentException>("Output message type cannot be empty");
    }

    [Fact]
    public void Validate_WithInvalidTimeout_ThrowsArgumentException()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );
        method.TimeoutMilliseconds = 0;

        // Act & Assert
        var act = () => method.Validate();
        act.Should().Throw<ArgumentException>("Timeout must be greater than 0");
    }

    [Theory]
    [InlineData(Domain.MethodType.Unary)]
    [InlineData(Domain.MethodType.ClientStreaming)]
    [InlineData(Domain.MethodType.ServerStreaming)]
    [InlineData(Domain.MethodType.BidirectionalStreaming)]
    public void Type_SetToAllValues_StoresCorrectly(Domain.MethodType type)
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );

        // Act
        method.Type = type;

        // Assert
        method.Type.Should().Be(type);
    }

    [Fact]
    public void IsDeprecated_SetToTrue_SetsProperty()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );

        // Act
        method.IsDeprecated = true;

        // Assert
        method.IsDeprecated.Should().BeTrue();
    }

    [Fact]
    public void Description_SetToValue_SetsProperty()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );
        const string description = "This is a test method description";

        // Act
        method.Description = description;

        // Assert
        method.Description.Should().Be(description);
    }

    [Fact]
    public void Description_SetToNull_SetsPropertyToNull()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );
        method.Description = "Test";

        // Act
        method.Description = null;

        // Assert
        method.Description.Should().BeNull();
    }

    [Fact]
    public void TimeoutMilliseconds_SetToValue_SetsProperty()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );
        const int timeout = 10000;

        // Act
        method.TimeoutMilliseconds = timeout;

        // Assert
        method.TimeoutMilliseconds.Should().Be(timeout);
    }

    [Fact]
    public void CreatedAt_SetToPastDate_SetsProperty()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );
        var pastDate = DateTime.UtcNow.AddDays(-1);

        // Act
        method.CreatedAt = pastDate;

        // Assert
        method.CreatedAt.Should().Be(pastDate);
    }

    [Fact]
    public void UpdatedAt_SetToDate_SetsProperty()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );
        var updateDate = DateTime.UtcNow.AddHours(-1);

        // Act
        method.UpdatedAt = updateDate;

        // Assert
        method.UpdatedAt.Should().Be(updateDate);
    }

    [Fact]
    public void UpdatedAt_SetToNull_SetsPropertyToNull()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );
        method.UpdatedAt = DateTime.UtcNow;

        // Act
        method.UpdatedAt = null;

        // Assert
        method.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );

        // Act
        var result = method.ToString();

        // Assert
        result.Should().Be("test.TestMethod (Unary)");
    }

    [Fact]
    public void Equals_WithSameFullNameAndType_ReturnsTrue()
    {
        // Arrange
        var method1 = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );
        var method2 = new GrpcMethod(
            "DifferentName",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "DifferentInput",
            "DifferentOutput"
        );

        // Act & Assert
        method1.Equals(method2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentFullName_ReturnsFalse()
    {
        // Arrange
        var method1 = new GrpcMethod(
            "TestMethod",
            "test.TestMethod1",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );
        var method2 = new GrpcMethod(
            "TestMethod",
            "test.TestMethod2",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );

        // Act & Assert
        method1.Equals(method2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        // Arrange
        var method1 = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );
        var method2 = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.ClientStreaming,
            "InputMessage",
            "OutputMessage"
        );

        // Act & Assert
        method1.Equals(method2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );

        // Act & Assert
        method.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ForSameMethods_ReturnsSameValue()
    {
        // Arrange
        var method1 = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );
        var method2 = new GrpcMethod(
            "DifferentName",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "DifferentInput",
            "DifferentOutput"
        );

        // Act
        var hash1 = method1.GetHashCode();
        var hash2 = method2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void GetHashCode_ForDifferentMethods_ReturnsDifferentValue()
    {
        // Arrange
        var method1 = new GrpcMethod(
            "TestMethod",
            "test.TestMethod1",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );
        var method2 = new GrpcMethod(
            "TestMethod",
            "test.TestMethod2",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );

        // Act
        var hash1 = method1.GetHashCode();
        var hash2 = method2.GetHashCode();

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void InputParameters_ReturnsReadOnlyCollection()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );
        var parameter = new MethodParameter("id", "string", 1);
        method.AddInputParameter(parameter);

        // Act
        var parameters = method.InputParameters;

        // Assert
        parameters.Should().BeAssignableTo<IReadOnlyCollection<MethodParameter>>();
        parameters.Should().ContainSingle().Which.Should().BeSameAs(parameter);
    }

    [Fact]
    public void OutputParameters_ReturnsReadOnlyCollection()
    {
        // Arrange
        var method = new GrpcMethod(
            "TestMethod",
            "test.TestMethod",
            Domain.MethodType.Unary,
            "InputMessage",
            "OutputMessage"
        );
        var parameter = new MethodParameter("result", "bool", 1);
        method.AddOutputParameter(parameter);

        // Act
        var parameters = method.OutputParameters;

        // Assert
        parameters.Should().BeAssignableTo<IReadOnlyCollection<MethodParameter>>();
        parameters.Should().ContainSingle().Which.Should().BeSameAs(parameter);
    }
}
