using GrpcWebBridge.Services;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace GrpcWebBridge.Tests
{
    public class RegistrySnapshotTests
    {
        [Fact]
        public void TotalServiceCount_HappyPath()
        {
            // Arrange
            var snapshot = new RegistrySnapshot();
            int expected = 42;

            // Act
            snapshot.TotalServiceCount = expected;
            int actual = snapshot.TotalServiceCount;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TotalServiceCount_ZeroAndNegative()
        {
            // Arrange
            var snapshot = new RegistrySnapshot();

            // Act & Assert
            snapshot.TotalServiceCount = 0;
            Assert.Equal(0, snapshot.TotalServiceCount);

            snapshot.TotalServiceCount = -1;
            Assert.Equal(-1, snapshot.TotalServiceCount);
        }

        [Fact]
        public void ServiceRegistrationTimestamps_HappyPath()
        {
            // Arrange
            var snapshot = new RegistrySnapshot();
            var expected = new Dictionary<string, DateTime>
            {
                { "ServiceA", new DateTime(2023, 1, 1) },
                { "ServiceB", new DateTime(2023, 12, 31) }
            };

            // Act
            snapshot.ServiceRegistrationTimestamps = expected;
            var actual = snapshot.ServiceRegistrationTimestamps;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ServiceRegistrationTimestamps_NullAndEmpty()
        {
            // Arrange
            var snapshot = new RegistrySnapshot();

            // Act & Assert for null
            snapshot.ServiceRegistrationTimestamps = null;
            Assert.Null(snapshot.ServiceRegistrationTimestamps);

            // Act & Assert for empty
            snapshot.ServiceRegistrationTimestamps = new Dictionary<string, DateTime>();
            Assert.Empty(snapshot.ServiceRegistrationTimestamps);
        }

        [Fact]
        public void ToJson_HappyPath()
        {
            // Arrange
            var snapshot = new RegistrySnapshot
            {
                TotalServiceCount = 2,
                ServiceRegistrationTimestamps = new Dictionary<string, DateTime>
                {
                    { "ServiceA", new DateTime(2023, 1, 1) },
                    { "ServiceB", new DateTime(2023, 12, 31) }
                }
            };

            // Act
            string json = snapshot.ToJson();

            // Assert
            Assert.NotNull(json);
            Assert.Contains("\"TotalServiceCount\": 2", json);
            Assert.Contains("\"ServiceRegistrationTimestamps\"", json);
            Assert.Contains("\"ServiceA\"", json);
            Assert.Contains("\"ServiceB\"", json);
        }

        [Fact]
        public void ToJson_WithNullDictionary_SerializesAsNull()
        {
            // Arrange
            var snapshot = new RegistrySnapshot
            {
                TotalServiceCount = 5,
                ServiceRegistrationTimestamps = null
            };

            // Act
            string json = snapshot.ToJson();

            // Assert
            Assert.NotNull(json);
            Assert.Contains("\"TotalServiceCount\": 5", json);
            Assert.Contains("\"ServiceRegistrationTimestamps\":null", json);
        }

        [Fact]
        public void ToJson_WithEmptyDictionary_SerializesEmptyObject()
        {
            // Arrange
            var snapshot = new RegistrySnapshot
            {
                TotalServiceCount = 0,
                ServiceRegistrationTimestamps = new Dictionary<string, DateTime>()
            };

            // Act
            string json = snapshot.ToJson();

            // Assert
            Assert.NotNull(json);
            Assert.Contains("\"TotalServiceCount\": 0", json);
            Assert.Contains("\"ServiceRegistrationTimestamps\":{}", json);
        }
    }
}