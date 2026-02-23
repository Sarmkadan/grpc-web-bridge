using Xunit;
using FluentAssertions;
using GrpcWebBridge.Domain.Exceptions;

public class ValidationExceptionExtensionsTests
{
    [Fact]
    public void ToErrorDetails_NullException_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => ValidationExceptionExtensions.ToErrorDetails(null));
    }

    [Fact]
    public void IsForField_NullException_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => ValidationExceptionExtensions.IsForField(null, "field"));
    }
}
