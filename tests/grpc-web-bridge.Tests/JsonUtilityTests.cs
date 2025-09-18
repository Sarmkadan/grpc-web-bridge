#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Utilities;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class JsonUtilityTests
{
    // ─────────────────────────────────────────────────────────────────────
    // Serialize
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Serialize_WithSimpleObject_ReturnsCamelCaseJson()
    {
        var obj = new { FirstName = "John", LastName = "Doe", Age = 30 };

        var json = JsonUtility.Serialize(obj);

        json.Should().Contain("\"firstName\"");
        json.Should().Contain("\"lastName\"");
        json.Should().Contain("\"age\"");
        json.Should().Contain("\"John\"");
    }

    [Fact]
    public void Serialize_WithNullObject_ReturnsNullLiteral()
    {
        string? obj = null;
        var json = JsonUtility.Serialize(obj);
        json.Should().Be("null");
    }

    [Fact]
    public void Serialize_WithIndented_ReturnsFormattedJson()
    {
        var obj = new { Value = 42 };
        var json = JsonUtility.Serialize(obj, indented: true);
        json.Should().Contain("\n");
    }

    [Fact]
    public void Serialize_WithNullProperty_OmitsNullProperty()
    {
        var obj = new { Name = "Alice", Email = (string?)null };
        var json = JsonUtility.Serialize(obj);
        json.Should().NotContain("\"email\"");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Deserialize
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Deserialize_WithValidJson_ReturnsMappedObject()
    {
        var json = "{\"name\":\"Alice\",\"score\":99}";

        var result = JsonUtility.Deserialize<TestPayload>(json);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Alice");
        result.Score.Should().Be(99);
    }

    [Fact]
    public void Deserialize_WithNullWhitespace_ReturnsDefault()
    {
        var result = JsonUtility.Deserialize<TestPayload>(" ");
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_WithInvalidJson_ThrowsInvalidOperationException()
    {
        var act = () => JsonUtility.Deserialize<TestPayload>("{not-valid-json}");
        act.Should().Throw<InvalidOperationException>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // TryDeserialize
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void TryDeserialize_WithValidJson_ReturnsTrueAndResult()
    {
        var success = JsonUtility.TryDeserialize<TestPayload>(
            "{\"name\":\"Bob\",\"score\":50}", out var result, out var error);

        success.Should().BeTrue();
        error.Should().BeNull();
        result!.Name.Should().Be("Bob");
    }

    [Fact]
    public void TryDeserialize_WithInvalidJson_ReturnsFalseWithError()
    {
        var success = JsonUtility.TryDeserialize<TestPayload>(
            "{{invalid}}", out var result, out var error);

        success.Should().BeFalse();
        result.Should().BeNull();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TryDeserialize_WithEmptyString_ReturnsFalse()
    {
        var success = JsonUtility.TryDeserialize<TestPayload>(
            string.Empty, out _, out var error);

        success.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    // ─────────────────────────────────────────────────────────────────────
    // MergeJson
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void MergeJson_SourceOverridesTargetProperty()
    {
        var target = "{\"a\":1,\"b\":2}";
        var source = "{\"b\":99,\"c\":3}";

        var merged = JsonUtility.MergeJson(target, source);
        var dict = JsonUtility.DeserializeToDictionary(merged);

        dict.Should().NotBeNull();
        dict!["b"].ToString().Should().Be("99");
        dict.Should().ContainKey("a");
        dict.Should().ContainKey("c");
    }

    [Fact]
    public void MergeJson_WithEmptySource_ReturnsTarget()
    {
        var target = "{\"key\":\"value\"}";
        var merged = JsonUtility.MergeJson(target, "{}");

        merged.Should().Contain("\"key\"");
        merged.Should().Contain("\"value\"");
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetPropertyValue
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetPropertyValue_WithSimplePath_ReturnsValue()
    {
        var json = "{\"name\":\"test\"}";
        var value = JsonUtility.GetPropertyValue(json, "name");

        value.Should().NotBeNull();
        value!.ToString().Should().Contain("test");
    }

    [Fact]
    public void GetPropertyValue_WithMissingKey_ReturnsNull()
    {
        var result = JsonUtility.GetPropertyValue("{\"a\":1}", "missing");
        result.Should().BeNull();
    }

    [Fact]
    public void GetPropertyValue_WithNestedPath_ReturnsNestedValue()
    {
        var json = "{\"outer\":{\"inner\":\"deep-value\"}}";
        var value = JsonUtility.GetPropertyValue(json, "outer.inner");

        value.Should().NotBeNull();
        value!.ToString().Should().Contain("deep-value");
    }

    // ─────────────────────────────────────────────────────────────────────
    // ValidateRequired
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateRequired_WithAllRequiredPresent_ReturnsTrue()
    {
        var json = "{\"name\":\"Alice\",\"email\":\"alice@example.com\"}";
        var valid = JsonUtility.ValidateRequired(json, "name", "email");
        valid.Should().BeTrue();
    }

    [Fact]
    public void ValidateRequired_WithMissingRequiredProperty_ReturnsFalse()
    {
        var json = "{\"name\":\"Alice\"}";
        var valid = JsonUtility.ValidateRequired(json, "name", "email");
        valid.Should().BeFalse();
    }

    [Fact]
    public void ValidateRequired_WithNullOrEmptyJson_ReturnsFalse()
    {
        JsonUtility.ValidateRequired(null!, "name").Should().BeFalse();
        JsonUtility.ValidateRequired(string.Empty, "name").Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────
    // DeserializeToDictionary
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void DeserializeToDictionary_WithValidJson_ReturnsDict()
    {
        var json = "{\"x\":1,\"y\":2}";
        var dict = JsonUtility.DeserializeToDictionary(json);

        dict.Should().NotBeNull();
        dict!.Should().ContainKey("x");
        dict.Should().ContainKey("y");
    }

    [Fact]
    public void DeserializeToDictionary_WithEmptyJson_ReturnsNull()
    {
        var dict = JsonUtility.DeserializeToDictionary("  ");
        dict.Should().BeNull();
    }

    private sealed record TestPayload(string Name, int Score);
}
