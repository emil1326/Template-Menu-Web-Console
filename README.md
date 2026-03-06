# Template Menu Web Console

Template de console .NET 10 avec architecture modulaire `App`/`Page`, navigation clavier unifiee, data-access en couches (Repository/Service), et gestion d'erreurs standardisee via `AppError`.

## Etat actuel (mars 2026)

Le projet suit maintenant ce modele:

- Un `Router` racine (app principale).
- Des apps enfants (ex: `UserApp`, `TextEditorApp`) en hierarchie recursive illimitee.
- Chaque app expose ses `Settings` et `Info` au niveau racine de l'app (pas sur chaque page).
- Les pages internes d'une app servent uniquement au flux metier (CRUD, recherche, etc.).

## Navigation

- `q`: quitte uniquement depuis le menu principal (Router).
- `q` dans les autres pages: retour en arriere.
- `Ctrl+B`: retour force vers la page precedente. Si aucun retour possible, message d'avertissement puis retour a la racine de page.
- `Ctrl+H`: retour accueil (Router).

## Parametres (UX actuelle)

- Les pages de parametres lisent maintenant une ligne complete pour la selection: `10`, `11`, etc. sont supportes.
- Si la valeur d'un setting est un bool (`true`/`false`), choisir son numero inverse directement la valeur.
- Les confirmations "Appuyez sur Entree" apres edition ont ete retirees pour rendre le flux plus rapide.
- Les parametres MongoDB sont exposes dans le scope `Router` (plus dans un menu legacy de `CMSCore`).
- Les modifications de settings sont persistantes (sauvegarde immediate).

## Structure

```text
Template Menu Web Console/
|- Program.cs
|- Template Menu Web Console.csproj
|- Core/
|  |- CMSCore.cs
|  |- App.cs
|  |- Helpers.cs
|  |- UIComponents/
|  |  |- MenuPage.cs
|  |  |- MenuChar.cs
|  |  |- SettingsComponent.cs
|  |- DataAccess/
|  |- Errors/
|  |- Results/
|  \- Identity/
|- UserApps/
|  |- Router.cs
|  |- App/UserApp.cs
|  |- TextEditorApp.cs
|  |- Classes/
|  \- Repository/
\- doc/
   |- userapp-guide.md
   |- data-access-architecture.md
   |- error-guidelines.md
   \- summary-guidelines.md
```

## Bootstrap

`Program.cs` compose le graphe d'apps:

```csharp
CMSCore app = new();
var userApp = new UserApp(app);
var textEditorApp = new TextEditorApp(app);
var router = new Router(app, userApp, textEditorApp);

app.RegisterUserMainMenu(() => router.ShowMainMenu());
app.RegisterChildApp(router);
app.RunApp();
```

## Contrats importants

- Toute app derive de `App`.
- Les apps peuvent enregistrer des sous-apps via `RegisterSubApp`.
- Les ecrans settings/info scopes sont fournis par `App`:
  - `ShowScopedSettingsPage(...)`
  - `ShowScopedInfoPage(...)`
- `GetSettingsEntries()` et `GetInfoLines()` sont agreges recursivement (self + descendants).

## Data access (resume)

- Entites: classes metier avec `[IsId]`.
- Repositories: derivent de `RepositoryBase<TEntity>`.
- Services: `JsonFileService<TEntity>` ou `MongoDBService<TEntity>`.
- Retour d'erreurs attendues: `Result` / `Result<T>` avec `AppError`.

Voir `doc/data-access-architecture.md` pour les details.

## Error handling (resume)

- Les erreurs metier/techniques significatives doivent etre representees en `AppError`.
- Les `AppError` sont loggees de maniere uniforme et affichables en UI.
- Eviter les exceptions brutes dans le flux normal.

Voir `doc/error-guidelines.md` pour les conventions completes.

## Build

Depuis `Template Menu Web Console/`:

```bash
dotnet build
```

## Debug VS Code (F5)

- `F5` utilise un launch `coreclr` en `externalTerminal`.
- Avant lancement, VS Code execute la task `clean-build-template-menu`:
  - `dotnet clean`
  - `dotnet build`
- Si une session debug est deja active, utiliser `Ctrl+Shift+F5` pour un restart propre.
