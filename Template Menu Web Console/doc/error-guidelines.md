# Error Codes & Default Severities

This document lists all the `ErrorCode` values defined in the project together
with a recommended **severity** value. Severity is an arbitrary integer used by
the logging subsystem (`Logger`) to decide whether a message should be shown on
the console. Messages with a severity below the global threshold
(`Globals.LogSeverityThreshold`) are still written to the log file but are
suppressed from the user-facing output.

The table below is sorted by severity (lowest first):

| Severity | ErrorCode     | Meaning                                             |
|----------|---------------|-----------------------------------------------------|
| `100`    | `Validation`  | Input data failed a validation rule.                |
| `200`    | `NotFound`    | Requested entity or resource could not be found.    |
| `300`    | `Conflict`    | Operation conflicts with the current data state.    |
| `500`    | `DataSource`  | Data source (file, database, network) error.        |
| `500`    | `Timeout`     | Operation exceeded allowed time limit.              |
| `600`    | `Configuration` | Application or entity configuration is invalid.   |
| `800`    | `Unknown`     | Unexpected error that does not fit another category. |
| `1000`   | `Breaking`    | Very dangerous error that requires app shutdown or complete restart. |

> **Note:** these severity values are conventions used when creating
> `AppError` instances; you are free to choose other numbers or assign
> different values per occurrence. For example:

```csharp
if (!result.IsSuccess)
{
    // a missing record is only a minor issue so log at 200
    Logger.Error(result.Error!.TechnicalMessage, severity:200);
    result.Error.Run();
}
```

If you would like to centralise the mapping from `ErrorCode` to severity, add an
`int GetSeverity(this ErrorCode code)` extension or a lookup table in the code
and update this document accordingly.

---

## When to use `AppError`

All failures that are significant enough to be logged, shown to the user, or
might help a developer diagnose a problem **must** be represented by an
`AppError` instance.  The class was created to centralise formatting,
severity, and stack‑trace trimming so that:

* every message is logged consistently (including a shortened stack trace);
* the global severity threshold can suppress unimportant noise from the
  console while still keeping a full record in the log file;
* console output that carries semantic meaning (`[WARN]`, `[ERROR]`, `[OK]`,
  etc.) should **always** be accompanied by a corresponding call to
  `Logger` so developers can see it even if the user is not watching the
  screen;
* the same object can render itself to the UI (it implements `IUIComponent`)
  and drive `Run()` behaviour in calling code.

> **Do not** throw or return raw `Exception`, `InvalidOperationException`,
> etc.  Those are for internal control flow only; if you want the problem to
> reach the log or the screen, wrap it in an `AppError` instead.  The only
> built‑in exception that is allowed to bubble is the one thrown by
> `AppError.Throw()` itself when you are intentionally aborting execution.

### Instantiating an `AppError`

Create a new instance with an `ErrorCode`, a user‑facing message and (optionally)
technical details:

```csharp
var err = new AppError(
    ErrorCode.NotFound,
    "Ouvrage introuvable",              // message users will see
    technicalMessage: $"ID {id} was not in the collection"
);
```

You may also pass a custom severity if you want to override the default
(500):

```csharp
new AppError(ErrorCode.Timeout, "op timed out", severity: 750);
```

In catch blocks you typically convert an unexpected exception to an
`AppError` so that the downstream code can handle it uniformly:

```csharp
catch (Exception ex)
{
    throw new AppError(
        ErrorCode.DataSource,
        "failed to read from database",
        inner: ex
    );
}
```

or use the static helper:

```csharp
AppError.Throw(ErrorCode.Validation, "input was malformed");
```

The constructor already logs the error using `Logger` (respecting
`Globals.LogSeverityThreshold`), so callers rarely need to log again unless
they are adding context.

### Consuming `AppError`

Methods that can fail should return a `Result<T>` or similar wrapper that
holds the error:

```csharp
var result = await repo.GetOuvrage(id);
if (!result.IsSuccess)
{
    // the error has already been logged, but we can decorate or react.
    result.Error!.Run();    // shows it on screen and optionally exits
}
```

When catching errors at a higher level, inspect the type:

```csharp
try
{
    await service.DoWork();
}
catch (AppError appErr)
{
    // already logged, just render.
    appErr.Run();
}
```

### Displaying to users and developers

The body of `AppError` implements `IUIComponent` so the `Run()` method will
print a formatted message, including the user message, severity and (if the
global threshold allows) the trimmed stack trace.  For developers running the
app under a debugger or tailing the log file, the full technical details and
inner exception (if any) are available via the `TechnicalMessage` property.

