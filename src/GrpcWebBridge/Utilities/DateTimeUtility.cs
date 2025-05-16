// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;

namespace GrpcWebBridge.Utilities;

/// <summary>
/// DateTime manipulation and formatting utilities.
/// Provides helpers for common date/time operations, timezone handling, and formatting.
/// </summary>
public static class DateTimeUtility
{
    /// <summary>
    /// Converts a DateTime to ISO 8601 string.
    /// Uses UTC time with Z suffix.
    /// </summary>
    public static string ToIso8601(DateTime dateTime)
    {
        return dateTime.ToUniversalTime().ToString("O");
    }

    /// <summary>
    /// Parses an ISO 8601 string to DateTime.
    /// </summary>
    public static DateTime? FromIso8601(string iso8601String)
    {
        if (string.IsNullOrEmpty(iso8601String))
            return null;

        if (DateTime.TryParseExact(
            iso8601String,
            "O",
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var result))
        {
            return result;
        }

        // Try more flexible parsing
        if (DateTime.TryParse(iso8601String, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var flexResult))
        {
            return flexResult;
        }

        return null;
    }

    /// <summary>
    /// Gets the Unix timestamp (seconds since epoch) for a DateTime.
    /// </summary>
    public static long ToUnixTimestamp(DateTime dateTime)
    {
        return (long)(dateTime.ToUniversalTime() - UnixEpoch).TotalSeconds;
    }

    /// <summary>
    /// Converts Unix timestamp to DateTime.
    /// </summary>
    public static DateTime FromUnixTimestamp(long unixTimestamp)
    {
        return UnixEpoch.AddSeconds(unixTimestamp);
    }

    /// <summary>
    /// Gets a human-readable relative time string (e.g., "2 hours ago").
    /// </summary>
    public static string ToRelativeTime(DateTime dateTime, DateTime? referenceTime = null)
    {
        var reference = referenceTime ?? DateTime.UtcNow;
        var diff = reference - dateTime.ToUniversalTime();

        if (diff.TotalSeconds < 60)
            return "just now";

        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes} minute{((int)diff.TotalMinutes != 1 ? "s" : "")} ago";

        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours} hour{((int)diff.TotalHours != 1 ? "s" : "")} ago";

        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays} day{((int)diff.TotalDays != 1 ? "s" : "")} ago";

        if (diff.TotalDays < 30)
            return $"{(int)(diff.TotalDays / 7)} week{((int)(diff.TotalDays / 7) != 1 ? "s" : "")} ago";

        if (diff.TotalDays < 365)
            return $"{(int)(diff.TotalDays / 30)} month{((int)(diff.TotalDays / 30) != 1 ? "s" : "")} ago";

        return $"{(int)(diff.TotalDays / 365)} year{((int)(diff.TotalDays / 365) != 1 ? "s" : "")} ago";
    }

    /// <summary>
    /// Converts DateTime to specified timezone.
    /// </summary>
    public static DateTime ConvertToTimeZone(DateTime dateTime, string timeZoneId)
    {
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTime(dateTime, timeZone);
        }
        catch
        {
            return dateTime;
        }
    }

    /// <summary>
    /// Gets the start of a time period (day, week, month, year).
    /// </summary>
    public static DateTime GetPeriodStart(DateTime dateTime, DateTimePeriod period)
    {
        return period switch
        {
            DateTimePeriod.Day => dateTime.Date,
            DateTimePeriod.Week => dateTime.AddDays(-(int)dateTime.DayOfWeek).Date,
            DateTimePeriod.Month => new DateTime(dateTime.Year, dateTime.Month, 1),
            DateTimePeriod.Year => new DateTime(dateTime.Year, 1, 1),
            _ => dateTime
        };
    }

    /// <summary>
    /// Gets the end of a time period.
    /// </summary>
    public static DateTime GetPeriodEnd(DateTime dateTime, DateTimePeriod period)
    {
        var start = GetPeriodStart(dateTime, period);
        return period switch
        {
            DateTimePeriod.Day => start.AddDays(1).AddTicks(-1),
            DateTimePeriod.Week => start.AddDays(7).AddTicks(-1),
            DateTimePeriod.Month => start.AddMonths(1).AddTicks(-1),
            DateTimePeriod.Year => start.AddYears(1).AddTicks(-1),
            _ => start
        };
    }

    /// <summary>
    /// Calculates business days between two dates (excludes weekends).
    /// </summary>
    public static int GetBusinessDaysBetween(DateTime startDate, DateTime endDate)
    {
        var businessDays = 0;
        var current = startDate.Date;
        var end = endDate.Date;

        while (current <= end)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
            {
                businessDays++;
            }

            current = current.AddDays(1);
        }

        return businessDays;
    }

    /// <summary>
    /// Gets the age in years from a birthdate.
    /// </summary>
    public static int GetAge(DateTime birthDate, DateTime? referenceDate = null)
    {
        var reference = referenceDate ?? DateTime.Today;
        var age = reference.Year - birthDate.Year;

        if (birthDate.Date > reference.AddYears(-age))
            age--;

        return age;
    }

    /// <summary>
    /// Checks if a date is a weekend.
    /// </summary>
    public static bool IsWeekend(DateTime dateTime)
    {
        return dateTime.DayOfWeek == DayOfWeek.Saturday || dateTime.DayOfWeek == DayOfWeek.Sunday;
    }

    /// <summary>
    /// Checks if a date is today.
    /// </summary>
    public static bool IsToday(DateTime dateTime)
    {
        return dateTime.Date == DateTime.Today;
    }

    /// <summary>
    /// Checks if a date is in the future.
    /// </summary>
    public static bool IsFuture(DateTime dateTime, DateTime? referenceTime = null)
    {
        var reference = referenceTime ?? DateTime.UtcNow;
        return dateTime > reference;
    }

    /// <summary>
    /// Checks if a date is in the past.
    /// </summary>
    public static bool IsPast(DateTime dateTime, DateTime? referenceTime = null)
    {
        var reference = referenceTime ?? DateTime.UtcNow;
        return dateTime < reference;
    }

    /// <summary>
    /// Rounds DateTime to nearest interval.
    /// </summary>
    public static DateTime RoundTo(DateTime dateTime, TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
            throw new ArgumentException("Interval must be positive", nameof(interval));

        var ticks = (dateTime.Ticks + interval.Ticks / 2) / interval.Ticks;
        return new DateTime(ticks * interval.Ticks);
    }

    /// <summary>
    /// Formats DateTime with a specified pattern.
    /// </summary>
    public static string Format(DateTime dateTime, string format = "G")
    {
        return dateTime.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets human-readable duration between two dates.
    /// </summary>
    public static string GetDurationString(DateTime startDate, DateTime endDate)
    {
        var duration = endDate - startDate;

        if (duration.TotalSeconds < 60)
            return $"{(int)duration.TotalSeconds}s";

        if (duration.TotalMinutes < 60)
            return $"{(int)duration.TotalMinutes}m {(int)duration.Seconds}s";

        if (duration.TotalHours < 24)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";

        return $"{(int)duration.TotalDays}d {duration.Hours}h";
    }

    private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}

/// <summary>
/// Time period enumeration.
/// </summary>
public enum DateTimePeriod
{
    Day,
    Week,
    Month,
    Year
}
