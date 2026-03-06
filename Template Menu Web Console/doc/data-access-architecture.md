# Data Access Architecture

The data stack remains layered and independent from menu/page navigation.
This document now also clarifies how data concerns map to the App hierarchy.

## 1) Layers

```text
App/Page UI -> Repository -> Service -> Data Source
```

- Entity layer: your domain types (`UserApps/Classes`), key marked with `[IsId]`.
- Repository layer: domain access API (`RepositoryBase<TEntity>` + optional custom methods).
- Service layer: persistence and cache (`JsonFileService<TEntity>`, `MongoDBService<TEntity>`).

## 2) Responsibilities

- UI/App pages call repositories only.
- Repositories orchestrate domain operations and delegate persistence to services.
- Services own cache lifecycle and source IO.
- No page should directly manage JSON/Mongo details.

## 3) IDs and key resolution

- `[IsId]` is mandatory on at least one property.
- Multiple `[IsId(Order = ...)]` properties produce a `CompositeKey`.
- Missing key annotation is a configuration error.

## 4) Result/AppError contract

Expected failures should flow through:

- `Result` / `Result<T>`
- `AppError`

This keeps behavior consistent for:

- logs (`Logger`)
- user-facing messages (`ToUserMessage`)
- severity filtering (`Globals.LogSeverityThreshold`)

## 5) Cache behavior

- Cache is service-owned.
- Repository reads can be cache-aware (`useCache`).
- Writes update data source and synchronize cache.

## 6) App hierarchy integration

App tree architecture (`Router` -> domain apps -> sub-apps) does not change data rules:

- each app can use one or more repositories
- parent apps can aggregate settings/info of child apps
- data access remains encapsulated in repositories/services

In short: app hierarchy is for navigation and ownership, not for bypassing repository boundaries.

## 7) Minimal example

```csharp
public class Book
{
    [IsId]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
}

public class BookRepository : RepositoryBase<Book>
{
    public BookRepository(IService<Book> svc) : base(svc) { }
}

var svc = new JsonFileService<Book>(new JsonFileServiceSettings
{
    FilePath = "books.json"
});

var repo = new BookRepository(svc);
var result = repo.Add(new Book { Title = "DDD" });
```

## 8) Review checklist

- Entity has `[IsId]`.
- Repository extends `RepositoryBase<TEntity>`.
- Service selection matches deployment needs (JSON vs Mongo).
- No direct data-source calls from UI pages.
- Failure path returns `Result` + `AppError`.
