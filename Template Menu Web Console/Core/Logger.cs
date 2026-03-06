using System;
using System.IO;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Simple file-based logger that also writes to the console.
    /// Logs are appended to a text file in the application directory so that developers
    /// can inspect a record of what happened after the program runs.
    /// </summary>
    internal static class Logger
    {
        /// <summary>Number of stack-trace lines to record when logging an exception (default).
        /// Can be overridden by callers.</summary>
        public const int DefaultStackTraceLines = 5;

        private static readonly string logFilePath = Path.Combine(AppContext.BaseDirectory, "devlogs.txt");
        private static readonly Lock lockObj = new();

        /// <summary>Writes a message with the given severity level.</summary>
        public static void Log(string message, int severity = 0)
        {
            WriteInternal("INFO", message, severity);
        }

        /// <summary>Logs a warning-level message.</summary>
        public static void Warn(string message, int severity = 100)
        {
            WriteInternal("WARN", message, severity);
        }

        /// <summary>Logs an error-level message.</summary>
        public static void Error(string message, int severity = 500)
        {
            WriteInternal("ERROR", message, severity);
        }

        /// <summary>Logs an exception message and a limited portion of its stack trace.</summary>
        public static void LogException(Exception ex, int maxLines = DefaultStackTraceLines, int severity = 500)
        {
            if (ex == null) return;

            Error(ex.Message, severity);

            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                var lines = ex.StackTrace.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < Math.Min(lines.Length, maxLines); i++)
                {
                    WriteInternal("ERROR", lines[i], severity);
                }
            }

            if (ex.InnerException != null)
            {
                WriteInternal("ERROR", "--- inner exception ---", severity);
                LogException(ex.InnerException, maxLines, severity);
            }
        }

        private static void WriteInternal(string level, string message, int severity)
        {
            var line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}][{level}][{severity}] {message}";

            // write to console for realtime visibility if above threshold
            if (severity >= Globals.LogSeverityThreshold)
            {
                Console.WriteLine(line);
            }

            try
            {
                lock (lockObj)
                {
                    File.AppendAllText(logFilePath, line + Environment.NewLine);
                }
            }
            catch
            {
                // if logging to file fails we swallow the exception to avoid
                // interfering with normal program flow. Console output may be
                // suppressed based on verbosity but file should still contain
                // the entry.
            }
        }
    }
}