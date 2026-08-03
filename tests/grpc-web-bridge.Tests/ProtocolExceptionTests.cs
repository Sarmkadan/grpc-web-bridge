using System;
using Xunit;
using GrpcWebBridge.Domain.Exceptions;

namespace GrpcWebBridge.Tests
{
    public class ProtocolExceptionTests
    {
        [Fact]
        public void Constructor_Happy_PATH()
        {
            // Arrange
            var exception = new ProtocolException();
            // Assert
            Assert.NotNull(exception);
        }

        [Fact]
        public void Constructor_Message_Happy_PATH()
        {
            // Arrange
            var exception = new ProtocolException("Test message");
            // Assert
            Assert.NotNull(exception);
            Assert.Equal("PROTOCOL_ERROR", exception.ErrorCode);
        }

        [Fact]
        public void Constructor_Message_WithInnerException_Happy_PATH()
        {
            // Arrange
            var innerException = new Exception("Inner exception");
            var exception = new ProtocolException("Test message", innerException);
            // Assert
            Assert.NotNull(exception);
            Assert.Equal("PROTOCOL_ERROR", exception.ErrorCode);
            Assert.Same(innerException, exception.InnerException);
        }

        [Fact]
        public void Constructor_SourceFormat_TargetFormat_Message_Happy_PATH()
        {
            // Arrange
            var exception = new ProtocolException("source", "target", "Test message");
            // Assert
            Assert.NotNull(exception);
            Assert.Equal("TRANSLATION_FAILED", exception.ErrorCode);
            Assert.Equal("source", exception.SourceFormat);
            Assert.Equal("target", exception.TargetFormat);
        }

        [Fact]
        public void SourceFormat_Setter_Happy_PATH()
        {
            // Arrange
            var exception = new ProtocolException();
            exception.SourceFormat = "Test source format";
            // Assert
            Assert.Equal("Test source format", exception.SourceFormat);
        }

        [Fact]
        public void TargetFormat_Setter_Happy_PATH()
        {
            // Arrange
            var exception = new ProtocolException();
            exception.TargetFormat = "Test target format";
            // Assert
            Assert.Equal("Test target format", exception.TargetFormat);
        }

        [Fact]
        public void RequestId_Setter_Happy_PATH()
        {
            // Arrange
            var exception = new ProtocolException();
            exception.RequestId = "Test request id";
            // Assert
            Assert.Equal("Test request id", exception.RequestId);
        }

        [Fact]
        public void ToString_Happy_PATH()
        {
            // Arrange
            var exception = new ProtocolException("source", "target", "Test message");
            // Act
            var result = exception.ToString();
            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void ToString_Null_SourceFormat_Happy_PATH()
        {
            // Arrange
            var exception = new ProtocolException(null, "target", "Test message");
            // Act
            var result = exception.ToString();
            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void ToString_Null_TargetFormat_Happy_PATH()
        {
            // Arrange
            var exception = new ProtocolException("source", null, "Test message");
            // Act
            var result = exception.ToString();
            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void ToString_Null_RequestId_Happy_PATH()
        {
            // Arrange
            var exception = new ProtocolException("source", "target", null);
            // Act
            var result = exception.ToString();
            // Assert
            Assert.NotNull(result);
        }
    }
}
