namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Categorises the kind of failure represented by an <see cref="AppError"/>.
    /// </summary>
    public enum ErrorCode
    {
        /// <summary>Input data failed a validation rule.</summary>
        Validation,
        /// <summary>The requested entity or resource could not be found.</summary>
        NotFound,
        /// <summary>The operation conflicts with the current state of the data.</summary>
        Conflict,
        /// <summary>The data source (file, database, network) is unavailable or returned an error.</summary>
        DataSource,
        /// <summary>The operation exceeded the allowed time limit.</summary>
        Timeout,
        /// <summary>The application or entity configuration is invalid (e.g. missing <see cref="IsIdAttribute"/>).</summary>
        Configuration,
        /// <summary>An unexpected error that does not fit any other category.</summary>
        Unknown
    }
}
