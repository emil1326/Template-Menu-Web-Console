using System;
using System.Collections.Generic;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// A simple settings UI component that displays labeled settings and allows editing.
    /// Uses the UIComponent base class to encapsulate render + logic.
    /// </summary>
    public class SettingsComponent : UIComponent
    {
        public record SettingEntry(string Label, Func<string> Getter, Action<string> Setter);

        private readonly List<SettingEntry> entries;
        private readonly Action? onFinish;

        public SettingsComponent(List<SettingEntry> entries, Action? onFinish = null)
        {
            this.entries = entries ?? new List<SettingEntry>();
            this.onFinish = onFinish;
            Title = "=== PARAMETRES ===";
        }

        public override void Render()
        {
            Helpers.ClearConsole();
            Console.WriteLine(Title);
            Console.WriteLine();

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                Console.WriteLine($"{i + 1}. {e.Label} : {e.Getter()}");
            }

            Console.WriteLine();
            Console.WriteLine("Entrez le numéro du champ à modifier, ou Q pour quitter");
        }

        public override void ProcessInput()
        {
            char key = char.ToLower(Console.ReadKey(true).KeyChar);
            Console.WriteLine();
            if (key == 'q')
            {
                onFinish?.Invoke();
                return;
            }

            if (int.TryParse(key.ToString(), out int idx) && idx >= 1 && idx <= entries.Count)
            {
                var entry = entries[idx - 1];
                Console.Write($"Nouvelle valeur pour '{entry.Label}' (vide = annuler) : ");
                string? val = Console.ReadLine();
                if (!string.IsNullOrEmpty(val))
                {
                    entry.Setter(val);
                    Console.WriteLine("[OK] Valeur mise à jour.");
                }
                else
                {
                    Console.WriteLine("[INFO] Modification annulée.");
                }
                Console.WriteLine("Appuyez sur Entrée pour revenir aux paramètres...");
                Console.ReadLine();
                Run();
            }
            else
            {
                Console.WriteLine("Entrée invalide.");
                Console.WriteLine("Appuyez sur Entrée pour réessayer...");
                Console.ReadLine();
                Run();
            }
        }
    }
}
