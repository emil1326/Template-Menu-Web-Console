# Creating and Registering a UserApp

This guide explains how to create a new user application (UserApp) and register it with the core.

1. Create a new class that provides a method to show the main menu. Example file path: `UserApps/MyUserApp.cs`.

2. Keep the namespace `EmilsWork.EmilsCMS` so it integrates with existing helpers and domain classes.

3. The class should accept a `CMSCore` instance in its constructor so it can access core services:

   - `CMSCore.RegisterUserMainMenu(Action)` — register a callback that the core will call to show the user's menu.
   - `CMSCore.Ouvrages` — access the repository for domain data.

4. Example pattern:

   - Add a method `public void ShowMainMenu()` that builds a `MenuChar` and calls `CMSCore.ProcessMenuInput(menu)`.
   - In `Program.cs`, instantiate your `CMSCore` then instantiate your `UserApp` and call `app.RegisterUserMainMenu(user.ShowMainMenu)`.

5. Keep domain classes (e.g., `Ouvrage`, `Livre`) in the `EmilsWork.EmilsCMS` namespace so both core and user apps can reference them without changes.

Notes:
- Prefer registration over reflection — it's explicit and easier to test.
- User apps should avoid calling `Environment.Exit` directly; use `CMSCore.ExitApp()` when necessary.

## Repository / Service Convention (v2)

The data flow now follows this rule:

1. User defines a data class in `UserApps/Classes` and marks its key property with `[IsId]`.
2. User chooses an existing service (`JsonFileService<TEntity>`, `MongoDBService<TEntity>`) or creates a new one for another source.
3. User creates a repository class that inherits `RepositoryBase<TEntity>`.
4. For basic usage, the repository can be almost empty and still gets default CRUD + sync behavior.

### ID annotation

- Use `[IsId]` on the key property.
- Composite keys are supported by putting `[IsId(Order = ...)]` on multiple properties.
- If no `[IsId]` is found, startup/configuration fails fast.

Example:

```csharp
public class Ouvrage
{
      [IsId]
      public string Id { get; set; } = string.Empty;
      public string Titre { get; set; } = string.Empty;
}
```

### Error handling convention

- Expected failures return `Result` / `Result<T>` with an `AppError`.
- `AppError` includes a technical message and an optional user message.
- UI should display `error.ToUserMessage()` when no custom message is provided.

### Service cache/stale policy

- Caching is service-owned (not repository-owned).
- Repository methods support cache-aware reads and explicit sync:
   - `GetAll(useCache: true)`
   - `GetById(id, useCache: false)` to force fresh read
   - `ReadAllFromDataSource()` / `WriteAllToDataSource()`
