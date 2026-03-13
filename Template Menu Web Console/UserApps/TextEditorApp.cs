using System;
using System.Collections.Generic;
using EmilsWork.EmilsCMS;

internal class TextEditorApp : App
{
    public override string DisplayName => "TextEditor";

    public TextEditorApp(CMSCore core)
        : base(core)
    {
    }

    public override IEnumerable<SettingsValues> GetSettingsValues()
    {
        return
        [
            new TextEditorSettingsValues()
        ];
    }

    public override IEnumerable<DisplaySettingsPage> GetDisplaySettingsPages()
    {
        return
        [
            new DisplaySettingsPage
            {
                PageKey = TextEditorSettingsValues.Key,
                Title = "=== TextEditor ===",
                Entries =
                [
                    new SettingDisplayEntry
                    {
                        FieldKey = nameof(TextEditorSettingsValues.ShowLineNumbers),
                        Label = "Afficher les numéros de ligne",
                        PromptText = "Afficher les numéros de ligne?",
                        ToggleBooleanDirectly = true
                    },
                    new SettingDisplayEntry
                    {
                        FieldKey = nameof(TextEditorSettingsValues.TabSize),
                        Label = "Taille de tabulation",
                        PromptText = "Taille de tabulation"
                    }
                ]
            }
        ];
    }

    public override IEnumerable<string> GetInfoLines()
    {
        var settings = SettingsComponent.GetOrCreatePage<TextEditorSettingsValues>(Globals.GlobalSettings);

        return new List<string>
        {
            "Module texte en préparation.",
            "Objectif: notes/édition rapide dans le terminal.",
            $"Config actuelle: line numbers={settings.ShowLineNumbers}, tabSize={settings.TabSize}.",
            $"Application: {Globals.AppName} v{Globals.AppVersion}",
            $"Compagnie: {Globals.Compagnie}"
        };
    }

    public void ShowPlaceholder(Action onBack)
    {
        Helpers.ClearConsole();
        Logger.Warn("Text editor not implemented");
        Console.WriteLine("=== EDITEUR DE TEXTE ===");
        Console.WriteLine();
        Console.WriteLine("Module à venir.");
        Console.WriteLine("Ctrl+H = retour accueil, Ctrl+B = retour page précédente.");
        Console.WriteLine();
        if (!Helpers.WaitForContinue("Appuyez sur Entrée pour revenir..."))
            return;
        onBack();
    }
}

internal sealed class TextEditorSettingsValues : SettingsValues
{
    public const string Key = "userapp.texteditor";
    public override string PageKey => Key;
    public bool ShowLineNumbers { get; set; } = true;
    public int TabSize { get; set; } = 4;
}
