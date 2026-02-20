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
