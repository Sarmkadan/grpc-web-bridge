# EnumExtensions

Static extension-method class in `GrpcWebBridge.Domain` (`src/GrpcWebBridge/Domain/EnumExtensions.cs`). It adds three things to enums:

- a display string for a value, taken from `[Display(Name = ...)]` when present;
- a case-insensitive `TryParse` that also matches display names;
- helpers to list display strings for an enum type and to check whether a value is defined.

It uses only the BCL (`System.ComponentModel.DataAnnotations`, `System.Reflection`). Nothing in the file needs a NuGet package.

## Overview

Enum member names are often not what a user should see, for example in a UI dropdown, a log line, or an API response. Decorating a member with `DisplayAttribute` gives it a friendlier label. These helpers read that label and also accept it as input, so the same string works in both directions.

```csharp
using System.ComponentModel.DataAnnotations;
using GrpcWebBridge.Domain;

public enum PlanTier
{
    [Display(Name = "Gold Plan")]
    Gold,
    Silver
}
```

---

## API

### `public static string ToDisplayString(this Enum value)`

Returns the display string for an enum value.

- If the member's field has a `DisplayAttribute` with a non-null `Name`, that `Name` is returned.
- Otherwise the member name (`value.ToString()`) is returned.

The lookup is by field name, so it applies to any `Enum`. It is also the overload used for `Enum`-typed values, while more specific overloads on a type (such as `StreamState.ToDisplayString`) take precedence where they exist.

- **Parameters**
  - `value`: The enum value to describe.
- **Returns**
  - The display name, or the member name when no display name is set.
- **Throws**
  - `ArgumentNullException`: If `value` is `null` (parameter name `value`).

**Examples**

```csharp
PlanTier.Gold.ToDisplayString();    // "Gold Plan"
PlanTier.Silver.ToDisplayString();  // "Silver"
((PlanTier)42).ToDisplayString();   // "42"  (undefined numeric value, no field matches)
```

---

### `public static bool TryParse<TEnum>(this string? value, out TEnum result) where TEnum : struct, Enum`

Attempts to convert a string to `TEnum`. The match is case-insensitive and accepts either:

1. the member name, or anything else `Enum.TryParse` accepts (numeric text, comma-separated flag names, leading or trailing whitespace); or
2. the `DisplayAttribute.Name` of a member, compared with `StringComparison.OrdinalIgnoreCase`.

Plain enum parsing runs first. The display-name pass runs only when that fails.

- **Type parameters**
  - `TEnum`: The enum type. Must be a value type and an `Enum`.
- **Parameters**
  - `value`: The text to parse. May be `null`.
  - `result`: On return, the parsed value when the method returns `true`. Otherwise `default(TEnum)`, which is the member whose underlying value is `0` (not necessarily a defined member).
- **Returns**
  - `true` if the text was parsed; otherwise `false`.
- **Throws**
  - Nothing. Invalid input returns `false`.

**Examples**

```csharp
bool ok1 = "gold plan".TryParse<PlanTier>(out var tier1);  // true, tier1 == PlanTier.Gold
bool ok2 = "SILVER".TryParse<PlanTier>(out var tier2);     // true, tier2 == PlanTier.Silver
bool ok3 = "Platinum".TryParse<PlanTier>(out var tier3);   // false, tier3 == PlanTier.Gold (default)
bool ok4 = ((string?)null).TryParse<PlanTier>(out _);      // false
```

A typical use is reading a value from a query string or configuration:

```csharp
if (!request.Query["tier"].ToString().TryParse<PlanTier>(out var tier))
{
    return Results.BadRequest("Unknown plan tier.");
}
```

---

### `public static IReadOnlyList<string> GetDisplayStrings<TEnum>() where TEnum : struct, Enum`

Returns the display string of every value of `TEnum`, in the order returned by `Enum.GetValues<TEnum>()`. Each entry is produced by `ToDisplayString`.

- **Type parameters**
  - `TEnum`: The enum type.
- **Returns**
  - A read-only list (the underlying array is a `string[]`) with one entry per declared value. Flag combinations are not included; only declared members are listed.
