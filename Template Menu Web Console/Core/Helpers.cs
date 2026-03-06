using System.Globalization;
using System.Text;

namespace EmilsWork.EmilsCMS
{
    internal class Helpers
    {
        /// <summary>
        /// Clears the console screen and attempts to clear the terminal's scrollback buffer.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="Console.Clear"/> and then writes an ANSI
        /// escape sequence that requests the terminal to also clear its scrollback
        /// buffer. The ANSI sequence is dependent on the terminal emulator and may
        /// be ignored on some hosts; callers should not rely on scrollback being
        /// cleared in every environment.
        /// </remarks>
        public static void ClearConsole()
        {
            Console.WriteLine("\x1b[3J");
            Console.Clear();
        }

        /// <summary>
        /// Prompts the user with <paramref name="Question"/> and attempts to convert
        /// the input to the requested type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The target type to convert the user's input into.</typeparam>
        /// <param name="Question">Prompt text displayed to the user.</param>
        /// <param name="response">When the method returns, contains the converted value if parsing
        /// succeeded; otherwise the default value of <typeparamref name="T"/>.</param>
        /// <param name="UseChar">If true, reads a single key press instead of a full line.</param>
        /// <param name="Erase">If true and <paramref name="UseChar"/> is true, the key pressed is not echoed.</param>
        /// <param name="inline">If true, writes the prompt on the same line as the input.</param>
        /// <returns>True when conversion succeeded; otherwise false. This method swallows
        /// exceptions thrown by <see cref="AskUsers{T}(string,bool,bool,bool)"/> and
        /// returns false on failure.</returns>
        public static bool TryAskUsers<T>(string Question, out T response, bool UseChar = false, bool Erase = false, bool inline = true)
        {
            try
            {
                response = AskUsers<T>(Question, UseChar, Erase, inline);
                return true;
            }
            catch
            {
                response = default!;
                return false;
            }
        }

        /// <summary>
        /// Prompts the user with <paramref name="Question"/> and converts the input
        /// to <typeparamref name="T"/>. This method does not catch conversion exceptions;
        /// callers should handle <see cref="InvalidCastException"/> as needed or use
        /// <see cref="TryAskUsers{T}(string,out T,bool,bool,bool)"/> to receive a
        /// success flag instead.
        /// </summary>
        /// <typeparam name="T">Target type for the converted value. Nullable value types
        /// are supported; an empty input will return <see langword="default"/> for
        /// nullable types.</typeparam>
        /// <param name="Question">Prompt text displayed to the user.</param>
        /// <param name="UseChar">If true, reads a single key press instead of a full line.</param>
        /// <param name="Erase">If true and <paramref name="UseChar"/> is true, the key pressed is not echoed.</param>
        /// <param name="inline">If true, writes the prompt on the same line as the input.</param>
        /// <returns>The parsed value as <typeparamref name="T"/>, or <see langword="default"/>
        /// if the input was null or an allowed nullable target type is empty.</returns>
        /// <exception cref="InvalidCastException">Thrown when the input cannot be converted to
        /// <typeparamref name="T"/>, when a required (non-nullable) value is empty, or when
        /// a date string does not match accepted formats. The original exception is preserved
        /// as the <see cref="Exception.InnerException"/>.</exception>
        /// <remarks>
        /// Special handling implemented by this method:
        /// - DateTime: attempts several common formats (ISO yyyy-MM-dd, dd/MM/yyyy, MM/dd/yyyy)
        ///   using invariant and culture fallbacks.
        /// - Floating point types: normalizes decimal separator for current culture before conversion.
        /// - Nullable target types: empty or whitespace input returns <see langword="default"/>.
        /// </remarks>
        public static T AskUsers<T>(string Question, bool UseChar = false, bool Erase = false, bool inline = true)
        {
            if (inline)
                Console.Write(Question);
            else
                Console.WriteLine(Question);

            string? text;
            if (UseChar)
            {
                var key = Console.ReadKey(Erase);
                if ((key.Modifiers & ConsoleModifiers.Control) != 0 && key.Key == ConsoleKey.H)
                {
                    Logger.Log("Home shortcut from AskUsers<char>");
                    CMSCore.Current?.MainMenu();
                    return default!;
                }

                if ((key.Modifiers & ConsoleModifiers.Control) != 0 && key.Key == ConsoleKey.B)
                {
                    Logger.Log("Back shortcut from AskUsers<char>");
                    if (!NavigationHistory.ReplayTop())
                        NavigationHistory.BackUnavailableFallback("AskUsers<char>");
                    return default!;
                }

                text = key.KeyChar.ToString();
            }
            else
            {
                var sb = new StringBuilder();

                while (true)
                {
                    var key = Console.ReadKey(intercept: true);

                    if ((key.Modifiers & ConsoleModifiers.Control) != 0 && key.Key == ConsoleKey.H)
                    {
                        Console.WriteLine();
                        Logger.Log("Home shortcut from AskUsers<string>");
                        CMSCore.Current?.MainMenu();
                        return default!;
                    }

                    if ((key.Modifiers & ConsoleModifiers.Control) != 0 && key.Key == ConsoleKey.B)
                    {
                        Console.WriteLine();
                        Logger.Log("Back shortcut from AskUsers<string>");
                        if (!NavigationHistory.ReplayTop())
                            NavigationHistory.BackUnavailableFallback("AskUsers<string>");
                        return default!;
                    }

                    if (key.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine();
                        break;
                    }

                    if (key.Key == ConsoleKey.Backspace)
                    {
                        if (sb.Length > 0)
                        {
                            sb.Length--;
                            Console.Write("\b \b");
                        }
                        continue;
                    }

                    if (!char.IsControl(key.KeyChar))
                    {
                        sb.Append(key.KeyChar);
                        Console.Write(key.KeyChar);
                    }
                }

                text = sb.ToString();
            }

            if (text == null)
                return default!;

            try
            {
                var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                if (string.IsNullOrWhiteSpace(text))
                {
                    if (Nullable.GetUnderlyingType(typeof(T)) != null)
                        return default!;

                    throw new InvalidCastException("Valeur requise.");
                }

                if (targetType == typeof(DateTime))
                {
                    var formats = new[] { "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy" };

                    if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                        || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt)
                        || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    {
                        return (T)(object)dt;
                    }

                    throw new InvalidCastException("Format de date invalide. Utilisez yyyy-MM-dd.");
                }

                if (targetType == typeof(float) || targetType == typeof(double) || targetType == typeof(decimal))
                {
                    text = text.Replace('.', ',');
                }

                var converted = Convert.ChangeType(text, targetType, CultureInfo.CurrentCulture);
                return (T)converted;
            }
            catch (Exception e)
            {
                // Wrap and preserve original exception (keeps stack trace in InnerException)
                throw new InvalidCastException("Unsupported return type or invalid input in AskUsers.", e);
            }
        }

