using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using GrpcWebBridge.Utilities;
using System.Text.Json;

namespace GrpcWebBridge.Tests
{
    public class DateTimeUtilityJsonExtensionsTests
    {
        [Fact]
        public void ToJson_Happy_PATH()
        {
            // Given
            DateTime value = new DateTime(2022, 1, 1);
            bool indented = false;

            // When
            string json = DateTimeUtilityJsonExtensions.ToJson(value, indented);

            // Then
            Assert.NotEmpty(json);
        }

        [Fact]
        public void FromJson_HAPPY_PATH()
        {
            // Given
            string json = "2022-01-01T00:00:00.000Z";

            // When
            DateTime? result = DateTimeUtilityJsonExtensions.FromJson(json);

            // Then
            Assert.NotNull(result);
            Assert.Equal(new DateTime(2022, 1, 1), result);
        }

        [Fact]
        public void TryFromJson_HAPPY_PATH()
        {
            // Given
            string json = "2022-01-01T00:00:00.000Z";
            DateTime? value = null;

            // When
            bool success = DateTimeUtilityJsonExtensions.TryFromJson(json, out value);

            // Then
            Assert.True(success);
            Assert.NotNull(value);
            Assert.Equal(new DateTime(2022, 1, 1), value);
        }

        [Fact]
        public void ToJson_NULL_INPUT()
        {
            // Given
            DateTime? value = null;
            bool indented = false;

            // When and Then
            Assert.Throws<ArgumentNullException>(() => DateTimeUtilityJsonExtensions.ToJson(value, indented));
        }

        [Fact]
        public void FromJson_NULL_INPUT()
        {
            // Given
            string json = null;

            // When and Then
            Assert.Throws<ArgumentException>(() => DateTimeUtilityJsonExtensions.FromJson(json));
        }

        [Fact]
        public void TryFromJson_NULL_INPUT()
        {
            // Given
            string json = null;
            DateTime? value = null;

            // When and Then
            Assert.Throws<ArgumentException>(() => DateTimeUtilityJsonExtensions.TryFromJson(json, out value));
        }
    }
}