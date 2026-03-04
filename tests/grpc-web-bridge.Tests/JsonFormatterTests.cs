using Xunit;
using FluentAssertions;
using GrpcWebBridge.Formatters;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Tests for the JsonFormatter class.
/// </summary>
public class JsonFormatterTests
{
    private readonly JsonFormatter _formatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonFormatterTests"/> class.
    /// </summary>
    public JsonFormatterTests()
    {
        _formatter = new JsonFormatter();
    }

    /// <summary>
    /// Verifies that the Format method returns a valid JSON string.
    /// </summary>
    [Fact]
    public void Format_ShouldReturnValidJson()
    {
        var obj = new { Name = "Test", Value = 1 };
        var json = _formatter.Format(obj);
        // Assuming lowercase keys
        json.Should().Contain("\"name\":\"Test\"").And.Contain("\"value\":1");
    }

    /// <summary>
    /// Verifies that the FormatWithSortedKeys method sorts the keys in the JSON string.
    /// </summary>
    [Fact]
    public void FormatWithSortedKeys_ShouldSortKeys()
    {
        var obj = new { B = 2, A = 1 };
        var json = _formatter.FormatWithSortedKeys(obj);
        
        // Ensure "a" comes before "b"
        json.IndexOf("a").Should().BeLessThan(json.IndexOf("b"));
    }

    /// <summary>
    /// Verifies that the Format method returns a null string when given a null object.
    /// </summary>
    [Fact]
    public void Format_WithNullObject_ShouldReturnNullString()
    {
        var json = _formatter.Format<object>(null!);
        json.Should().Be("null");
    }
}