        /// <summary>
        /// Waits for Enter so users can read the current screen before continuing.
        /// Supports global shortcuts Ctrl+B (back) and Ctrl+H (home).
        /// </summary>
        /// <param name="message">Optional pause message shown before waiting for input.</param>
        /// <returns>
        /// True when Enter was pressed and caller can continue its normal flow.
        /// False when a navigation shortcut was handled and caller should stop.
        /// </returns>
        public static bool WaitForContinue(string message = "Appuyez sur Entrée pour continuer...")
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine(message);
            }

            while (true)
            {
                var key = Console.ReadKey(intercept: true);

                if ((key.Modifiers & ConsoleModifiers.Control) != 0 && key.Key == ConsoleKey.H)
                {
                    Console.WriteLine();
                    Logger.Log("Home shortcut from WaitForContinue");
                    CMSCore.Current?.MainMenu();
                    return false;
                }

                if ((key.Modifiers & ConsoleModifiers.Control) != 0 && key.Key == ConsoleKey.B)
                {
                    Console.WriteLine();
                    Logger.Log("Back shortcut from WaitForContinue");
                    if (!NavigationHistory.ReplayTop())
                        NavigationHistory.BackUnavailableFallback("WaitForContinue");
                    return false;
                }

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return true;
                }
            }
        }

        /// <summary>
        /// Reads a full input line with shortcut handling (Ctrl+B / Ctrl+H).
        /// Returns null when a navigation shortcut was handled.
        /// </summary>
        public static string? ReadLineWithShortcuts(string prompt = "", bool inline = true)
        {
            if (!string.IsNullOrEmpty(prompt))
            {
                if (inline)
                    Console.Write(prompt);
                else
                    Console.WriteLine(prompt);
            }

            var sb = new StringBuilder();

            while (true)
            {
                var key = Console.ReadKey(intercept: true);

                if ((key.Modifiers & ConsoleModifiers.Control) != 0 && key.Key == ConsoleKey.H)
                {
                    Console.WriteLine();
                    Logger.Log("Home shortcut from ReadLineWithShortcuts");
                    CMSCore.Current?.MainMenu();
                    return null;
                }

                if ((key.Modifiers & ConsoleModifiers.Control) != 0 && key.Key == ConsoleKey.B)
                {
                    Console.WriteLine();
                    Logger.Log("Back shortcut from ReadLineWithShortcuts");
                    if (!NavigationHistory.ReplayTop())
                        NavigationHistory.BackUnavailableFallback("ReadLineWithShortcuts");
                    return null;
                }

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return sb.ToString();
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0)
                    {
                        sb.Length--;
                        Console.Write("\b \b");
                    }
                    continue;
                }

                if (!char.IsControl(key.KeyChar))
                {
                    sb.Append(key.KeyChar);
                    Console.Write(key.KeyChar);
                }
            }
        }

    }
}
