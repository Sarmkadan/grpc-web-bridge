# DateTimeUtility

A static utility class providing a comprehensive set of date and time operations for the `grpc-web-bridge` project. It handles ISO 8601 serialization, Unix timestamp conversion, relative time formatting, time zone conversions, period calculations, business day counting, age computation, and various date predicates. All methods are stateless and operate on `DateTime` values without relying on instance state.

## API

### `ToIso8601`
```csharp
public static string ToIso8601(DateTime dateTime)
```
Converts a `DateTime` value to its ISO 8601 string representation in round-trip format (`"O"`). The output preserves time zone offset information when present.

**Parameters:**
- `dateTime` — The `DateTime` to serialize.

**Returns:** A string conforming to ISO 8601.

**Throws:** Never throws.

---

### `FromIso8601`
```csharp
public static DateTime? FromIso8601(string iso8601String)
```
Parses an ISO 8601 formatted string into a `DateTime`. Returns `null` if the string is null, empty, or not a valid ISO 8601 representation.

**Parameters:**
- `iso8601String` — The string to parse.

**Returns:** A `DateTime?` that is `null` on failure, or the parsed value on success.

**Throws:** Never throws.

---

### `ToUnixTimestamp`
```csharp
public static long ToUnixTimestamp(DateTime dateTime)
```
Converts a `DateTime` to a Unix timestamp representing the number of seconds elapsed since 1970-01-01T00:00:00Z. The input `DateTime` is treated as UTC; if its `Kind` is `Local` or `Unspecified`, it is converted to UTC before the calculation.

**Parameters:**
- `dateTime` — The `DateTime` to convert.

**Returns:** A `long` containing the Unix timestamp in seconds.

**Throws:** Never throws.

---

### `FromUnixTimestamp`
```csharp
public static DateTime FromUnixTimestamp(long unixTimestamp)
```
Creates a `DateTime` from a Unix timestamp (seconds since the Unix epoch). The resulting `DateTime` has `Kind` set to `Utc`.

**Parameters:**
- `unixTimestamp` — The number of seconds since 1970-01-01T00:00:00Z.

**Returns:** A UTC `DateTime` corresponding to the timestamp.

**Throws:** Never throws.

---

### `ToRelativeTime`
```csharp
public static string ToRelativeTime(DateTime dateTime)
```
Produces a human-readable relative time string comparing the given `DateTime` to the current system time (e.g., "3 hours ago", "in 2 days"). The comparison uses UTC to avoid local time discontinuities.

**Parameters:**
- `dateTime` — The `DateTime` to describe relative to now.

**Returns:** A string such as "just now", "5 minutes ago", "yesterday", or "in 3 months".

**Throws:** Never throws.

---

### `ConvertToTimeZone`
```csharp
public static DateTime ConvertToTimeZone(DateTime dateTime, string timeZoneId)
```
Converts a `DateTime` to the specified IANA or Windows time zone. The input is first normalized to UTC if necessary, then shifted to the target zone.

**Parameters:**
- `dateTime` — The source `DateTime`.
- `timeZoneId` — A valid time zone identifier (e.g., `"Eastern Standard Time"` or `"America/New_York"`).

**Returns:** A `DateTime` representing the same instant in the target time zone.

**Throws:** `ArgumentException` if `timeZoneId` is not a recognized time zone.

---

### `GetPeriodStart`
```csharp
public static DateTime GetPeriodStart(DateTime dateTime, string period)
```
Returns the start boundary of the specified period containing `dateTime`. Supported period values include `"day"`, `"week"`, `"month"`, `"quarter"`, and `"year"`. For `"week"`, the start is typically Monday at midnight.

**Parameters:**
- `dateTime` — The reference `DateTime`.
- `period` — A string identifying the period.

**Returns:** A `DateTime` set to the beginning of the period.

**Throws:** `ArgumentException` if `period` is not one of the supported values.

---

### `GetPeriodEnd`
```csharp
public static DateTime GetPeriodEnd(DateTime dateTime, string period)
```
Returns the exclusive end boundary of the specified period containing `dateTime`. For example, for `"month"` this is the first moment of the following month.

**Parameters:**
- `dateTime` — The reference `DateTime`.
- `period` — A string identifying the period.

**Returns:** A `DateTime` set to the end of the period.

**Throws:** `ArgumentException` if `period` is not one of the supported values.

---

### `GetBusinessDaysBetween`
```csharp
public static int GetBusinessDaysBetween(DateTime start, DateTime end)
```
Counts the number of business days (Monday through Friday) between two dates, inclusive of the start date and exclusive of the end date. The calculation ignores time-of-day components and does not account for public holidays.

**Parameters:**
- `start` — The start date.
- `end` — The end date.

**Returns:** A non-negative integer representing the count of weekdays in the interval. Returns 0 if `start >= end`.

**Throws:** Never throws.

---

### `GetAge`
```csharp
public static int GetAge(DateTime birthDate, DateTime referenceDate)
```
Calculates the age in full years as of the `referenceDate` based on the given `birthDate`. The result is the difference in years, decremented by one if the birthday has not yet occurred in the reference year.

**Parameters:**
- `birthDate` — The date of birth.
- `referenceDate` — The date on which to compute the age.

**Returns:** An integer age in years.

**Throws:** `ArgumentException` if `birthDate` is later than `referenceDate`.

---

### `IsWeekend`
```csharp
public static bool IsWeekend(DateTime dateTime)
```
Determines whether the given date falls on a Saturday or Sunday.

**Parameters:**
- `dateTime` — The date to check.

**Returns:** `true` if the day is Saturday or Sunday; otherwise `false`.

**Throws:** Never throws.

---

