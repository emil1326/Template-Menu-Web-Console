# Data Access Architecture

The data access system is split into three layers: **Entity**, **Repository**, and **Service**. Each layer has a single responsibility, and the user only needs to define the bottom and top of the stack — the middle is handled by the framework.

---

## Layer Overview

```mermaid
flowchart TB
    subgraph UserApp["User Application"]
        direction TB
        Entity["MyEntity\n──────────────\n[IsId] string Id\nstring Name\n..."]
        UserRepo["MyRepository : RepositoryBase&lt;MyEntity&gt;\n──────────────\nDomain helpers built on top of\nGetAll / Add / Update / Delete"]
    end

    subgraph Core["Core — DataAccess"]
        direction TB
        subgraph RepoLayer["Repository Layer"]
            IRepo["IRepository&lt;TEntity&gt;"]
            RepoBase["RepositoryBase&lt;TEntity&gt;\n──────────────\nGetAll · GetById\nAdd · Update\nDelete · DeleteById\nUpdateById\nWriteAll · ReadAll"]
        end
        subgraph SvcLayer["Service Layer"]
            ISvc["IService&lt;TEntity&gt;"]
            JsonSvc["JsonFileService&lt;TEntity&gt;\n+ JsonFileServiceSettings\n──────────────\nFilePath · CacheStaleAfter\nJsonFormatting\nCreateFileIfMissing"]
            MongoSvc["MongoDBService&lt;TEntity&gt;\n+ MongoDBServiceSettings\n──────────────\nHost · User · Password\nDatabaseName · CollectionName\nCacheStaleAfter"]
        end
        subgraph InfraLayer["Infrastructure"]
            KeyResolver["EntityKeyResolver&lt;TEntity&gt;\nResolves [IsId] at runtime"]
            Result["Result / Result&lt;T&gt;\nAppError · ErrorCode"]
        end
    end

    subgraph DataSources["Data Sources"]
        File["JSON file"]
        Mongo["MongoDB"]
    end

    UserRepo -- extends --> RepoBase
    RepoBase -- implements --> IRepo
    UserRepo -. "injects IService at construction" .-> RepoBase
    RepoBase -- "delegates CRUD + cache" --> ISvc
    JsonSvc -- implements --> ISvc
    MongoSvc -- implements --> ISvc
    JsonSvc -- reads / writes --> File
    MongoSvc -- reads / writes --> Mongo
    RepoBase -. "validates key config" .-> KeyResolver
    Entity -. "[IsId] annotation" .-> KeyResolver
    RepoBase -- returns --> Result
    ISvc -- returns --> Result
```

---

## Request Flow — Add an entity

The sequence below shows what happens when user code calls `myRepo.Add(entity)`.

```mermaid
sequenceDiagram
    actor User as User Code
    participant Repo as MyRepository
    participant Svc as IService (Json or Mongo)
    participant DS as Data Source (file or DB)

    User->>Repo: Add(entity)
    Repo->>Svc: Add(entity)
    Svc->>DS: persist entity
    DS-->>Svc: ok / error
    Svc->>Svc: update in-memory cache
    Svc-->>Repo: Result
    Repo-->>User: Result

    note over Svc,DS: Cache is always updated together with the data source write.

    User->>Repo: Items (property)
    Repo->>Svc: ReadAll(useCache: true)
    Svc-->>Repo: cached list
    Repo-->>User: IReadOnlyList
```

---

## How to add a new entity type

### 1 — Define the entity and mark its key

```csharp
public class Book
{
    [IsId]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Title { get; set; } = string.Empty;
    public int Year { get; set; }
}
```

For composite keys, add `Order`:

```csharp
[IsId(Order = 0)] public string LibraryId { get; set; }
[IsId(Order = 1)] public string BookId { get; set; }
```

---

### 2 — Choose a service and configure its settings

**JSON file** (local persistence):

```csharp
var settings = new JsonFileServiceSettings
{
    FilePath = "books.json",
    CacheStaleAfter = TimeSpan.FromMinutes(10),
    JsonFormatting = Formatting.Indented,
    CreateFileIfMissing = true
};
IService<Book> svc = new JsonFileService<Book>(settings);
```

**MongoDB**:

```csharp
var settings = new MongoDBServiceSettings
{
    Host = "cluster.mongodb.net",
    User = "admin",
    Password = "secret",
    DatabaseName = "LibraryDB",
    CollectionName = "Books",
    AppName = "MyApp",
    CacheStaleAfter = TimeSpan.FromSeconds(30)
};
IService<Book> svc = new MongoDBService<Book>(settings);
```

The `Settings` property on both services is **mutable at runtime** — changes take effect on the next operation.

---

### 3 — Create a minimal repository

```csharp
public class BookRepository : RepositoryBase<Book>
{
    public BookRepository(IService<Book> svc) : base(svc) { }

    // Optional domain helpers:
    public List<Book> GetByYear(int year) =>
        Items.Where(b => b.Year == year).ToList();
}
```

That's it. `Add`, `Update`, `Delete`, `DeleteById`, `UpdateById`, `GetAll`, `GetById`, `Items`, `WriteAllToDataSource`, `ReadAllFromDataSource` are all provided by `RepositoryBase`.

---

## Key resolution rules

| Scenario | Result |
|---|---|
| One `[IsId]` property | Key is that property's value (`object`) |
| Multiple `[IsId]` properties | Key is a `CompositeKey` ordered by `Order` |
| No `[IsId]` property | Constructor throws `InvalidOperationException` at startup |

`EntityKeyResolver<TEntity>` caches the reflected `PropertyInfo[]` statically — resolution is only paid once per type.

---

## Error handling

All mutating methods return `Result` or `Result<T>`. No exceptions are thrown for expected failures.

```csharp
var result = repo.Add(book);
if (!result.IsSuccess)
{
    Console.WriteLine(result.Error!.ToUserMessage());
}
```

`AppError` carries:
- `ErrorCode` — category (`NotFound`, `Conflict`, `DataSource`, `Validation`, `Configuration`, `Timeout`, `Unknown`)
- `TechnicalMessage` — always set, developer-facing
- `UserMessage` — optional override; `ToUserMessage()` falls back to a default per `ErrorCode`
