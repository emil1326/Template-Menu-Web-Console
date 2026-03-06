using System;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Represents a structured error with a categorisation code, a developer-facing message, and an optional user-facing message.
    /// When an instance is created it is automatically logged to <c>devlogs.txt</c> for developers.
    /// </summary>
    public class AppError : Exception, IUIComponent
    {
        /// <summary>Gets the category of this error.</summary>
        public ErrorCode Code { get; }

        /// <summary>Gets the developer-facing description of the error. Always set.</summary>
        public string TechnicalMessage { get; }

        /// <summary>Gets the optional user-facing override message. When <c>null</c>, <see cref="ToUserMessage"/> returns a default for <see cref="Code"/>.</summary>
        public string? UserMessage { get; }

        /// <summary>Title shown when the error is rendered as a UI component.</summary>
        public string? Title => $"[ERROR] {Code}";

        /// <summary>Severity/gravity of this error. Higher numbers are more important.</summary>
        public int Severity { get; }

        /// <summary>
        /// Initialises a new <see cref="AppError"/>.
        /// </summary>
        /// <param name="code">The error category.</param>
        /// <param name="technicalMessage">The developer-facing description. Must not be <c>null</c>.</param>
        /// <param name="inner">Optional inner exception; its stack trace will be logged as well.</param>
        /// <param name="userMessage">Optional user-facing override. When omitted, <see cref="ToUserMessage"/> provides a default per <paramref name="code"/>.</param>
        /// <param name="severity">Gravity of the error; messages below <see cref="Globals.LogSeverityThreshold"/> will be hidden from console.</param>
        public AppError(ErrorCode code, string technicalMessage, Exception? inner = null, string? userMessage = null, int severity = 500)
            : base(technicalMessage, inner)
        {
            Code = code;
            TechnicalMessage = technicalMessage;
            UserMessage = userMessage;
            Severity = severity;

            // log immediately for developer diagnostics
            Logger.Error($"{code}: {technicalMessage}", Severity);

            // the exception's own StackTrace property is only populated when thrown,
            // so if it's not yet available grab the current call stack instead.
            if (string.IsNullOrWhiteSpace(this.StackTrace))
            {
                var stackSnapshot = Environment.StackTrace;
                if (!string.IsNullOrWhiteSpace(stackSnapshot))
                {
                    // log a few lines of the snapshot directly
                    var lines = stackSnapshot.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < Math.Min(Logger.DefaultStackTraceLines, lines.Length); i++)
                    {
                        Logger.Error(lines[i], Severity);
                    }
                }
            }
            else
            {
                Logger.LogException(this, Logger.DefaultStackTraceLines, Severity);
            }
        }

        /// <summary>
        /// Returns the user-facing message: <see cref="UserMessage"/> if set, otherwise a localised default string based on <see cref="Code"/>.
        /// </summary>
        /// <returns>A non-null, non-empty user-facing string.</returns>
        public string ToUserMessage() =>
            !string.IsNullOrWhiteSpace(UserMessage)
                ? UserMessage
                : Code switch
                {
                    ErrorCode.Validation => "Les données fournies sont invalides.",
                    ErrorCode.NotFound => "L'élément demandé est introuvable.",
                    ErrorCode.Conflict => "L'opération est en conflit avec l'état actuel des données.",
                    ErrorCode.DataSource => "La source de données est indisponible pour le moment.",
                    ErrorCode.Timeout => "Le délai d'attente a été dépassé.",
                    ErrorCode.Configuration => "La configuration de l'application est invalide.",
                    _ => "Une erreur inattendue est survenue."
                };

        // ---------------------------------------------------------
        // IUIComponent implementation so an error can be shown as a
        // self‑contained screen.
        // ---------------------------------------------------------

        public void Render()
        {
            Helpers.ClearConsole();
            Console.WriteLine(Title);
            Console.WriteLine();
            Console.WriteLine(ToUserMessage());
            Console.WriteLine();
            Console.WriteLine("(appuyez sur Entrée pour continuer)");
        }

        public void ProcessInput()
        {
            Helpers.WaitForContinue(string.Empty);
        }

        /// <summary>
        /// Helper that both logs the error and throws it; intended for
        /// use in catch blocks when you want an exception to propagate.
        /// </summary>
        public static void Throw(ErrorCode code, string technicalMessage, Exception? inner = null, string? userMessage = null, int severity = 500)
        {
            throw new AppError(code, technicalMessage, inner, userMessage, severity);
        }
    }
}