### `IsToday`
```csharp
public static bool IsToday(DateTime dateTime)
```
Checks whether the given date is the same calendar day as the current system date, based on local time.

**Parameters:**
- `dateTime` — The date to compare.

**Returns:** `true` if the date equals today; otherwise `false`.

**Throws:** Never throws.

---

### `IsFuture`
```csharp
public static bool IsFuture(DateTime dateTime)
```
Indicates whether the given `DateTime` is strictly later than the current system time in UTC.

**Parameters:**
- `dateTime` — The `DateTime` to evaluate.

**Returns:** `true` if the moment is in the future; otherwise `false`.

**Throws:** Never throws.

---

### `IsPast`
```csharp
public static bool IsPast(DateTime dateTime)
```
Indicates whether the given `DateTime` is strictly earlier than the current system time in UTC.

**Parameters:**
- `dateTime` — The `DateTime` to evaluate.

**Returns:** `true` if the moment is in the past; otherwise `false`.

**Throws:** Never throws.

---

### `RoundTo`
```csharp
public static DateTime RoundTo(DateTime dateTime, TimeSpan interval)
```
Rounds a `DateTime` to the nearest multiple of the specified `interval`. Midpoint values round up to the next interval boundary.

**Parameters:**
- `dateTime` — The `DateTime` to round.
- `interval` — The `TimeSpan` representing the rounding granularity.

**Returns:** A new `DateTime` rounded to the nearest interval boundary.

**Throws:** `ArgumentException` if `interval` is zero or negative.

---

### `Format`
```csharp
public static string Format(DateTime dateTime, string format)
```
Formats a `DateTime` using a custom or standard format string. This is a pass-through to the standard `DateTime.ToString(string)` method with invariant culture to ensure consistent output across environments.

**Parameters:**
- `dateTime` — The `DateTime` to format.
- `format` — A standard or custom format string (e.g., `"yyyy-MM-dd"`).

**Returns:** The formatted string representation.

**Throws:** `FormatException` if `format` is invalid.

---

### `GetDurationString`
```csharp
public static string GetDurationString(TimeSpan duration)
```
Converts a `TimeSpan` into a concise human-readable duration string, such as "2h 30m 15s". Components with zero value are omitted unless the entire duration is zero, in which case "0s" is returned.

**Parameters:**
- `duration` — The `TimeSpan` to represent.

**Returns:** A string describing the duration.

**Throws:** Never throws.

## Usage

### Example 1: Parsing, Converting, and Formatting
```csharp
string isoInput = "2025-03-15T14:30:00.0000000Z";
DateTime? parsed = DateTimeUtility.FromIso8601(isoInput);

if (parsed.HasValue)
{
    DateTime utcTime = parsed.Value;
    long unix = DateTimeUtility.ToUnixTimestamp(utcTime);
    DateTime eastern = DateTimeUtility.ConvertToTimeZone(utcTime, "Eastern Standard Time");
    string formatted = DateTimeUtility.Format(eastern, "yyyy-MM-dd HH:mm:ss zzz");
    string relative = DateTimeUtility.ToRelativeTime(utcTime);

    Console.WriteLine($"Unix: {unix}");
    Console.WriteLine($"Eastern: {formatted}");
    Console.WriteLine($"Relative: {relative}");
}
```

### Example 2: Business Logic with Periods and Predicates
```csharp
DateTime orderDate = new DateTime(2025, 3, 14, 9, 0, 0, DateTimeKind.Utc);
DateTime shipDate = new DateTime(2025, 3, 20, 17, 0, 0, DateTimeKind.Utc);

int transitBusinessDays = DateTimeUtility.GetBusinessDaysBetween(orderDate, shipDate);
DateTime monthStart = DateTimeUtility.GetPeriodStart(orderDate, "month");
DateTime monthEnd = DateTimeUtility.GetPeriodEnd(orderDate, "month");
bool shippedOnWeekend = DateTimeUtility.IsWeekend(shipDate);
bool isPastDue = DateTimeUtility.IsPast(shipDate);

Console.WriteLine($"Transit days: {transitBusinessDays}");
Console.WriteLine($"Order month: {DateTimeUtility.Format(monthStart, "yyyy-MM-dd")} to {DateTimeUtility.Format(monthEnd, "yyyy-MM-dd")}");
Console.WriteLine($"Shipped on weekend: {shippedOnWeekend}");
Console.WriteLine($"Past due: {isPastDue}");
```

## Notes

- **Time Zone Handling:** Methods that depend on the current system time (`ToRelativeTime`, `IsToday`, `IsFuture`, `IsPast`) use the local system clock. `ConvertToTimeZone` relies on the platform's time zone database and will throw if the identifier is unrecognized.
- **Edge Cases in `FromIso8601`:** Null, empty, or malformed strings return `null` without throwing. Callers should check for `null` before using the result.
- **`GetBusinessDaysBetween`:** The calculation excludes weekends but does not account for holidays. The interval is `[start, end)` — the start date counts if it is a weekday, the end date does not. If `start` is later than or equal to `end`, the result is 0.
- **`GetAge`:** Throws `ArgumentException` when `birthDate > referenceDate`. Leap years are handled correctly; a person born on February 29 is considered to age on March 1 in non-leap years.
- **`RoundTo`:** Requires a positive `TimeSpan`. Zero or negative intervals cause an `ArgumentException`. Midpoint rounding uses the "away from zero" convention.
- **`GetDurationString`:** Zero and negative `TimeSpan` values are supported. A zero duration yields `"0s"`. Negative durations produce a leading minus sign (e.g., `"-1h 30m"`).
- **Thread Safety:** All methods are static and operate on immutable `DateTime` and `TimeSpan` structs or strings. No shared mutable state is used. The class is safe to call concurrently from multiple threads without external synchronization.
