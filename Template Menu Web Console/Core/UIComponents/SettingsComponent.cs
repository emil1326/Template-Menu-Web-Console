using System;
using System.Collections.Generic;
using System.Linq;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// A simple settings UI component that displays labeled settings and allows editing.
    /// Uses the UIComponent base class to encapsulate render + logic.
    /// </summary>
    public class SettingsComponent : UIComponent
    {
        public record SettingEntry(string Label, Func<string> Getter, Action<string> Setter, bool IsEditable = true);

        private readonly List<SettingEntry> entries;
        private readonly Action? onFinish;
        private List<SettingEntry> editableEntries = new();

        public SettingsComponent(List<SettingEntry> entries, Action? onFinish = null, string? title = null)
        {
            this.entries = entries ?? [];
            this.onFinish = onFinish;
            Title = string.IsNullOrWhiteSpace(title) ? "=== PARAMETRES ===" : title;
        }

        public override void Render()
        {
            Helpers.ClearConsole();
            Console.WriteLine(Title);
            Console.WriteLine();

            editableEntries = entries.Where(x => x.IsEditable).ToList();

            int visibleIndex = 1;
            foreach (var e in entries)
            {
                if (!e.IsEditable)
                {
                    Console.WriteLine(e.Label);
                    continue;
                }

                Console.WriteLine($"{visibleIndex}. {e.Label} : {e.Getter()}");
                visibleIndex++;
            }

            Console.WriteLine();
            Console.WriteLine("Entrez le numéro du champ à modifier (ex: 10), puis Entrée. Q pour revenir.");
        }

        public override void ProcessInput()
        {
            while (true)
            {
                var input = Helpers.ReadLineWithShortcuts("> ", inline: true);
                if (input == null)
                {
                    return;
                }

                input = input.Trim();
                if (string.Equals(input, "q", StringComparison.OrdinalIgnoreCase))
                {
                    onFinish?.Invoke();
                    return;
                }

                if (!int.TryParse(input, out int idx) || idx < 1 || idx > editableEntries.Count)
                {
                    Logger.Warn("Entrée invalide dans paramètres");
                    Console.WriteLine("Entrée invalide.");
                    continue;
                }

                var entry = editableEntries[idx - 1];

                // Bool settings toggle directly when selected (no extra prompt).
                var currentValue = entry.Getter();
                if (bool.TryParse(currentValue, out var currentBool))
                {
                    var toggled = !currentBool;
                    entry.Setter(toggled.ToString());
                    Logger.Log($"Valeur booléenne inversée: {entry.Label}={toggled}");
                    Console.WriteLine($"[OK] '{entry.Label}' => {toggled}");
                    Run();
                    return;
                }

                var val = Helpers.ReadLineWithShortcuts($"Nouvelle valeur pour '{entry.Label}' (vide = annuler) : ", inline: true);
                if (val == null)
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(val))
                {
                    entry.Setter(val);
                    Logger.Log("Valeur mise à jour.");
                    Console.WriteLine("[OK] Valeur mise à jour.");
                }
                else
                {
                    Logger.Log("Modification annulée.");
                    Console.WriteLine("[INFO] Modification annulée.");
                }
                Run();
                return;
            }
        }
    }
}