By using `AppError` everywhere you guarantee that nothing important is
silently swallowed and that reviewers can audit all possible visible failures
by scanning the code for `new AppError(...)` (and checking the table earlier in
this document).

---

## Current `AppError` usages

Below are all the locations in the codebase where an `AppError` is constructed.
The severity column shows the value passed (or `500` if omitted – the default).
New error constructions **must be added to this list** to keep the severity
values logical and reviewable.

| Severity | File | Line | ErrorCode / Message |
|----------|------|------|---------------------|
| `600` | `Core/DataAccess/Services/MongoDBService.cs` | 46 | Configuration validation failure |
| `500` | `Core/DataAccess/Services/MongoDBService.cs` | 99 | DataSource (exception catch) |
| `500` | `Core/DataAccess/Services/MongoDBService.cs` | 102 | DataSource (exception catch) |
| `500` | `Core/DataAccess/Services/MongoDBService.cs` | 113 | NotFound (entity not found) |
| `500` | `Core/DataAccess/Services/MongoDBService.cs` | 116 | NotFound (entity with key not found) |
| `500` | `Core/DataAccess/Services/MongoDBService.cs` | 127 | DataSource (exception catch) |
| `500` | `Core/DataAccess/Services/MongoDBService.cs` | 130 | DataSource (exception catch) |
| `500` | `Core/DataAccess/Services/MongoDBService.cs` | 138 | NotFound (entity not found) |
| `500` | `Core/DataAccess/Services/MongoDBService.cs` | 141 | NotFound (entity with key not found) |
| `500` | `Core/DataAccess/Services/MongoDBService.cs` | 147 | DataSource (exception catch) |
| `500` | `Core/DataAccess/Services/MongoDBService.cs` | 150 | DataSource (exception catch) |
| `500` | `Core/DataAccess/Services/MongoDBService.cs` | 175 | DataSource (exception catch) |
| `500` | `Core/DataAccess/Services/MongoDBService.cs` | 178 | DataSource (exception catch) |
| `500` | `Core/DataAccess/Services/MongoDBService.cs` | 205 | DataSource (exception catch in generic method) |
| `600` | `Core/DataAccess/Services/MongoDBService.cs` | 225 | Configuration (no ID property) |
| `600` | `Core/DataAccess/Services/MongoDBService.cs` | 238 | Configuration (invalid composite key) |
| `600` | `Core/DataAccess/Services/MongoDBService.cs` | 256 | Configuration (null key value) |
| `600` | `Core/DataAccess/Services/JsonFileService.cs` | 40 | Configuration validation failure |
| `500` | `Core/DataAccess/Services/JsonFileService.cs` | 81 | DataSource (exception catch) |
| `500` | `Core/DataAccess/Services/JsonFileService.cs` | 99 | NotFound (entity with key not found) |
| `500` | `Core/DataAccess/Services/JsonFileService.cs` | 107 | DataSource (exception catch) |
| `500` | `Core/DataAccess/Services/JsonFileService.cs` | 124 | NotFound (entity with key not found) |
| `500` | `Core/DataAccess/Services/JsonFileService.cs` | 131 | DataSource (exception catch) |
| `500` | `Core/DataAccess/Services/JsonFileService.cs` | 163 | DataSource (exception catch) |
| `500` | `Core/DataAccess/Services/JsonFileService.cs` | 195 | DataSource (exception catch in generic method) |
| `600` | `Core/DataAccess/Repository/RepositoryBase.cs` | 33 | Configuration validation failure |
| `500` | `Core/DataAccess/Repository/RepositoryBase.cs` | 95 | NotFound (entity not found) |
| `600` | `Core/Identity/EntityKeyResolver.cs` | 36 | Runtime error: key was null |
| `500` | `Core/Identity/EntityKeyResolver.cs` | 85 | Configuration (exception wrapped) |
| `600` | `Core/Identity/EntityKeyResolver.cs` | 100 | Configuration (no ID property) |
| `500` | `UserApps/Repository/RepositoryOuvrages.cs` | 77 | NotFound (ouvrage introuvable) |
| `800` | `UserApps/UIComponents/Exemple.cs` | 13 | Unknown (not implemented) |
| `500` | `Core/Errors/AppError.cs` | 110 | Helper `Throw()` (centralised throw + logging) |

> **Reminder:** whenever you add `new AppError(...)` anywhere in the
> repository, update this table (and adjust severity if needed) so the
> mapping remains accurate.  This ensures severity levels stay logical and
> reviewers can quickly spot anomalies.
