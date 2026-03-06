using System;
using System.Collections.Generic;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// User-side MenuChar implemented as a UIComponent (render + logic).
    /// Compatible with existing usage via the instance method `ProcessMenuInput()`.
    /// </summary>
    public class MenuChar : UIComponent
    {
        public List<string> MenuNames { get; set; } = [];
        public List<char> Chars { get; set; } = [];
        public List<Action> Actions { get; set; } = [];
        public Action OnError { get; set; }

        public MenuChar(List<string> menuNames, List<char> chars, List<Action> actions, Action onError)
        {
            MenuNames = menuNames ?? [];
            Chars = chars ?? [];
            Actions = actions ?? [];
            OnError = onError;
        }

        public override void Render()
        {
            foreach (var line in MenuNames)
            {
                Console.WriteLine(line);
            }
        }

        public override void ProcessInput()
        {
            var key = Console.ReadKey(true);
            if ((key.Modifiers & ConsoleModifiers.Control) != 0 && key.Key == ConsoleKey.H)
            {
                Logger.Log("Home shortcut from MenuChar");
                if (CMSCore.Current != null)
                {
                    CMSCore.Current.MainMenu();
                }
                return;
            }

            if ((key.Modifiers & ConsoleModifiers.Control) != 0 && key.Key == ConsoleKey.B)
            {
                Logger.Log("Back shortcut from MenuChar");
                if (!NavigationHistory.TryBack())
                {
                    NavigationHistory.BackUnavailableFallback("MenuChar");
                }
                return;
            }

            char input = char.ToLower(key.KeyChar);
            Console.WriteLine();

            for (int i = 0; i < Chars.Count; i++)
            {
                if (Chars[i] == input)
                {
                    Actions[i]?.Invoke();
                    return;
                }
            }

            if (OnError != null)
            {
                OnError();
                return;
            }

            Logger.Warn("Invalid menu input.");
            Console.WriteLine("[WARN] Invalid menu input.");
        }

        public override void Run()
        {
            NavigationHistory.Push($"MenuChar:{Title}:{MenuNames.Count}:{Chars.Count}", Run);
            base.Run();
        }

        /// <summary>
        /// Backwards-compatible entry used throughout the user app.
        /// </summary>
        [Obsolete("This method is deprecated and will be removed in a future version. Use the Run() method instead.")]
        public void ProcessMenuInput() => Run();
    }
}
