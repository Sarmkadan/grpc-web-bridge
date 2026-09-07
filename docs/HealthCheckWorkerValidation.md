# HealthCheckWorker validation

`HealthCheckWorkerValidation` provides three extension methods for checking a
[`HealthCheckWorker`](HealthCheckWorker.md) instance: `Validate`, `IsValid`, and
`EnsureValid`. Validation does not start the worker or perform a service health
check.

The methods inspect the `HealthCheckOptions` supplied to the worker's
constructor. The default options (`30` seconds, `5000` milliseconds, and `10`
seconds) satisfy all validation rules.

## Validation rules

| Option | Valid range | Problem reported for an invalid value |
| --- | --- | --- |
| `CheckIntervalSeconds` | 5 through 300 seconds, inclusive | A non-positive value must be positive; a value from 1 through 4 must be at least 5; a value above 300 must not exceed 300. |
| `CheckTimeoutMs` | 100 through 30,000 milliseconds, inclusive | A non-positive value must be positive; a value from 1 through 99 must be at least 100; a value above 30,000 must not exceed 30,000. |
| `InitialDelaySeconds` | 0 through 300 seconds, inclusive | A negative value is not allowed; a value above 300 must not exceed 300. |

All option problems are collected in option order: check interval, timeout,
then initial delay. Validation also calls `GetStatistics()`. If that call throws,
the exception is converted into a problem with this form:

```text
GetStatistics() method failed: <exception message>
```

The current `HealthCheckWorker.GetStatistics()` implementation normally returns
a statistics snapshot without throwing. The probe is nevertheless part of the
validation contract.

## `Validate`

```csharp
public static IReadOnlyList<string> Validate(this HealthCheckWorker? value)
```

Returns a read-only list of human-readable problems. An empty list means that
the options and statistics probe passed validation. Invalid option values are
reported in the list rather than thrown.

`Validate` throws `ArgumentNullException` when `value` is `null`.

```csharp
using GrpcWebBridge.BackgroundWorkers;

var options = new HealthCheckOptions
{
    CheckIntervalSeconds = 2,
    CheckTimeoutMs = 50,
    InitialDelaySeconds = -1
};

var worker = new HealthCheckWorker(logger, serviceRegistry, eventBus, options);
IReadOnlyList<string> problems = worker.Validate();

foreach (string problem in problems)
{
    Console.WriteLine(problem);
}
```

This example returns three problems: the interval must be at least 5 seconds,
the timeout must be at least 100 milliseconds, and the initial delay cannot be
negative.

## `IsValid`

```csharp
public static bool IsValid(this HealthCheckWorker? value)
```

Calls `Validate` and returns `true` only when no problems are found. It returns
`false` for invalid options or a failed statistics probe.

Because it delegates to `Validate`, `IsValid` throws `ArgumentNullException`
when `value` is `null`.

```csharp
var options = new HealthCheckOptions
{
    CheckIntervalSeconds = 30,
    CheckTimeoutMs = 5000,
    InitialDelaySeconds = 0
};

var worker = new HealthCheckWorker(logger, serviceRegistry, eventBus, options);

if (worker.IsValid())
{
    Console.WriteLine("The health-check worker configuration is valid.");
}
```

## `EnsureValid`

```csharp
public static void EnsureValid(this HealthCheckWorker? value)
```

Returns normally when `Validate` finds no problems. When one or more problems
are found, it throws `ArgumentException`; the exception message contains every
problem on a separate line, and `ParamName` is `value`.

`EnsureValid` throws:

- `ArgumentNullException` when `value` is `null`.
- `ArgumentException` when validation reports one or more problems.

```csharp
var options = new HealthCheckOptions
{
    CheckIntervalSeconds = 301,
    CheckTimeoutMs = 5000,
    InitialDelaySeconds = 10
};

var worker = new HealthCheckWorker(logger, serviceRegistry, eventBus, options);

try
{
    worker.EnsureValid();
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message);
    // Includes: CheckIntervalSeconds must not exceed 300 seconds, but was 301.
}
```

## Usage notes

- The examples assume that `logger`, `serviceRegistry`, and `eventBus` have
  already been created or resolved from dependency injection.
- Validation reads the options retained by the worker. Changing the original
  `HealthCheckOptions` object after construction changes what a later validation
  observes because the worker retains that same object.
- These helpers report configuration and statistics-access problems only. They
  do not establish whether registered services are healthy.
