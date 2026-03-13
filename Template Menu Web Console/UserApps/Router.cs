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

        public override IEnumerable<SettingsValues> GetSettingsValues()
        {
            return
            [
                new RouterSettingsValues(),
                new CoreMongoSettingsValues()
            ];
        }

        public override IEnumerable<DisplaySettingsPage> GetDisplaySettingsPages()
        {
            return
            [
                new DisplaySettingsPage
                {
                    PageKey = RouterSettingsValues.Key,
                    Title = "=== Router ===",
                    Entries =
                    [
                        new SettingDisplayEntry
                        {
                            FieldKey = nameof(RouterSettingsValues.ShowBanner),
                            Label = "Afficher bannière globale",
                            PromptText = "Afficher bannière globale?",
                            ToggleBooleanDirectly = true
                        }
                    ]
                },
                new DisplaySettingsPage
                {
                    PageKey = CoreMongoSettingsValues.Key,
                    Title = "=== MongoDB (Core) ===",
                    Entries =
                    [
                        new SettingDisplayEntry { FieldKey = nameof(CoreMongoSettingsValues.MongoEnabled), Label = "Mongo activé", PromptText = "Mongo activé?", ToggleBooleanDirectly = true },
                        new SettingDisplayEntry { FieldKey = nameof(CoreMongoSettingsValues.MongoHost), Label = "Mongo host", PromptText = "Mongo host" },
                        new SettingDisplayEntry { FieldKey = nameof(CoreMongoSettingsValues.MongoPort), Label = "Mongo port", PromptText = "Mongo port" },
                        new SettingDisplayEntry { FieldKey = nameof(CoreMongoSettingsValues.MongoUser), Label = "Mongo user", PromptText = "Mongo user" },
                        new SettingDisplayEntry { FieldKey = nameof(CoreMongoSettingsValues.MongoDbPassword), Label = "Mongo password", PromptText = "Mongo password" },
                        new SettingDisplayEntry { FieldKey = nameof(CoreMongoSettingsValues.MongoDatabase), Label = "Mongo database", PromptText = "Mongo database" },
                        new SettingDisplayEntry { FieldKey = nameof(CoreMongoSettingsValues.MongoCollection), Label = "Mongo collection", PromptText = "Mongo collection" }
                    ]
                }
            ];
        }

        public override IEnumerable<string> GetInfoLines()
        {
            var routerSettings = SettingsComponent.GetOrCreatePage<RouterSettingsValues>(Globals.GlobalSettings);
            var mongo = SettingsComponent.GetOrCreatePage<CoreMongoSettingsValues>(Globals.GlobalSettings);

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
                $"Bannière globale: {routerSettings.ShowBanner}",
                $"Mongo activé: {mongo.MongoEnabled}",
                $"Mongo cible: {mongo.MongoHost ?? "(vide)"}:{mongo.MongoPort} / {mongo.MongoDatabase ?? "(vide)"}.{mongo.MongoCollection ?? "(vide)"}"
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
                    var routerSettings = SettingsComponent.GetOrCreatePage<RouterSettingsValues>(Globals.GlobalSettings);
                    if (routerSettings.ShowBanner)
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

internal sealed class RouterSettingsValues : SettingsValues
{
    public const string Key = "router.main";
    public override string PageKey => Key;
    public bool ShowBanner { get; set; } = true;
}
