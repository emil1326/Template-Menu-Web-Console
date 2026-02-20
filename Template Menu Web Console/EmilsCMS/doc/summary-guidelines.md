# XML <summary> Documentation Guidelines

Purpose
- Provide concise, consistent, and helpful XML documentation for public and internal APIs so other developers (and tooling) can read intent, behavior, and exceptions quickly.

Structure and required tags
- `<summary>`: One-line overview in the imperative mood (what the method does).
- `<param name="...">`: Describe purpose and any important constraints for each parameter.
- `<typeparam name="...">`: Describe the type's role when generic.
- `<returns>`: Describe the return value, including nullability and special sentinel values.
- `<exception cref="...">`: Document conditions that cause exceptions and mention which exception types may be thrown.
- `<remarks>`: Optional extended notes, edge cases, algorithmic complexity, side effects, or platform-specific behavior.
- `<example>`: Optional short usage example when the behavior is non-obvious.

Style rules
- Use the imperative voice: "Returns the parsed value" not "Returns the value that was parsed".
- Keep `<summary>` to a single sentence; move details to `<remarks>`.
- Mention culture/format assumptions explicitly (dates, numbers, encodings).
- When a method may swallow exceptions or return a fallback value, state that clearly in `<summary>` or `<returns>`.
- Prefer explicit tags over free text in `<remarks>` for key behaviors (e.g., "This method uses an ANSI escape sequence to clear terminal scrollback").

Examples

ClearConsole
```xml
/// <summary>
/// Clears the console screen and attempts to clear the terminal's scrollback buffer.
/// </summary>
/// <remarks>
/// Calls Console.Clear and writes an ANSI escape sequence that may not be supported
/// by every terminal emulator.
/// </remarks>
```

AskUsers (summary + remarks)
```xml
/// <summary>
/// Prompts the user and converts the input to <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Target type for the converted value. Nullable value types return default on empty input.</typeparam>
/// <param name="Question">Prompt text displayed to the user.</param>
/// <returns>The parsed value as <typeparamref name="T"/>, or default for nullable types when input is empty.</returns>
/// <exception cref="InvalidCastException">Thrown when conversion fails or required input is empty.</exception>
/// <remarks>
/// Date parsing attempts common formats (ISO yyyy-MM-dd, dd/MM/yyyy, MM/dd/yyyy) using culture fallbacks.
/// Floating point parsing normalizes decimal separators to the current culture.
/// </remarks>
```

Quick checklist before committing
- Is the `<summary>` a single clear sentence?
- Are parameter constraints and nullability documented?
- Are exceptions and special behaviors described?
- Did you include culture or format expectations (dates, numbers)?

Follow these rules to keep XML documentation consistent and useful.
