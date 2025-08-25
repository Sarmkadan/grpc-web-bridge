// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Utilities;
using Xunit;

namespace GrpcWebBridge.Tests;

public class DateTimeUtilityTests
{
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ToUnixTimestamp_WithUnixEpoch_ReturnsZero()
    {
        // Arrange & Act
        var timestamp = DateTimeUtility.ToUnixTimestamp(UnixEpoch);

        // Assert
        timestamp.Should().Be(0L);
    }

    [Fact]
    public void FromUnixTimestamp_WithZero_ReturnsUnixEpoch()
    {
        // Arrange & Act
        var result = DateTimeUtility.FromUnixTimestamp(0);

        // Assert
        result.Should().Be(UnixEpoch);
    }

    [Fact]
    public void ToRelativeTime_WithinOneMinute_ReturnsJustNow()
    {
        // Arrange
        var reference = DateTime.UtcNow;
        var recent = reference.AddSeconds(-30);

        // Act
        var result = DateTimeUtility.ToRelativeTime(recent, reference);

        // Assert
        result.Should().Be("just now");
    }

    [Fact]
    public void GetBusinessDaysBetween_MondayToFriday_ReturnsFive()
    {
        // Arrange
        var monday = new DateTime(2024, 1, 8);  // confirmed Monday
        var friday = new DateTime(2024, 1, 12); // confirmed Friday

        // Act
        var days = DateTimeUtility.GetBusinessDaysBetween(monday, friday);

        // Assert
        days.Should().Be(5);
    }

    [Fact]
    public void GetBusinessDaysBetween_AcrossWeekend_ExcludesSaturdayAndSunday()
    {
        // Arrange — Friday to Monday spans a weekend
        var friday = new DateTime(2024, 1, 12);
        var monday = new DateTime(2024, 1, 15);

        // Act
        var days = DateTimeUtility.GetBusinessDaysBetween(friday, monday);

        // Assert: Friday (1) + Monday (1) = 2; Saturday and Sunday excluded
        days.Should().Be(2);
    }

    [Fact]
    public void IsWeekend_WithSaturday_ReturnsTrue()
    {
        // Arrange
        var saturday = new DateTime(2024, 1, 13);

        // Act & Assert
        DateTimeUtility.IsWeekend(saturday).Should().BeTrue();
    }
}
