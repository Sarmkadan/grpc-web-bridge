#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using GrpcWebBridge.Utilities;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Extension methods for <see cref="DateTimeUtilityTests"/> that provide utility functionality
/// for testing date/time operations and common testing scenarios.
/// </summary>
public static class DateTimeUtilityTestsExtensions
{
    /// <summary>
    /// Creates a sequence of dates between two dates, excluding weekends and optionally holidays.
    /// Useful for generating test data for business day calculations.
    /// </summary>
    /// <param name="start">The start date (inclusive).</param>
    /// <param name="end">The end date (inclusive).</param>
    /// <param name="includeHolidays">Whether to include holidays in the sequence.</param>
    /// <param name="holidays">Optional collection of holidays to exclude.</param>
    /// <returns>An enumerable of dates between start and end, excluding weekends and holidays.</returns>
    /// <exception cref="ArgumentException"><paramref name="start"/> is after <paramref name="end"/>.</exception>
    public static IEnumerable<DateTime> GetBusinessDaysInRange(
        this DateTimeUtilityTests _,
        DateTime start,
        DateTime end,
        bool includeHolidays = false,
        IReadOnlyCollection<DateTime>? holidays = null)
    {
        if (start > end)
        {
            throw new ArgumentException("Start date must be before or equal to end date.", nameof(start));
        }

        var current = start;
        while (current <= end)
        {
            if (DateTimeUtility.IsWeekend(current) is false &&
                (includeHolidays || holidays is null || !holidays.Contains(current)))
            {
                yield return current;
            }

            current = current.AddDays(1);
        }
    }

    /// <summary>
    /// Converts a Unix timestamp to a formatted date string for test assertions.
    /// Useful for debugging and test output formatting.
    /// </summary>
    /// <param name="_">The <see cref="DateTimeUtilityTests"/> instance.</param>
    /// <param name="timestamp">The Unix timestamp to format.</param>
    /// <param name="format">Optional format string. Defaults to "yyyy-MM-dd HH:mm:ss".</param>
    /// <returns>A formatted date string.</returns>
    public static string ToFormattedDateString(
        this DateTimeUtilityTests _,
        long timestamp,
        string? format = null)
    {
        var date = DateTimeUtility.FromUnixTimestamp(timestamp);
        return format is null
            ? date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            : date.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Generates a sequence of relative time descriptions between a reference date and test dates.
    /// Useful for testing relative time formatting with various time differences.
    /// </summary>
    /// <param name="_">The <see cref="DateTimeUtilityTests"/> instance.</param>
    /// <param name="reference">The reference date.</param>
    /// <param name="testDates">Dates to compare against the reference.</param>
    /// <returns>A sequence of relative time descriptions.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="testDates"/> is null.</exception>
    public static IEnumerable<string> GetRelativeTimeDescriptions(
        this DateTimeUtilityTests _,
        DateTime reference,
        IEnumerable<DateTime> testDates)
    {
        ArgumentNullException.ThrowIfNull(testDates);

        foreach (var testDate in testDates)
        {
            yield return DateTimeUtility.ToRelativeTime(testDate, reference);
        }
    }

    /// <summary>
    /// Calculates the number of business days between two dates, including both endpoints.
    /// Provides a more flexible version of GetBusinessDaysBetween that allows customization.
    /// </summary>
    /// <param name="_">The <see cref="DateTimeUtilityTests"/> instance.</param>
    /// <param name="start">The start date.</param>
    /// <param name="end">The end date.</param>
    /// <param name="includeStartEnd">Whether to include both start and end dates in the count (when false, excludes both).</param>
    /// <returns>The number of business days between the dates.</returns>
    /// <exception cref="ArgumentException"><paramref name="start"/> is after <paramref name="end"/>.</exception>
    public static int GetBusinessDaysBetweenFlexible(
        this DateTimeUtilityTests _,
        DateTime start,
        DateTime end,
        bool includeStartEnd = true)
    {
        if (start > end)
        {
            throw new ArgumentException("Start date must be before or equal to end date.", nameof(start));
        }

        var count = 0;
        var current = start;

        while (current <= end)
        {
            if (DateTimeUtility.IsWeekend(current) is false)
            {
                count++;
            }

            current = current.AddDays(1);
        }

        return includeStartEnd ? count : Math.Max(0, count - 2);
    }
}