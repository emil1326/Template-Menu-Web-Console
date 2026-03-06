# Error Guidelines

This document defines how errors should be modeled and displayed in the current architecture.

## 1) Core rule

If a failure is meaningful for users, logs, diagnostics, or review, represent it as `AppError`.

Expected failures should not rely on raw exceptions in normal control flow.

## 2) Preferred flow

- Domain/service/repository operation fails.
- Return `Result` / `Result<T>` with an `AppError`.
- Caller decides whether to display (`Run`, `ToUserMessage`) or propagate.

## 3) Severity convention

Suggested severity map:

- `Validation`: 100
- `NotFound`: 200
- `Conflict`: 300
- `DataSource`: 500
- `Timeout`: 500
- `Configuration`: 600
- `Unknown`: 800
- `Breaking`: 1000

These values are conventions, not hard constraints.

## 4) What belongs in AppError

- `ErrorCode`: stable category.
- `TechnicalMessage`: developer-oriented root cause.
- `UserMessage` (optional): customized user-facing text.
- `Inner` exception (optional): retain original technical context.

## 5) UI behavior

- `AppError` implements UI rendering behavior.
- Use `ToUserMessage()` for safe user text fallback.
- Keep internal stack details for logs/dev context.

## 6) App hierarchy note

Router/app/page refactors do not change error policy:

- app root pages still own settings/info links
- child pages still surface failures through `Result` + `AppError`
- parent apps can inspect aggregated info/settings, not raw exceptions

## 7) Minimal usage examples

Create and return failure:

```csharp
return Result.Failure(new AppError(
    ErrorCode.NotFound,
    "Ouvrage introuvable",
    technicalMessage: $"No record for id={id}"));
```

Display in UI:

```csharp
if (!result.IsSuccess)
{
    result.Error!.Run();
}
```

Wrap unexpected exception when needed:

```csharp
catch (Exception ex)
{
    return Result.Failure(new AppError(
        ErrorCode.DataSource,
        "Erreur de lecture",
        technicalMessage: "ReadAll failed",
        inner: ex));
}
```

## 8) Maintenance checklist

When adding a new `new AppError(...)` site:

- choose the right `ErrorCode`
- set useful technical context
- keep user message clear
- verify severity consistency with existing usage
- update this document if policy changed
