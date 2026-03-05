#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using FluentAssertions;
using GrpcWebBridge.Utilities;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Provides unit tests for the <see cref="JsonUtility"/> class, which offers JSON serialization,
/// deserialization, merging, and validation utilities for gRPC web bridge operations.
/// </summary>
public sealed class JsonUtilityTests
{
    // ─────────────────────────────────────────────────────────────────────
    // Serialize
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tests that <see cref="JsonUtility.Serialize"/> converts an anonymous object to camelCase JSON string.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="JsonUtility.Serialize"/> returns "null" literal when serializing a null object.
    /// </summary>
    [Fact]
    public void Serialize_WithNullObject_ReturnsNullLiteral()
    {
        string? obj = null;
        var json = JsonUtility.Serialize(obj);
        json.Should().Be("null");
    }

    /// <summary>
    /// Tests that <see cref="JsonUtility.Serialize"/> with indented parameter returns formatted JSON with newlines.
    /// </summary>
    [Fact]
    public void Serialize_WithIndented_ReturnsFormattedJson()
    {
        var obj = new { Value = 42 };
        var json = JsonUtility.Serialize(obj, indented: true);
        json.Should().Contain("\n");
    }

    /// <summary>
    /// Tests that <see cref="JsonUtility.Serialize"/> omits null properties from the resulting JSON string.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="JsonUtility.Deserialize{T}"/> successfully deserializes valid JSON into the specified type.
    /// </summary>
    [Fact]
    public void Deserialize_WithValidJson_ReturnsMappedObject()
    {
        var json = "{\"name\":\"Alice\",\"score\":99}";

        var result = JsonUtility.Deserialize<TestPayload>(json);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Alice");
        result.Score.Should().Be(99);
    }

    /// <summary>
    /// Tests that <see cref="JsonUtility.Deserialize{T}"/> returns null when deserializing whitespace or empty string.
    /// </summary>
    [Fact]
    public void Deserialize_WithNullWhitespace_ReturnsDefault()
    {
        var result = JsonUtility.Deserialize<TestPayload>(" ");
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that <see cref="JsonUtility.Deserialize{T}"/> throws <see cref="InvalidOperationException"/> when provided with invalid JSON.
    /// </summary>
    [Fact]
    public void Deserialize_WithInvalidJson_ThrowsInvalidOperationException()
    {
        var act = () => JsonUtility.Deserialize<TestPayload>("{not-valid-json}");
        act.Should().Throw<InvalidOperationException>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // TryDeserialize
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tests that <see cref="JsonUtility.TryDeserialize{T}"/> successfully deserializes valid JSON and returns true with the result.
    /// </summary>
    [Fact]
    public void TryDeserialize_WithValidJson_ReturnsTrueAndResult()
    {
        var success = JsonUtility.TryDeserialize<TestPayload>(
            "{\"name\":\"Bob\",\"score\":50}", out var result, out var error);

        success.Should().BeTrue();
        error.Should().BeNull();
        result!.Name.Should().Be("Bob");
    }

    /// <summary>
    /// Tests that <see cref="JsonUtility.TryDeserialize{T}"/> returns false with an error message when provided with invalid JSON.
    /// </summary>
    [Fact]
    public void TryDeserialize_WithInvalidJson_ReturnsFalseWithError()
    {
        var success = JsonUtility.TryDeserialize<TestPayload>(
            "{{invalid}}", out var result, out var error);

        success.Should().BeFalse();
        result.Should().BeNull();
        error.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Tests that <see cref="JsonUtility.TryDeserialize{T}"/> returns false when provided with an empty string.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="JsonUtility.MergeJson"/> merges two JSON strings, with source properties overriding target properties.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="JsonUtility.MergeJson"/> returns the target JSON unchanged when the source is an empty object.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="JsonUtility.GetPropertyValue"/> retrieves a property value from JSON using a simple property path.
    /// </summary>
    [Fact]
    public void GetPropertyValue_WithSimplePath_ReturnsValue()
    {
        var json = "{\"name\":\"test\"}";
        var value = JsonUtility.GetPropertyValue(json, "name");

        value.Should().NotBeNull();
        value!.ToString().Should().Contain("test");
    }

    /// <summary>
    /// Tests that <see cref="JsonUtility.GetPropertyValue"/> returns null when the specified property key does not exist in the JSON.
    /// </summary>
    [Fact]
    public void GetPropertyValue_WithMissingKey_ReturnsNull()
    {
        var result = JsonUtility.GetPropertyValue("{\"a\":1}", "missing");
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that <see cref="JsonUtility.GetPropertyValue"/> retrieves a nested property value from JSON using a dot-separated path.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="JsonUtility.ValidateRequired"/> returns true when all specified required properties are present in the JSON.
    /// </summary>
    [Fact]
    public void ValidateRequired_WithAllRequiredPresent_ReturnsTrue()
    {
        var json = "{\"name\":\"Alice\",\"email\":\"alice@example.com\"}";
        var valid = JsonUtility.ValidateRequired(json, "name", "email");
        valid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that <see cref="JsonUtility.ValidateRequired"/> returns false when any of the specified required properties are missing from the JSON.
    /// </summary>
    [Fact]
    public void ValidateRequired_WithMissingRequiredProperty_ReturnsFalse()
    {
        var json = "{\"name\":\"Alice\"}";
        var valid = JsonUtility.ValidateRequired(json, "name", "email");
        valid.Should().BeFalse();
    }

    /// <summary>
    /// Tests that <see cref="JsonUtility.ValidateRequired"/> returns false when the JSON string is null, empty, or whitespace.
    /// </summary>
    [Fact]
    public void ValidateRequired_WithNullOrEmptyJson_ReturnsFalse()
    {
        JsonUtility.ValidateRequired(null!, "name").Should().BeFalse();
        JsonUtility.ValidateRequired(string.Empty, "name").Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────
    // DeserializeToDictionary
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tests that <see cref="JsonUtility.DeserializeToDictionary"/> successfully deserializes valid JSON into a dictionary.
    /// </summary>
    [Fact]
    public void DeserializeToDictionary_WithValidJson_ReturnsDict()
    {
        var json = "{\"x\":1,\"y\":2}";
        var dict = JsonUtility.DeserializeToDictionary(json);

        dict.Should().NotBeNull();
        dict!.Should().ContainKey("x");
        dict.Should().ContainKey("y");
    }

    /// <summary>
    /// Tests that <see cref="JsonUtility.DeserializeToDictionary"/> returns null when deserializing whitespace or empty JSON.
    /// </summary>
    [Fact]
    public void DeserializeToDictionary_WithEmptyJson_ReturnsNull()
    {
        var dict = JsonUtility.DeserializeToDictionary(" ");
        dict.Should().BeNull();
    }

    private sealed record TestPayload(string Name, int Score);
}