using Xunit;
using FluentAssertions;
using GrpcWebBridge.Domain.Exceptions;

/// <summary>
/// Unit tests for <see cref="ValidationExceptionExtensions"/>.
/// </summary>
public class ValidationExceptionExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="ValidationExceptionExtensions.ToErrorDetails(ValidationException)"/> throws an <see cref="ArgumentNullException"/> when the exception argument is null.
    /// </summary>
    [Fact]
    public void ToErrorDetails_NullException_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => ValidationExceptionExtensions.ToErrorDetails(null));
    }

    /// <summary>
    /// Verifies that <see cref="ValidationExceptionExtensions.IsForField(ValidationException, string)"/> throws an <see cref="ArgumentNullException"/> when the exception argument is null.
    /// </summary>
    [Fact]
    public void IsForField_NullException_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => ValidationExceptionExtensions.IsForField(null, "field"));
    }
}
