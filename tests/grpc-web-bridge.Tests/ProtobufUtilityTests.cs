using Xunit;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Grpc.WebBridge;
using Grpc.WebBridge.Utilities;

namespace GrpcWebBridge.Tests
{
    public class ProtobufUtilityTests
    {
        [Fact]
        public void ToJson_Happy_PATH()
        {
            // Arrange
            var message = new MyMessage();
            message.MyField = "Hello World!";

            // Act
            var json = ProtobufUtility.ToJson(message);

            // Assert
            Assert.NotEmpty(json);
            Assert.Contains("Hello World!", json);
        }

        [Fact]
        public void ToJson_NULL_INPUT()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => ProtobufUtility.ToJson(null));
        }

        [Fact]
        public void FromJson_HAPPY_PATH()
        {
            // Arrange
            var json = "{\"MyField\": \"Hello World!\"}";

            // Act
            var message = ProtobufUtility.FromJson<MyMessage>(json);

            // Assert
            Assert.NotNull(message);
            Assert.Equal("Hello World!", message.MyField);
        }

        [Fact]
        public void FromJson_NULL_INPUT()
        {
            // Act and Assert
            Assert.Throws<ArgumentException>(() => ProtobufUtility.FromJson<MyMessage>(null));
        }

        [Fact]
        public void ToBytes_HAPPY_PATH()
        {
            // Arrange
            var message = new MyMessage();
            message.MyField = "Hello World!";

            // Act
            var bytes = ProtobufUtility.ToBytes(message);

            // Assert
            Assert.NotEmpty(bytes);
        }

        [Fact]
        public void ToBytes_NULL_INPUT()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => ProtobufUtility.ToBytes(null));
        }

        [Fact]
        public void FromBytes_HAPPY_PATH()
        {
            // Arrange
            var message = new MyMessage();
            message.MyField = "Hello World!";
            var bytes = ProtobufUtility.ToBytes(message);

            // Act
            var deserializedMessage = ProtobufUtility.FromBytes<MyMessage>(bytes);

            // Assert
            Assert.NotNull(deserializedMessage);
            Assert.Equal("Hello World!", deserializedMessage.MyField);
        }

        [Fact]
        public void FromBytes_NULL_INPUT()
        {
            // Act and Assert
            Assert.Throws<ArgumentException>(() => ProtobufUtility.FromBytes<MyMessage>(null));
        }

        [Fact]
        public void GetMessageSize_HAPPY_PATH()
        {
            // Arrange
            var message = new MyMessage();
            message.MyField = "Hello World!";

            // Act
            var size = ProtobufUtility.GetMessageSize(message);

            // Assert
            Assert.True(size > 0);
        }

        [Fact]
        public void GetMessageSize_NULL_INPUT()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => ProtobufUtility.GetMessageSize(null));
        }

        [Fact]
        public void ToDict_HAPPY_PATH()
        {
            // Arrange
            var message = new MyMessage();
            message.MyField = "Hello World!";

            // Act
            var dict = ProtobufUtility.ToDict(message);

            // Assert
            Assert.NotNull(dict);
            Assert.Contains("MyField", dict.Keys);
            Assert.Equal("Hello World!", dict["MyField"]);        }

        [Fact]
        public void ToDict_NULL_INPUT()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => ProtobufUtility.ToDict(null));
        }

        [Fact]
        public void Clone_HAPPY_PATH()
        {
            // Arrange
            var message = new MyMessage();
            message.MyField = "Hello World!";

            // Act
            var clone = ProtobufUtility.Clone(message);

            // Assert
            Assert.NotNull(clone);
            Assert.Equal("Hello World!", clone.MyField);
        }

        [Fact]
        public void Clone_NULL_INPUT()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => ProtobufUtility.Clone(null));
        }

        [Fact]
        public void Merge_HAPPY_PATH()
        {
            // Arrange
            var message1 = new MyMessage();
            message1.MyField = "Hello World!";
            var message2 = new MyMessage();
            message2.MyField = "Hello World Again!";

            // Act
            var mergedMessage = ProtobufUtility.Merge(message1, message2);

            // Assert
            Assert.NotNull(mergedMessage);
            Assert.Equal("Hello World Again!", mergedMessage.MyField);
        }

        [Fact]
        public void Merge_NULL_INPUT()
        {
            // Act and Assert
            Assert.Throws<ArgumentException>(() => ProtobufUtility.Merge(null));
        }

        [Fact]
        public void AreEqual_HAPPY_PATH()
        {
            // Arrange
            var message1 = new MyMessage();
            message1.MyField = "Hello World!";
            var message2 = new MyMessage();
            message2.MyField = "Hello World!";

            // Act and Assert
            Assert.True(ProtobufUtility.AreEqual(message1, message2));
        }

        [Fact]
        public void AreEqual_NULL_INPUT()
        {
            // Act and Assert
            Assert.False(ProtobufUtility.AreEqual(null, null));
        }

        [Fact]
        public void Validate_HAPPY_PATH()
        {
            // Arrange
            var message = new MyMessage();
            message.MyField = "Hello World!";

            // Act
            var (valid, errors) = ProtobufUtility.Validate(message);

            // Assert
            Assert.True(valid);
            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_NULL_INPUT()
        {
            // Act and Assert
            var (valid, errors) = ProtobufUtility.Validate(null);
            Assert.False(valid);
            Assert.Contains("Message cannot be null", errors);
        }
    }
}