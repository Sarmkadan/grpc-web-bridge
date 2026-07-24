#nullable enable

using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class GrpcMethodExtensionsTests
{
    [Fact]
    public void ToCSharpSignature_NullMethod_ThrowsArgumentNullException()
    {
        // Arrange
        GrpcMethod? method = null;

        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => method!.ToCSharpSignature());
    }

    [Fact]
    public void ToCSharpSignature_UnaryMethod_GeneratesCorrectSignature()
    {
        // Arrange
        var method = new GrpcMethod
        {
            Name = "GetUser",
            FullName = "users.UserService/GetUser",
            Type = MethodType.Unary,
            InputMessageType = "UserRequest",
            OutputMessageType = "UserResponse"
        };

        // Act
        var result = method.ToCSharpSignature();

        // Assert
        result.Should().Be("public async Task<UserResponse> GetUserAsync(UserRequest request, CancellationToken cancellationToken = default)");
    }

    [Fact]
    public void ToCSharpSignature_UnaryMethod_WithoutAsyncModifier_GeneratesCorrectSignature()
    {
        // Arrange
        var method = new GrpcMethod
        {
            Name = "GetUser",
            FullName = "users.UserService/GetUser",
            Type = MethodType.Unary,
            InputMessageType = "UserRequest",
            OutputMessageType = "UserResponse"
        };

        // Act
        var result = method.ToCSharpSignature(includeAsync: false);

        // Assert
        result.Should().Be("public Task<UserResponse> GetUserAsync(UserRequest request, CancellationToken cancellationToken = default)");
    }

    [Fact]
    public void ToCSharpSignature_UnaryMethod_WithoutCancellationToken_GeneratesCorrectSignature()
    {
        // Arrange
        var method = new GrpcMethod
        {
            Name = "GetUser",
            FullName = "users.UserService/GetUser",
            Type = MethodType.Unary,
            InputMessageType = "UserRequest",
            OutputMessageType = "UserResponse"
        };

        // Act
        var result = method.ToCSharpSignature(includeCancellationToken: false);

        // Assert
        result.Should().Be("public async Task<UserResponse> GetUserAsync(UserRequest request)");
    }

    [Fact]
    public void ToCSharpSignature_ServerStreamingMethod_GeneratesCorrectSignature()
    {
        // Arrange
        var method = new GrpcMethod
        {
            Name = "SubscribeToUpdates",
            FullName = "updates.UpdateService/SubscribeToUpdates",
            Type = MethodType.ServerStreaming,
            InputMessageType = "UpdateRequest",
            OutputMessageType = "UpdateResponse"
        };

        // Act
        var result = method.ToCSharpSignature();

        // Assert
        result.Should().Be("public async IAsyncEnumerable<UpdateResponse> SubscribeToUpdatesAsync(UpdateRequest request, CancellationToken cancellationToken = default)");
    }

    [Fact]
    public void ToCSharpSignature_BidirectionalStreamingMethod_GeneratesCorrectSignature()
    {
        // Arrange
        var method = new GrpcMethod
        {
            Name = "ProcessStream",
            FullName = "streaming.StreamService/ProcessStream",
            Type = MethodType.BidirectionalStreaming,
            InputMessageType = "StreamRequest",
            OutputMessageType = "StreamResponse"
        };

        // Act
        var result = method.ToCSharpSignature();

        // Assert
        result.Should().Be("public async IAsyncEnumerable<StreamResponse> ProcessStreamAsync(StreamRequest request, CancellationToken cancellationToken = default)");
    }

    [Fact]
    public void ToCSharpSignature_MethodWithInputParameters_GeneratesCorrectSignature()
    {
        // Arrange
        var method = new GrpcMethod
        {
            Name = "SearchUsers",
            FullName = "users.UserService/SearchUsers",
            Type = MethodType.Unary,
            InputMessageType = "SearchRequest",
            OutputMessageType = "UserCollection"
        };

        method.AddInputParameter(new MethodParameter("query", "string", 1, true) { Description = "Search query" });
        method.AddInputParameter(new MethodParameter("limit", "int", 2, false) { Description = "Maximum results" });

        // Act
        var result = method.ToCSharpSignature();

        // Assert
        result.Should().Be("public async Task<UserCollection> SearchUsersAsync(SearchRequest request, string query, int limit, CancellationToken cancellationToken = default)");
    }

    [Fact]
    public void ToCSharpSignature_MethodWithOutputParameters_GeneratesCorrectSignature()
    {
        // Arrange
        var method = new GrpcMethod
        {
            Name = "GetUserDetails",
            FullName = "users.UserService/GetUserDetails",
            Type = MethodType.Unary,
            InputMessageType = "UserIdRequest",
            OutputMessageType = "UserDetailsResponse"
        };

        method.AddOutputParameter(new MethodParameter("userId", "string", 1) { Description = "The user ID" });
        method.AddOutputParameter(new MethodParameter("isActive", "bool", 2) { Description = "Whether user is active" });

        // Act
        var result = method.ToCSharpSignature();

        // Assert
        result.Should().Be("public async Task<UserDetailsResponse> GetUserDetailsAsync(UserIdRequest request, CancellationToken cancellationToken = default)");
    }

    [Fact]
    public void IsStreaming_UnaryMethod_ReturnsFalse()
    {
        // Arrange
        var method = new GrpcMethod
        {
            Name = "GetUser",
            FullName = "users.UserService/GetUser",
            Type = MethodType.Unary,
            InputMessageType = "UserRequest",
            OutputMessageType = "UserResponse"
        };

        // Act
        var result = method.IsStreaming();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsStreaming_ClientStreamingMethod_ReturnsFalse()
    {
        // Arrange
        var method = new GrpcMethod
        {
            Name = "UploadFile",
            FullName = "files.FileService/UploadFile",
            Type = MethodType.ClientStreaming,
            InputMessageType = "FileChunk",
            OutputMessageType = "UploadResult"
        };

        // Act
        var result = method.IsStreaming();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsStreaming_ServerStreamingMethod_ReturnsTrue()
    {
        // Arrange
        var method = new GrpcMethod
        {
            Name = "SubscribeToUpdates",
            FullName = "updates.UpdateService/SubscribeToUpdates",
            Type = MethodType.ServerStreaming,
            InputMessageType = "UpdateRequest",
            OutputMessageType = "UpdateResponse"
        };

        // Act
        var result = method.IsStreaming();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsStreaming_BidirectionalStreamingMethod_ReturnsTrue()
    {
        // Arrange
        var method = new GrpcMethod
        {
            Name = "ProcessStream",
            FullName = "streaming.StreamService/ProcessStream",
            Type = MethodType.BidirectionalStreaming,
            InputMessageType = "StreamRequest",
            OutputMessageType = "StreamResponse"
        };

        // Act
        var result = method.IsStreaming();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsStreaming_NullMethod_ThrowsArgumentNullException()
    {
        // Arrange
        GrpcMethod? method = null;

        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => method!.IsStreaming());
    }

    [Fact]
    public void GetTotalParameterCount_UnaryMethodWithNoParameters_ReturnsZero()
    {
        // Arrange
        var method = new GrpcMethod
        {
            Name = "GetUser",
            FullName = "users.UserService/GetUser",
            Type = MethodType.Unary,
            InputMessageType = "UserRequest",
            OutputMessageType = "UserResponse"
        };

        // Act
        var result = method.GetTotalParameterCount();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void GetTotalParameterCount_UnaryMethodWithInputParameters_ReturnsInputParameterCount()
    {
        // Arrange
        var method = new GrpcMethod
        {
            Name = "SearchUsers",
            FullName = "users.UserService/SearchUsers",
            Type = MethodType.Unary,
            InputMessageType = "SearchRequest",
            OutputMessageType = "UserCollection"
        };

        method.AddInputParameter(new MethodParameter("query", "string", 1));
        method.AddInputParameter(new MethodParameter("limit", "int", 2));

        // Act
        var result = method.GetTotalParameterCount();

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void GetTotalParameterCount_MethodWithOutputParameters_ReturnsOutputParameterCount()
    {
        // Arrange
        var method = new GrpcMethod
        {
            Name = "GetUserDetails",
            FullName = "users.UserService/GetUserDetails",
            Type = MethodType.Unary,
            InputMessageType = "UserIdRequest",
            OutputMessageType = "UserDetailsResponse"
        };

        method.AddOutputParameter(new MethodParameter("userId", "string", 1));
        method.AddOutputParameter(new MethodParameter("isActive", "bool", 2));

        // Act
        var result = method.GetTotalParameterCount();

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void GetTotalParameterCount_MethodWithBothInputAndOutputParameters_ReturnsSum()
    {
        // Arrange
        var method = new GrpcMethod
        {
            Name = "ComplexMethod",
            FullName = "complex.ComplexService/ComplexMethod",
            Type = MethodType.Unary,
            InputMessageType = "ComplexRequest",
            OutputMessageType = "ComplexResponse"
        };

        method.AddInputParameter(new MethodParameter("param1", "string", 1));
        method.AddInputParameter(new MethodParameter("param2", "int", 2));
        method.AddOutputParameter(new MethodParameter("result1", "bool", 1));
        method.AddOutputParameter(new MethodParameter("result2", "string", 2));
        method.AddOutputParameter(new MethodParameter("result3", "int", 3));

        // Act
        var result = method.GetTotalParameterCount();

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public void GetTotalParameterCount_NullMethod_ThrowsArgumentNullException()
    {
        // Arrange
        GrpcMethod? method = null;

        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => method!.GetTotalParameterCount());
    }

    [Fact]
    public void ToXmlDocumentation_NullMethod_ThrowsArgumentNullException()
    {
        // Arrange
        GrpcMethod? method = null;

        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => method!.ToXmlDocumentation());
    }

    [Fact]
    public void ToXmlDocumentation_MethodWithDescription_GeneratesCorrectXml()
    {
        // Arrange
        var method = new GrpcMethod
        {
            Name = "GetUser",
            FullName = "users.UserService/GetUser",
            Type = MethodType.Unary,
            InputMessageType = "UserRequest",
            OutputMessageType = "UserResponse",
            Description = "Retrieves a user by their unique identifier"
        };

        // Act
        var result = method.ToXmlDocumentation();

        // Assert
        var expected = "/// <summary>\n/// Retrieves a user by their unique identifier\n/// </summary>\n" +
                       "/// <param name=\"request\">UserRequest parameter</param>\n" +
                       "/// <returns>Response message</returns>\n";
        result.Should().Be(expected);
    }

    [Fact]
    public void ToXmlDocumentation_MethodWithoutDescription_GeneratesDefaultXml()
    {
        // Arrange
        var method = new GrpcMethod
        {
            Name = "GetUser",
            FullName = "users.UserService/GetUser",
            Type = MethodType.Unary,
            InputMessageType = "UserRequest",
            OutputMessageType = "UserResponse"
        };

        // Act
        var result = method.ToXmlDocumentation();

        // Assert
        var expected = "/// <summary>\n/// users.UserService/GetUser - Unary method\n/// </summary>\n" +
                       "/// <param name=\"request\">UserRequest parameter</param>\n" +
                       "/// <returns>Response message</returns>\n";
        result.Should().Be(expected);
    }

    [Fact]
    public void ToXmlDocumentation_MethodWithInputParameters_GeneratesXmlWithParameters()
    {
        // Arrange
        var method = new GrpcMethod
        {
            Name = "SearchUsers",
            FullName = "users.UserService/SearchUsers",
            Type = MethodType.Unary,
            InputMessageType = "SearchRequest",
            OutputMessageType = "UserCollection",
            Description = "Searches for users matching the given criteria"
        };

        method.AddInputParameter(new MethodParameter("query", "string", 1, true) { Description = "Search query string" });
        method.AddInputParameter(new MethodParameter("limit", "int", 2, false) { Description = "Maximum number of results to return" });

        // Act
        var result = method.ToXmlDocumentation();

        // Assert
        var expected = "/// <summary>\n/// Searches for users matching the given criteria\n/// </summary>\n" +
                       "/// <param name=\"query\">Search query string</param>\n" +
                       "/// <param name=\"limit\">Maximum number of results to return</param>\n" +
                       "/// <param name=\"request\">SearchRequest parameter</param>\n" +
                       "/// <returns>Response message</returns>\n";
        result.Should().Be(expected);
    }

    [Fact]
    public void ToXmlDocumentation_DeprecatedMethod_GeneratesXmlWithDeprecatedRemark()
    {
        // Arrange
        var method = new GrpcMethod
        {
            Name = "OldMethod",
            FullName = "legacy.LegacyService/OldMethod",
            Type = MethodType.Unary,
            InputMessageType = "LegacyRequest",
            OutputMessageType = "LegacyResponse",
            IsDeprecated = true,
            Description = "This is an old deprecated method"
        };

        // Act
        var result = method.ToXmlDocumentation();

        // Assert
        var expected = "/// <summary>\n/// This is an old deprecated method\n/// </summary>\n" +
                       "/// <param name=\"request\">LegacyRequest parameter</param>\n" +
                       "/// <returns>Response message</returns>\n" +
                       "/// <remarks>This method is deprecated</remarks>\n";
        result.Should().Be(expected);
    }

    [Fact]
    public void ToXmlDocumentation_MethodWithTimeout_GeneratesXmlWithTimeoutRemark()
    {
        // Arrange
        var method = new GrpcMethod
        {
            Name = "GetData",
            FullName = "data.DataService/GetData",
            Type = MethodType.Unary,
            InputMessageType = "DataRequest",
            OutputMessageType = "DataResponse",
            TimeoutMilliseconds = 5000
        };

        // Act
        var result = method.ToXmlDocumentation();

        // Assert
        var expected = "/// <summary>\n/// data.DataService/GetData - Unary method\n/// </summary>\n" +
                       "/// <param name=\"request\">DataRequest parameter</param>\n" +
                       "/// <returns>Response message</returns>\n" +
                       "/// <remarks>Timeout: 5000ms</remarks>\n";
        result.Should().Be(expected);
    }
}
