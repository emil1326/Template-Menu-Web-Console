using System;
using System.Collections.Generic;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Lightweight navigation history used by menu/prompt input handlers.
    /// Supports a best-effort global "back" behavior (Ctrl+B).
    /// </summary>
    internal static class NavigationHistory
    {
        private sealed record Entry(string Key, Action Render);

        private static readonly Stack<Entry> stack = new();

        public static int Depth => stack.Count;

        public static void Push(string key, Action render)
        {
            if (stack.Count > 0 && stack.Peek().Key == key)
            {
                return;
            }

            stack.Push(new Entry(key, render));
            Logger.Log($"[NAV] push '{key}' depth={stack.Count}");
        }

        public static bool TryBack()
        {
            if (stack.Count <= 1)
            {
                return false;
            }

            var from = stack.Pop();
            var previous = stack.Peek();
            Logger.Log($"[NAV] back '{from.Key}' -> '{previous.Key}' depth={stack.Count}");

            previous.Render();

            return true;
        }

        /// <summary>
        /// Replays the current (top) registered page without popping history.
        /// Intended for non-menu flows where Ctrl+B should return to the last menu page.
        /// </summary>
        public static bool ReplayTop()
        {
            if (stack.Count == 0)
            {
                return false;
            }

            var current = stack.Peek();
            Logger.Log($"[NAV] replay '{current.Key}' depth={stack.Count}");

            current.Render();

            return true;
        }

        public static void BackUnavailableFallback(string source)
        {
            Logger.Warn($"[NAV] back unavailable from {source} depth={stack.Count}");
            Console.WriteLine("[WARN] Retour impossible, retour a la racine de la page.");

            if (!ReplayTop())
            {
                CMSCore.Current?.MainMenu();
            }
        }
    }
}
