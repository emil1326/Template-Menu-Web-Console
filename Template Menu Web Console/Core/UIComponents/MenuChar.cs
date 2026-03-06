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
            char input = char.ToLower(Console.ReadKey(true).KeyChar);
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

        /// <summary>
        /// Backwards-compatible entry used throughout the user app.
        /// </summary>
        [Obsolete("This method is deprecated and will be removed in a future version. Use the Run() method instead.")]
        public void ProcessMenuInput() => Run();
    }
}
