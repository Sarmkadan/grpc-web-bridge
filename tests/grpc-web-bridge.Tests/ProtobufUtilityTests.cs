using System;
using System.Collections.Generic;
using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using GrpcWebBridge.Utilities;
using Xunit;

namespace GrpcWebBridge.Tests;

public class ProtobufUtilityTests
{
    [Fact]
    public void ToJson_ShouldReturnValidJson()
    {
        var timestamp = new Timestamp { Seconds = 1620000000, Nanos = 123456789 };
        var json = ProtobufUtility.ToJson(timestamp);
        // Timestamp uses the RFC 3339 string form in the protobuf JSON mapping
        Assert.Equal("\"2021-05-03T00:00:00.123456789Z\"", json);
    }

    [Fact]
    public void FromJson_ShouldParseBackToEquivalentMessage()
    {
        var original = new Timestamp { Seconds = 1620000000, Nanos = 123456789 };
        var json = ProtobufUtility.ToJson(original);
        var parsed = ProtobufUtility.FromJson<Timestamp>(json);
        Assert.NotNull(parsed);
        Assert.True(ProtobufUtility.AreEqual(original, parsed));
    }

    [Fact]
    public void ToBytes_And_FromBytes_RoundTrip()
    {
        var original = new Timestamp { Seconds = 1620000000, Nanos = 123456789 };
        var bytes = ProtobufUtility.ToBytes(original);
        var parsed = ProtobufUtility.FromBytes<Timestamp>(bytes);
        Assert.NotNull(parsed);
        Assert.True(ProtobufUtility.AreEqual(original, parsed));
    }

    [Fact]
    public void GetMessageSize_ShouldMatchCalculateSize()
    {
        var message = new Timestamp { Seconds = 1, Nanos = 2 };
        var sizeFromUtility = ProtobufUtility.GetMessageSize(message);
        var sizeDirect = message.CalculateSize();
        Assert.Equal(sizeDirect, sizeFromUtility);
    }

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

    [Fact]
    public void Clone_ShouldCreateEqualButDistinctInstance()
    {
        var original = new Timestamp { Seconds = 5, Nanos = 6 };
        var clone = ProtobufUtility.Clone(original);
        Assert.True(ProtobufUtility.AreEqual(original, clone));
        Assert.NotSame(original, clone);
    }

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

    [Fact]
    public void AreEqual_ShouldReturnTrueForEqualMessages()
    {
        var a = new Timestamp { Seconds = 100, Nanos = 200 };
        var b = new Timestamp { Seconds = 100, Nanos = 200 };
        Assert.True(ProtobufUtility.AreEqual(a, b));
    }

    [Fact]
    public void Validate_ShouldReturnValidForWellFormedMessage()
    {
        var message = new Timestamp { Seconds = 1, Nanos = 0 };
        var (valid, errors) = ProtobufUtility.Validate(message);
        Assert.True(valid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ToJson_NullMessage_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ProtobufUtility.ToJson(null!));
    }

    [Fact]
    public void FromJson_NullOrEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ProtobufUtility.FromJson<Timestamp>(null!));
        Assert.Throws<ArgumentException>(() => ProtobufUtility.FromJson<Timestamp>(string.Empty));
    }

    [Fact]
    public void FromBytes_NullOrEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ProtobufUtility.FromBytes<Timestamp>(null!));
        Assert.Throws<ArgumentException>(() => ProtobufUtility.FromBytes<Timestamp>(Array.Empty<byte>()));
    }
}
