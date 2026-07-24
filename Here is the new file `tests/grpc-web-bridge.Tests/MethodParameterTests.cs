namespace GrpcWebBridge.Tests;

using Xunit;
using GrpcWebBridge.Domain.Models;

public class MethodParameterTests
{
    [Fact]
    public void Constructor_HappyPath()
    {
        var parameter = new MethodParameter("Name", "TypeName", 1);

        Assert.Equal("Name", parameter.Name);
        Assert.Equal("TypeName", parameter.TypeName);
        Assert.Equal(1, parameter.FieldNumber);
        Assert.True(parameter.IsRequired);
    }

    [Fact]
    public void Constructor_NullName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new MethodParameter(null, "TypeName", 1));
    }

    [Fact]
    public void Constructor_EmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new MethodParameter("", "TypeName", 1));
    }

    [Fact]
    public void Constructor_NullTypeName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new MethodParameter("Name", null, 1));
    }

    [Fact]
    public void Constructor_EmptyTypeName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new MethodParameter("Name", "", 1));
    }

    [Fact]
    public void Constructor_FieldNumberLessThanOne_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new MethodParameter("Name", "TypeName", 0));
    }

    [Fact]
    public void Constructor_FieldNumberGreaterThanMaxValue_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new MethodParameter("Name", "TypeName", 536870912));
    }

    [Fact]
    public void Validate_HappyPath()
    {
        var parameter = new MethodParameter("Name", "TypeName", 1);

        parameter.Validate();

        Assert.True(true);
    }

    [Fact]
    public void Validate_NullName_ThrowsArgumentException()
    {
        var parameter = new MethodParameter(null, "TypeName", 1);

        Assert.Throws<ArgumentException>(() => parameter.Validate());
    }

    [Fact]
    public void Validate_EmptyName_ThrowsArgumentException()
    {
        var parameter = new MethodParameter("", "TypeName", 1);

        Assert.Throws<ArgumentException>(() => parameter.Validate());
    }

    [Fact]
    public void Validate_NullTypeName_ThrowsArgumentException()
    {
        var parameter = new MethodParameter("Name", null, 1);

        Assert.Throws<ArgumentException>(() => parameter.Validate());
    }

    [Fact]
    public void Validate_EmptyTypeName_ThrowsArgumentException()
    {
        var parameter = new MethodParameter("Name", "", 1);

        Assert.Throws<ArgumentException>(() => parameter.Validate());
    }

    [Fact]
    public void Validate_FieldNumberLessThanOne_ThrowsArgumentException()
    {
        var parameter = new MethodParameter("Name", "TypeName", 0);

        Assert.Throws<ArgumentException>(() => parameter.Validate());
    }

    [Fact]
    public void Validate_FieldNumberGreaterThanMaxValue_ThrowsArgumentException()
    {
        var parameter = new MethodParameter("Name", "TypeName", 536870912);

        Assert.Throws<ArgumentException>(() => parameter.Validate());
    }
}
