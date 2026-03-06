using System;
using System.Collections.Generic;
using EmilsWork.EmilsCMS;

internal class TextEditorApp : App
{
    private bool showLineNumbers = true;
    private int tabSize = 4;

    public override string DisplayName => "TextEditor";

    public TextEditorApp(CMSCore core)
        : base(core)
    {
    }

    public override IEnumerable<SettingsComponent.SettingEntry> GetSettingsEntries()
    {
        return new List<SettingsComponent.SettingEntry>
        {
            new("Afficher les numéros de ligne", () => showLineNumbers.ToString(), v => showLineNumbers = bool.TryParse(v, out var b) ? b : showLineNumbers),
            new("Taille de tabulation", () => tabSize.ToString(), v => tabSize = int.TryParse(v, out var i) ? i : tabSize)
        };
    }

    public override IEnumerable<string> GetInfoLines()
    {
        return new List<string>
        {
            "Module texte en préparation.",
            "Objectif: notes/édition rapide dans le terminal.",
            $"Config actuelle: line numbers={showLineNumbers}, tabSize={tabSize}.",
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
