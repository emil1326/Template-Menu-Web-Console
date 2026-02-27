namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Represents a structured error with a categorisation code, a developer-facing message, and an optional user-facing message.
    /// </summary>
    public sealed class AppError
    {
        /// <summary>Gets the category of this error.</summary>
        public ErrorCode Code { get; }

        /// <summary>Gets the developer-facing description of the error. Always set.</summary>
        public string TechnicalMessage { get; }

        /// <summary>Gets the optional user-facing override message. When <c>null</c>, <see cref="ToUserMessage"/> returns a default for <see cref="Code"/>.</summary>
        public string? UserMessage { get; }

        /// <summary>
        /// Initialises a new <see cref="AppError"/>.
        /// </summary>
        /// <param name="code">The error category.</param>
        /// <param name="technicalMessage">The developer-facing description. Must not be <c>null</c>.</param>
        /// <param name="userMessage">Optional user-facing override. When omitted, <see cref="ToUserMessage"/> provides a default per <paramref name="code"/>.</param>
        public AppError(ErrorCode code, string technicalMessage, string? userMessage = null)
        {
            Code = code;
            TechnicalMessage = technicalMessage;
            UserMessage = userMessage;
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
    }
}
