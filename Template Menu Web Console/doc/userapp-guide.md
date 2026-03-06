# UserApp Guide (Current Architecture)

This guide explains how to create a new app module and integrate it in the current router/app tree.

## 1) Mental model

- `Router` is the root app.
- Domain modules (`UserApp`, `TextEditorApp`, etc.) are child apps.
- Each app can have sub-apps (recursive, unlimited depth).
- An app is a collection of pages, but settings/info belong to the app itself.

## 2) App contract

Create an app by inheriting from `App`:

```csharp
internal class CatalogApp : App
{
    public CatalogApp(CMSCore core) : base(core) {}

    public override string DisplayName => "Catalog";

    public override IEnumerable<SettingsComponent.SettingEntry> GetSettingsEntries()
    {
        return
        [
            new("Enable feature X", () => featureX.ToString(), v => featureX = bool.TryParse(v, out var b) ? b : featureX)
        ];
    }

    public override IEnumerable<string> GetInfoLines()
    {
        return
        [
            "Catalog module.",
            "Owns browse and management pages."
        ];
    }

    public void ShowRootMenu()
    {
        new MenuPage(
            title: "=== CATALOG ===",
            description: "Choose an action:",
            options:
            [
                new MenuPage.MenuOption('1', "Browse", ShowBrowsePage),
                new MenuPage.MenuOption('2', "Manage", ShowManagePage),
                new MenuPage.MenuOption('s', "Settings (Catalog scope)", () => ShowScopedSettingsPage(ShowRootMenu)),
                new MenuPage.MenuOption('i', "Info (Catalog scope)", () => ShowScopedInfoPage(ShowRootMenu)),
                new MenuPage.MenuOption('q', "Back", () => Core.MainMenu())
            ])
            .Run();
    }
}
```

## 3) Settings/Info placement rule

Important rule for UX consistency:

- Add `s`/`i` only on app root menus.
- Do not repeat `s`/`i` on every CRUD/search/detail page.
- Parent apps can still manage descendant settings via scoped aggregation.
- Infrastructure settings (ex: MongoDB) belong to `Router` scope.

## 3.1) Settings interaction behavior

- Settings page accepts multi-digit selection (`10`, `11`, etc.).
- Bool settings toggle immediately when selected.
- Non-bool settings prompt for a new value.
- There is no blocking confirmation pause after each change.

## 4) Register apps in Program.cs

```csharp
CMSCore app = new();

var userApp = new UserApp(app);
var textEditorApp = new TextEditorApp(app);
var router = new Router(app, userApp, textEditorApp);

app.RegisterUserMainMenu(() => router.ShowMainMenu());
app.RegisterChildApp(router);
app.RunApp();
```

## 5) Register child apps inside parent apps

In parent app constructor:

```csharp
RegisterSubApp(childA);
RegisterSubApp(childB);
```

This enables:

- recursive init/cleanup
- recursive settings aggregation
- recursive info aggregation

## 6) Navigation rules to respect

- Root router page: `q` = quit.
- Any non-root page: `q` = back.
- `Ctrl+B` = back; if unavailable, show warning and replay page root.
- `Ctrl+H` = home.

## 7) Data and errors from app pages

Inside app pages:

- Use repositories for data access.
- Handle failures through `Result` / `Result<T>` and `AppError`.
- Keep page methods focused on user flow.

## 8) Quick checklist for a new app

- Inherits from `App`.
- Implements `DisplayName`.
- Implements `GetSettingsEntries` / `GetInfoLines`.
- Has one root menu with `s`/`i` actions.
- Registers child apps if needed.
- Uses `q` as back on internal pages.
- Does not duplicate settings/info actions on child pages.
