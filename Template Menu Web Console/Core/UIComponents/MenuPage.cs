using System;
using System.Collections.Generic;
using System.Linq;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Page-oriented menu component with built-in invalid-input refresh handling.
    /// </summary>
    public sealed class MenuPage : UIComponent
    {
        public sealed record MenuOption(char Key, string Label, Action Action, bool IsDecoration = false)
        {
            public static MenuOption Space() => new('\0', string.Empty, () => { }, true);
            public static MenuOption Separator(string text = "-----------") => new('\0', text, () => { }, true);
        }

        private readonly List<MenuOption> options;
        private readonly Action? baseRenderPrefix;
        private readonly Action? transientRenderPrefix;

        public string? Description { get; init; }

        public MenuPage(
            string title,
            IEnumerable<MenuOption> options,
            string? description = null,
            Action? onRenderPrefix = null)
        {
            Title = title;
            this.options = options?.ToList() ?? new List<MenuOption>();
            Description = description;
            baseRenderPrefix = onRenderPrefix;
            transientRenderPrefix = null;
        }

        private MenuPage(
            string title,
            IEnumerable<MenuOption> options,
            string? description,
            Action? baseRenderPrefix,
            Action? transientRenderPrefix)
        {
            Title = title;
            this.options = options?.ToList() ?? new List<MenuOption>();
            Description = description;
            this.baseRenderPrefix = baseRenderPrefix;
            this.transientRenderPrefix = transientRenderPrefix;
        }

        public override void Render()
        {
            Helpers.ClearConsole();
            baseRenderPrefix?.Invoke();
            transientRenderPrefix?.Invoke();

            if (!string.IsNullOrWhiteSpace(Title))
            {
                Console.WriteLine(Title);
                Console.WriteLine();
            }

            if (!string.IsNullOrWhiteSpace(Description))
            {
                Console.WriteLine(Description);
                Console.WriteLine();
            }

            foreach (var option in options)
            {
                if (option.IsDecoration || option.Key == '\0')
                {
                    Console.WriteLine(option.Label);
                    continue;
                }

                Console.WriteLine($"{char.ToUpper(option.Key)}. {option.Label}");
            }
        }

        public override void ProcessInput()
        {
            var key = Console.ReadKey(true);
            if ((key.Modifiers & ConsoleModifiers.Control) != 0 && key.Key == ConsoleKey.H)
            {
                Logger.Log($"Home shortcut from {Title}");
                if (CMSCore.Current != null)
                {
                    CMSCore.Current.MainMenu();
                }
                return;
            }

            if ((key.Modifiers & ConsoleModifiers.Control) != 0 && key.Key == ConsoleKey.B)
            {
                Logger.Log($"Back shortcut from {Title}");
                if (!NavigationHistory.TryBack())
                {
                    NavigationHistory.BackUnavailableFallback($"MenuPage:{Title}");
                }
                return;
            }

            char input = char.ToLower(key.KeyChar);

            foreach (var option in options)
            {
                if (option.IsDecoration || option.Key == '\0')
                {
                    continue;
                }

                if (char.ToLower(option.Key) != input)
                {
                    continue;
                }

                option.Action();
                return;
            }

            Logger.Warn($"Invalid menu input '{input}' in {Title}");
            RunWithError();
        }

        public override void Run()
        {
            NavigationHistory.Push($"MenuPage:{Title}:{Description}", Run);
            base.Run();
        }

        public void RunWithError()
        {
            RunWithPrefix(() =>
            {
                Console.WriteLine("[!] Commande non reconnue");
                Console.WriteLine();
            });
        }

        private void RunWithPrefix(Action prefix)
        {
            // Keep a stable base prefix (e.g. banner) and replace only transient overlays.
            var wrapped = new MenuPage(Title ?? string.Empty, options, Description, baseRenderPrefix, prefix);
            wrapped.Run();
        }
    }
}
