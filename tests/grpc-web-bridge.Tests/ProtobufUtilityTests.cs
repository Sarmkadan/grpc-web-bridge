using System;
using System.Collections.Generic;
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
        Assert.Contains("\"seconds\":1620000000", json);
        Assert.Contains("\"nanos\":123456789", json);
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
        var timestamp = new Timestamp { Seconds = 10, Nanos = 20 };
        var dict = ProtobufUtility.ToDict(timestamp);
        Assert.True(dict.ContainsKey("seconds"));
        Assert.True(dict.ContainsKey("nanos"));
        Assert.Equal(10L, dict["seconds"]);
        Assert.Equal(20L, dict["nanos"]);
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
