using System;
using System.Collections.Generic;
using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using GrpcWebBridge.Utilities;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Tests for <see cref="ProtobufUtility"/> covering JSON and binary serialization, size, dictionary conversion, cloning, merging, equality and validation.
/// </summary>
public class ProtobufUtilityTests
{
    /// <summary>
    /// Verifies that <see cref="ProtobufUtility.ToJson"/> renders a Timestamp in its RFC 3339 string form.
    /// </summary>
    [Fact]
    public void ToJson_ShouldReturnValidJson()
    {
        var timestamp = new Timestamp { Seconds = 1620000000, Nanos = 123456789 };
        var json = ProtobufUtility.ToJson(timestamp);
        // Timestamp uses the RFC 3339 string form in the protobuf JSON mapping
        Assert.Equal("\"2021-05-03T00:00:00.123456789Z\"", json);
    }

    /// <summary>
    /// Verifies that a message serialized with <see cref="ProtobufUtility.ToJson"/> parses back to an equal message via <see cref="ProtobufUtility.FromJson{T}"/>.
    /// </summary>
    [Fact]
    public void FromJson_ShouldParseBackToEquivalentMessage()
    {
        var original = new Timestamp { Seconds = 1620000000, Nanos = 123456789 };
        var json = ProtobufUtility.ToJson(original);
        var parsed = ProtobufUtility.FromJson<Timestamp>(json);
        Assert.NotNull(parsed);
        Assert.True(ProtobufUtility.AreEqual(original, parsed));
    }

    /// <summary>
    /// Verifies that a message survives a binary round trip through <see cref="ProtobufUtility.ToBytes"/> and <see cref="ProtobufUtility.FromBytes{T}"/>.
    /// </summary>
    [Fact]
    public void ToBytes_And_FromBytes_RoundTrip()
    {
        var original = new Timestamp { Seconds = 1620000000, Nanos = 123456789 };
        var bytes = ProtobufUtility.ToBytes(original);
        var parsed = ProtobufUtility.FromBytes<Timestamp>(bytes);
        Assert.NotNull(parsed);
        Assert.True(ProtobufUtility.AreEqual(original, parsed));
    }

    /// <summary>
    /// Verifies that <see cref="ProtobufUtility.GetMessageSize"/> returns the same value as the message's own <c>CalculateSize</c>.
    /// </summary>
    [Fact]
    public void GetMessageSize_ShouldMatchCalculateSize()
    {
        var message = new Timestamp { Seconds = 1, Nanos = 2 };
        var sizeFromUtility = ProtobufUtility.GetMessageSize(message);
        var sizeDirect = message.CalculateSize();
        Assert.Equal(sizeDirect, sizeFromUtility);
    }

    /// <summary>
    /// Verifies that <see cref="ProtobufUtility.ToDict"/> exposes every field of a Struct as a dictionary entry with its numeric value.
    /// </summary>
    [Fact]
    public void ToDict_ShouldContainAllFields()
    {
        // Struct maps to a plain JSON object, so its fields become dictionary entries
        var message = new Struct { Fields = { ["seconds"] = Value.ForNumber(10), ["nanos"] = Value.ForNumber(20) } };
        var dict = ProtobufUtility.ToDict(message);
        Assert.True(dict.ContainsKey("seconds"));
        Assert.True(dict.ContainsKey("nanos"));
        Assert.Equal(10, ((JsonElement)dict["seconds"]!).GetInt32());
        Assert.Equal(20, ((JsonElement)dict["nanos"]!).GetInt32());
    }

    /// <summary>
    /// Verifies that <see cref="ProtobufUtility.Clone"/> returns a message equal to the original but a distinct instance.
    /// </summary>
    [Fact]
    public void Clone_ShouldCreateEqualButDistinctInstance()
    {
        var original = new Timestamp { Seconds = 5, Nanos = 6 };
        var clone = ProtobufUtility.Clone(original);
        Assert.True(ProtobufUtility.AreEqual(original, clone));
        Assert.NotSame(original, clone);
    }

    /// <summary>
    /// Verifies that <see cref="ProtobufUtility.Merge"/> combines the fields of two messages into one result.
    /// </summary>
    [Fact]
    public void Merge_ShouldCombineFieldsFromMultipleMessages()
    {
        var first = new Struct();
        first.Fields.Add("first", Value.ForString("one"));
        var second = new Struct();
        second.Fields.Add("second", Value.ForNumber(2));

        var merged = ProtobufUtility.Merge(first, second);
        Assert.True(merged.Fields.ContainsKey("first"));
        Assert.True(merged.Fields.ContainsKey("second"));
        Assert.Equal("one", merged.Fields["first"].StringValue);
        Assert.Equal(2, merged.Fields["second"].NumberValue);
    }

    /// <summary>
    /// Verifies that <see cref="ProtobufUtility.AreEqual"/> reports true for two messages with identical field values.
    /// </summary>
    [Fact]
    public void AreEqual_ShouldReturnTrueForEqualMessages()
    {
        var a = new Timestamp { Seconds = 100, Nanos = 200 };
        var b = new Timestamp { Seconds = 100, Nanos = 200 };
        Assert.True(ProtobufUtility.AreEqual(a, b));
    }

    /// <summary>
    /// Verifies that <see cref="ProtobufUtility.Validate"/> reports a well-formed message as valid with no errors.
    /// </summary>
    [Fact]
    public void Validate_ShouldReturnValidForWellFormedMessage()
    {
        var message = new Timestamp { Seconds = 1, Nanos = 0 };
        var (valid, errors) = ProtobufUtility.Validate(message);
        Assert.True(valid);
        Assert.Empty(errors);
    }

    /// <summary>
    /// Verifies that <see cref="ProtobufUtility.ToJson"/> throws <see cref="ArgumentNullException"/> when the message is null.
    /// </summary>
    [Fact]
    public void ToJson_NullMessage_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ProtobufUtility.ToJson(null!));
    }

    /// <summary>
    /// Verifies that <see cref="ProtobufUtility.FromJson{T}"/> throws <see cref="ArgumentException"/> for null or empty input.
    /// </summary>
    [Fact]
    public void FromJson_NullOrEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ProtobufUtility.FromJson<Timestamp>(null!));
        Assert.Throws<ArgumentException>(() => ProtobufUtility.FromJson<Timestamp>(string.Empty));
    }

    /// <summary>
    /// Verifies that <see cref="ProtobufUtility.FromBytes{T}"/> throws <see cref="ArgumentException"/> for null or empty input.
    /// </summary>
    [Fact]
    public void FromBytes_NullOrEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ProtobufUtility.FromBytes<Timestamp>(null!));
        Assert.Throws<ArgumentException>(() => ProtobufUtility.FromBytes<Timestamp>(Array.Empty<byte>()));
    }
}