- **Throws**
  - Nothing.

**Examples**

```csharp
IReadOnlyList<string> tiers = EnumExtensions.GetDisplayStrings<PlanTier>();
// ["Gold Plan", "Silver"]

[Flags]
public enum Perm { None = 0, [Display(Name = "Can Read")] Read = 1, Write = 2 }

EnumExtensions.GetDisplayStrings<Perm>();
// ["None", "Can Read", "Write"]
```

---

### `public static bool IsDefined<TEnum>(this TEnum value) where TEnum : struct, Enum`

Returns whether `value` equals one of the declared constants of `TEnum`. It delegates to `Enum.IsDefined`.

- **Type parameters**
  - `TEnum`: The enum type.
- **Parameters**
  - `value`: The value to check.
- **Returns**
  - `true` if `value` is a declared member; otherwise `false`.
- **Throws**
  - Nothing.

**Examples**

```csharp
PlanTier.Gold.IsDefined();        // true
((PlanTier)42).IsDefined();       // false
(Perm.Read | Perm.Write).IsDefined(); // false: the combination is not a declared member
```

---

## Edge cases

| Input | `ToDisplayString` | `TryParse` | Notes |
|---|---|---|---|
| Member with `[Display(Name = "X")]` | `"X"` | `true` for `"X"` or the member name | Name matching is case-insensitive. |
| Member without `DisplayAttribute` | Member name | `true` for member name | Display-name pass finds nothing extra. |
| `[Display]` with no `Name` set | Member name | Member name only | `Name` is `null`, so the fallback applies. |
| Undefined numeric value, e.g. `(PlanTier)42` | `"42"` | `"42"` parses to `(PlanTier)42` | `IsDefined()` returns `false`. Check it when you must reject unknown values. |
| Combined flags, e.g. `Read \| Write` | `"Read, Write"` (from `ToString`) | `"Read, Write"` parses to the combination | Only the combined name is produced; no display name is used for the combination. |
| `null` value passed to `ToDisplayString` | Throws `ArgumentNullException` | n/a | |
| `null`, `""`, or whitespace-only string to `TryParse` | n/a | Returns `false` | `result` is `default(TEnum)`. |
| Leading or trailing whitespace, e.g. `" Silver "` | n/a | `true` | Whitespace is accepted by `Enum.TryParse`. |
| Display name with different case, e.g. `"can read"` | n/a | `true` | Display comparison is `OrdinalIgnoreCase`. |
| Two members sharing a display name | Each returns its own name | First match in reflection order wins | Avoid duplicate display names. |

### Notes

- **`TryParse` accepts numbers.** `"99".TryParse<PlanTier>(out var t)` returns `true` with `t == (PlanTier)99`. Call `IsDefined()` on the result when only declared members should be accepted.
- **`TryParse` does not return `false` on an out-of-range value.** It returns `true`, as above. The `false` path is for text that matches neither a name nor a display name.
- **Display names are not trimmed.** Only `Enum.TryParse` trims whitespace. A display name with surrounding spaces in the attribute will only match that exact spelling.
- **Reflection on every call.** `ToDisplayString` and the display pass of `TryParse` read attributes via reflection each time. Cache the results (for example, the output of `GetDisplayStrings<T>()`) when the calls are on a hot path.
- **Overload precedence.** `ToDisplayString` is declared on `Enum`. `ServiceExtensions` (`src/GrpcWebBridge/Extensions/ServiceExtensions.cs`) declares a more specific `ToDisplayString(this StreamState state)`. Calls on a `StreamState` receiver bind to that overload, which returns `"Initializing"` for `StreamState.New`, not `"New"`. Calls on any other enum, or on a receiver typed as `Enum`, use this class's version.

## See also

- [Enums.md](Enums.md) - Every enum in `GrpcWebBridge.Domain`, including `ToDisplayString`.
- [ServiceExtensions.md](ServiceExtensions.md) - Contains the `StreamState`-specific `ToDisplayString` overload.
