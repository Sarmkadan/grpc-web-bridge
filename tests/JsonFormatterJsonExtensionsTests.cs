using System.Text.Json;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using GrpcWebBridge.Formatters;

namespace GrpcWebBridge.Tests
{
    public class JsonFormatterJsonExtensionsTests
    {
        [Fact]
        public void ToJson_Happy_PATH()
        {
            // Arrange
            var formatter = new JsonFormatter();
            // Act
            var json = JsonFormatterJsonExtensions.ToJson(formatter);
            // Assert
            Assert.NotNull(json);
        }

        [Fact]
        public void FromJson_HAPPY_PATH()
        {
            // Arrange
            var json = "{}";
            // Act
            var formatter = JsonFormatterJsonExtensions.FromJson(json);
            // Assert
            Assert.NotNull(formatter);
        }

        [Fact]
        public void TryFromJson_HAPPY_PATH()
        {
            // Arrange
            var json = "{}";
            JsonFormatter? formatter;
            // Act
            var success = JsonFormatterJsonExtensions.TryFromJson(json, out formatter);
            // Assert
            Assert.True(success);
            Assert.NotNull(formatter);
        }
    }
}