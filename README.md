# Template Menu Web Console

A reusable C# console application template built on .NET 10 that provides a full-featured interactive menu shell, automatic settings persistence, a layered data access framework, optional MongoDB integration, and structured error handling — out of the box.

**Authors:** Emilien Devauchelle — Emil's Works  
**Version:** 1.1  
**Target framework:** net10.0

---

## Table of contents

1. [Overview](#overview)
2. [Project structure](#project-structure)
3. [Quick start](#quick-start)
4. [Adding your own data type](#adding-your-own-data-type)
5. [Data access architecture](#data-access-architecture)
6. [Settings](#settings)
7. [Error handling](#error-handling)
8. [UI components](#ui-components)
9. [Core utilities](#core-utilities)
10. [Dependencies](#dependencies)

---

## Overview

The template separates the framework (`Core/`) from user code (`UserApps/`) so you can extend the app without touching the engine. The boot sequence is wired in `Program.cs` in three lines:

```csharp
CMSCore app = new();
var userApp = new UserApp(app);
app.RegisterUserMainMenu(() => userApp.ShowMainMenu());
app.RunApp();
```

The framework then:
- Loads `settings.json` (creating it with defaults on first run)
- Connects to MongoDB if configured, otherwise falls back to a local JSON file
- Calls your `ShowMainMenu()` in a crash-safe loop (`noCrashMode = true` auto-restarts on unhandled exceptions)

---

## Project structure

```
Template Menu Web Console/
├── Program.cs                      Boot entry point
├── settings.json                   Runtime settings file (auto-created)
│
├── Core/                           Framework — do not edit
│   ├── CMSCore.cs                  App lifecycle, menu loop, settings & DB init
│   ├── Globals.cs                  App-wide constants (AppName, version, etc.)
│   ├── AppSettings.cs              Settings entity (persisted via SettingsRepository)
│   ├── Helpers.cs                  Console I/O helpers (AskUsers, TryAskUsers, etc.)
│   │
│   ├── DataAccess/
│   │   ├── Services/
│   │   │   ├── IService.cs                     Service contract
│   │   │   ├── JsonFileService.cs + Settings    JSON file service + config class
│   │   │   └── MongoDBService.cs + Settings     MongoDB service + config class
│   │   └── Repository/
│   │       ├── IRepository.cs                  Repository contract
│   │       ├── RepositoryBase.cs               Default CRUD + cache delegation
│   │       └── SettingsRepository.cs           Built-in settings repository
│   │
│   ├── Errors/
│   │   ├── ErrorCode.cs            Error category enum
│   │   └── AppError.cs             Structured error with technical + user message
│   ├── Results/
│   │   └── Result.cs               Result / Result<T> outcome types
│   ├── Identity/
│   │   ├── IsIdAttribute.cs        [IsId] key annotation
│   │   ├── CompositeKey.cs         Multi-property key value type
│   │   └── EntityKeyResolver.cs   Reflection-based key extraction (cached)
│   │
│   └── UIComponents/
│       ├── UIComponent.cs          Abstract base for console UI components
│       ├── MenuChar.cs             Keyboard-driven menu component
│       └── SettingsComponent.cs   Built-in settings editor component
│
├── UserApps/                       Your application code lives here
│   ├── App/
│   │   └── UserApp.cs             Your menus and screen logic
│   ├── Classes/
│   │   └── UserClasses.cs         Your entity definitions
│   └── Repository/
│       └── RepositoryOuvrages.cs  Example domain repository
│
└── doc/
    ├── summary-guidelines.md       XML documentation style guide
    ├── userapp-guide.md            Guide for building user screens
    └── data-access-architecture.md Full data access layer reference
```

---

## Quick start

### Prerequisites

- .NET 10 SDK
- (Optional) A MongoDB Atlas cluster for remote persistence

### Run

```bash
dotnet run --project "Template Menu Web Console"
```

On first launch the app writes `settings.json` with default values and loads from a local `ouvrages.json` file. To switch to MongoDB, open **S › Paramètres** from the main menu and fill in the connection details.

---

## Adding your own data type

### 1 — Define an entity in `UserApps/Classes/`

```csharp
public class Product
{
    [IsId]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

For a composite key use `Order`:

```csharp
[IsId(Order = 0)] public string CategoryId { get; set; }
[IsId(Order = 1)] public string ProductId { get; set; }
```

> Entities without at least one `[IsId]` property will throw at service/repository construction — the check is intentionally fail-fast.

### 2 — Choose a service

**JSON file** (local, zero infrastructure):

```csharp
var settings = new JsonFileServiceSettings
{
    FilePath        = "products.json",
    CacheStaleAfter = TimeSpan.FromMinutes(10),
    JsonFormatting  = Formatting.Indented,
    CreateFileIfMissing = true
};
IService<Product> svc = new JsonFileService<Product>(settings);
```

**MongoDB**:

```csharp
var settings = new MongoDBServiceSettings
{
    Host           = "cluster0.example.mongodb.net",
    User           = "admin",
    Password       = "secret",
    DatabaseName   = "ShopDB",
    CollectionName = "Products",
    AppName        = "MyApp",
    CacheStaleAfter = TimeSpan.FromSeconds(30)
};
IService<Product> svc = new MongoDBService<Product>(settings);
```

### 3 — Create a minimal repository in `UserApps/Repository/`

```csharp
public class ProductRepository : RepositoryBase<Product>
{
    public ProductRepository(IService<Product> svc) : base(svc) { }

    // All of these are provided by RepositoryBase — nothing to implement:
    // Items, GetAll, GetById, Add, Update, UpdateById, Delete, DeleteById,
    // WriteAllToDataSource, ReadAllFromDataSource

    // Add domain helpers as needed:
    public List<Product> GetByPriceRange(decimal min, decimal max) =>
        Items.Where(p => p.Price >= min && p.Price <= max).ToList();
}
```

### 4 — Wire it in `UserApp.cs`

```csharp
var svc  = new JsonFileService<Product>(new JsonFileServiceSettings { FilePath = "products.json" });
var repo = new ProductRepository(svc);
var all  = repo.GetAll();
```

---

## Data access architecture

The stack has three layers. Only the outer two require any code from you.

```
UserApp  ──►  Repository  ──►  Service  ──►  Data source
               (yours)          (built-in)    (file / MongoDB)
```

| Layer | Your role | Framework role |
|---|---|---|
| **Entity** | Define properties + `[IsId]` | Key resolution via reflection |
| **Repository** | (Optional) add domain helpers | Full CRUD, key lookup, cache delegation |
| **Service** | Pick `JsonFileService` or `MongoDBService` + fill settings | Persistence, cache, stale TTL, error wrapping |

- The repository holds **no local copy** of the list — `Items` reads directly from the service cache.
- The service cache is invalidated when it exceeds `CacheStaleAfter`, or immediately after any write.
- All mutating operations return `Result` or `Result<T>`; exceptions are never used for expected failures.

See [doc/data-access-architecture.md](Template%20Menu%20Web%20Console/doc/data-access-architecture.md) for the full reference with Mermaid diagrams.

---

## Settings

`AppSettings` is persisted automatically to `settings.json` (path configured in `Globals.SettingsFile`). Access current settings anywhere via `Globals.Settings`.

| Property | Default | Purpose |
|---|---|---|
| `MongoEnabled` | `false` | Enables MongoDB on next startup |
| `MongoHost` | — | Atlas cluster hostname |
| `MongoPort` | `27017` | Port (informational) |
| `MongoUser` / `MongoDbPassword` | — | Atlas credentials |
| `MongoDatabase` / `MongoCollection` | — | Target database and collection |

Add your own fields directly to `AppSettings` — they are persisted and loaded automatically.

---

## Error handling

No exceptions are thrown for expected failures. Every CRUD method returns a `Result` or `Result<T>`:

Each log or error has a **severity** value (gravity). Use `Globals.LogSeverityThreshold` to suppress console output below a chosen level; lower-severity messages still appear in the log file. Severity numbers are arbitrary (e.g. 1=debug,100=info,500=error,1000=fatal).

All failures are expressed as `AppError` instances (no other exception types are raised for normal flow). Keep [`doc/error-severities.md`](doc/error-severities.md) up to date—it now lists every `AppError` creation with its severity so reviewers can ensure consistent logging gravity.


```csharp
var result = repo.Add(item);

if (!result.IsSuccess)
{
    // developer logs are written to devlogs.txt automatically when AppError
    // instances are created; console output is still performed via Logger.
    Logger.Error(result.Error!.TechnicalMessage);

    // User-friendly fallback string (French by default):
    Console.WriteLine(result.Error.ToUserMessage());
}
```

> **Log file**: a running record of `INFO`/`WARN`/`ERROR` messages (with their severity) is kept in `devlogs.txt` next to the executable. The file is ignored by git (`.gitignore`).  When exceptions are logged, their stack traces are truncated to a few lines (see `Logger.DefaultStackTraceLines`).


**`ErrorCode` values:**

| Code | Meaning |
|---|---|
| `Validation` | Input failed a validation rule |
| `NotFound` | Entity or resource does not exist |
| `Conflict` | Operation conflicts with current data state |
| `DataSource` | File or database error |
| `Timeout` | Operation exceeded time limit |
| `Configuration` | Invalid setup (e.g. missing `[IsId]`) |
| `Unknown` | Unexpected error |

Provide a custom user message at the call site when needed:

```csharp
Result.Failure(new AppError(ErrorCode.NotFound, "key '42' missing", "L'ouvrage demandé est introuvable."));
```

---

## UI components

The `Core/UIComponents/` folder provides a lightweight component model for console screens.

### `MenuChar`

Displays a list of labelled lines and dispatches key presses to actions:

```csharp
new MenuChar(
    ["1. List all", "Q. Quit"],
    ['1', 'q'],
    [() => ListAll(), () => Environment.Exit(0)]
).Run();
```

### `UIComponent` (base class)

Implement `Render()` and `ProcessInput()` to build custom screens. Call `Run()` to execute both in sequence.

### `SettingsComponent`

Built-in settings editor screen — registered automatically by `CMSCore`.

---

## Core utilities

### `Helpers.AskUsers<T>(prompt)`

Prompts the user and converts input to `T`. Handles `string`, numeric types, `bool`, `DateTime`, and `char` (single-key mode). Throws `InvalidCastException` on failure.

### `Helpers.TryAskUsers<T>(prompt, out T response)`

Same as above but swallows exceptions and returns `false` on failure. Prefer this in menu flow.

### `Globals`

| Member | Type | Value / Purpose |
|---|---|---|
| `AppName` | `const string` | `"Mongo console app"` |
| `AppVersion` | `const string` | `"1.1"` |
| `SettingsFile` | `const string` | `"settings.json"` |
| `noCrashMode` | `const bool` | Auto-restart on unhandled exception |
| `Settings` | `static AppSettings` | Current runtime settings |
| `AppDate` | `static string` | Build timestamp (computed at startup) |

---

## Dependencies

| Package | Version | Use |
|---|---|---|
| `Newtonsoft.Json` | 13.0.4 | JSON serialisation for `JsonFileService` |
| `MongoDB.Driver` | 3.6.0 | MongoDB client for `MongoDBService` |
| `MongoDB.Bson` | 3.6.0 | BSON type mapping |
