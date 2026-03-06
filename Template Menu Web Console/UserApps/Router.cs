using System;
using System.Collections.Generic;
using EmilsWork.EmilsCMS;

/// <summary>
/// Top‑level application router/menu.  This sits outside of the domain-specific
/// menu (the old "main menu") so additional modules (text editor, etc.) can
/// be added without touching the user app logic.
/// </summary>
internal class Router : App
{
        private readonly UserApp userApp;
        private readonly TextEditorApp textEditorApp;
        private bool showBanner = true;

        public Router(CMSCore core, UserApp userApp, TextEditorApp textEditorApp)
            : base(core)
        {
            this.userApp = userApp ?? throw new ArgumentNullException(nameof(userApp));
            this.textEditorApp = textEditorApp ?? throw new ArgumentNullException(nameof(textEditorApp));

            // Router owns module apps; this enables recursive app hierarchy.
            RegisterSubApp(this.userApp);
            RegisterSubApp(this.textEditorApp);
        }

        public override string DisplayName => "Router";

        public override IEnumerable<SettingsComponent.SettingEntry> GetSettingsEntries()
        {
            return new List<SettingsComponent.SettingEntry>
            {
                new("Afficher bannière globale", () => showBanner.ToString(), v =>
                {
                    showBanner = bool.TryParse(v, out var b) ? b : showBanner;
                    Core.PersistSettings();
                }),

                new("=== MongoDB (Core) ===", () => string.Empty, _ => { }, IsEditable: false),

                new("Mongo activé", () => Globals.Settings.MongoEnabled.ToString(), v =>
                {
                    Globals.Settings.MongoEnabled = bool.TryParse(v, out var b) ? b : Globals.Settings.MongoEnabled;
                    Core.PersistSettings();
                }),

                new("Mongo host", () => Globals.Settings.MongoHost ?? string.Empty, v =>
                {
                    Globals.Settings.MongoHost = string.IsNullOrWhiteSpace(v) ? null : v;
                    Core.PersistSettings();
                }),

                new("Mongo port", () => Globals.Settings.MongoPort.ToString(), v =>
                {
                    if (int.TryParse(v, out var p))
                    {
                        Globals.Settings.MongoPort = p;
                        Core.PersistSettings();
                    }
                }),

                new("Mongo user", () => Globals.Settings.MongoUser ?? string.Empty, v =>
                {
                    Globals.Settings.MongoUser = string.IsNullOrWhiteSpace(v) ? null : v;
                    Core.PersistSettings();
                }),

                new("Mongo password", () => Globals.Settings.MongoDbPassword ?? string.Empty, v =>
                {
                    Globals.Settings.MongoDbPassword = string.IsNullOrWhiteSpace(v) ? null : v;
                    Core.PersistSettings();
                }),

                new("Mongo database", () => Globals.Settings.MongoDatabase ?? string.Empty, v =>
                {
                    Globals.Settings.MongoDatabase = string.IsNullOrWhiteSpace(v) ? null : v;
                    Core.PersistSettings();
                }),

                new("Mongo collection", () => Globals.Settings.MongoCollection ?? string.Empty, v =>
                {
                    Globals.Settings.MongoCollection = string.IsNullOrWhiteSpace(v) ? null : v;
                    Core.PersistSettings();
                })
            };
        }

        public override IEnumerable<string> GetInfoLines()
        {
            return new List<string>
            {
                "Router principal de l'application.",
                "Gère la navigation entre modules.",
                "Raccourcis: Ctrl+B (back), Ctrl+H (home).",
                $"Application: {Globals.AppName} v{Globals.AppVersion}",
                $"Compagnie: {Globals.Compagnie}",
                $"Créateur: {Globals.Createur}",
                $"Dernier build: {Globals.AppDate}",
                $"Fichier settings: {Globals.SettingsFile}",
                $"Mongo activé: {Globals.Settings.MongoEnabled}",
                $"Mongo cible: {Globals.Settings.MongoHost ?? "(vide)"}:{Globals.Settings.MongoPort} / {Globals.Settings.MongoDatabase ?? "(vide)"}.{Globals.Settings.MongoCollection ?? "(vide)"}"
            };
        }

        /// <summary>
        /// Display the primary menu for the whole application.
        /// </summary>
        public void ShowMainMenu(bool showError = false)
        {
            var page = new MenuPage(
                title: "=== MENU PRINCIPAL ===",
                description: "Sélectionnez un module:",
                onRenderPrefix: () =>
                {
                    if (showBanner)
                    {
                        Console.WriteLine(Globals.AppHeader);
                        Console.WriteLine($"=== {Globals.AppName} v{Globals.AppVersion} ===");
                        Console.WriteLine();
                    }
                },
                options:
                [
                    new MenuPage.MenuOption('1', "Ouvrages (exemple métier)", () => userApp.ShowOuvragesMenu()),
                    new MenuPage.MenuOption('2', "Editeur de texte (bientôt)", () => textEditorApp.ShowPlaceholder(() => ShowMainMenu())),
                    MenuPage.MenuOption.Space(),
                    MenuPage.MenuOption.Separator(),
                    new MenuPage.MenuOption('s', "Paramètres (scope Router)", () => ShowScopedSettingsPage(() => ShowMainMenu())),
                    new MenuPage.MenuOption('i', "Informations (scope Router)", () => ShowScopedInfoPage(() => ShowMainMenu())),
                    new MenuPage.MenuOption('q', "Quitter", CMSCore.ExitApp)
                ]);

            if (showError)
            {
                page.RunWithError();
                return;
            }

            page.Run();
        }

}
