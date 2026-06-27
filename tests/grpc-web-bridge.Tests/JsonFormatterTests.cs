using Xunit;
using FluentAssertions;
using GrpcWebBridge.Formatters;

namespace GrpcWebBridge.Tests;

public class JsonFormatterTests
{
    private readonly JsonFormatter _formatter;

    public JsonFormatterTests()
    {
        _formatter = new JsonFormatter();
    }

    [Fact]
    public void Format_ShouldReturnValidJson()
    {
        var obj = new { Name = "Test", Value = 1 };
        var json = _formatter.Format(obj);
        // Assuming lowercase keys
        json.Should().Contain("\"name\":\"Test\"").And.Contain("\"value\":1");
    }

    [Fact]
    public void FormatWithSortedKeys_ShouldSortKeys()
    {
        var obj = new { B = 2, A = 1 };
        var json = _formatter.FormatWithSortedKeys(obj);
        
        // Ensure "a" comes before "b"
        json.IndexOf("a").Should().BeLessThan(json.IndexOf("b"));
    }

    [Fact]
    public void Format_WithNullObject_ShouldReturnNullString()
    {
        var json = _formatter.Format<object>(null!);
        json.Should().Be("null");
    }
}
