using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// A simple settings UI component that displays labeled settings and allows editing.
    /// Uses the UIComponent base class to encapsulate render + logic.
    /// </summary>
    public class SettingsComponent : UIComponent
    {
        private static readonly JsonSerializerSettings CompiledSettingsSerializer = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented
        };

        public record SettingEntry(
            string Label,
            Func<string> Getter,
            Action<string> Setter,
            bool IsEditable = true,
            string? PromptText = null,
            bool ToggleBooleanDirectly = true);

        private readonly List<SettingEntry> entries;
        private readonly Action? onFinish;
        private List<SettingEntry> editableEntries = new();

        public static CompiledSettings LoadCompiledSettings(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return EnsureCoreMongoDefaults(new CompiledSettings());
            }

            try
            {
                string json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return EnsureCoreMongoDefaults(new CompiledSettings());
                }

                var compiled = JsonConvert.DeserializeObject<CompiledSettings>(json, CompiledSettingsSerializer);
                if (compiled != null)
                {
                    return EnsureCoreMongoDefaults(compiled);
                }

                return EnsureCoreMongoDefaults(new CompiledSettings());
            }
            catch
            {
                return EnsureCoreMongoDefaults(new CompiledSettings());
            }
        }

        public static void SaveCompiledSettings(string filePath, CompiledSettings compiled)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("settings file path is required", nameof(filePath));
            }

            var normalized = EnsureCoreMongoDefaults(compiled ?? new CompiledSettings());
            normalized.LastSavedUtc = DateTime.UtcNow;

            string json = JsonConvert.SerializeObject(normalized, CompiledSettingsSerializer);
            File.WriteAllText(filePath, json);
        }

        public static T GetOrCreatePage<T>(CompiledSettings globalSettings) where T : SettingsValues, new()
        {
            globalSettings ??= new CompiledSettings();
            globalSettings.Pages ??= [];

            var page = globalSettings.Pages.OfType<T>().FirstOrDefault();
            if (page != null)
            {
                return page;
            }

            page = new T();
            globalSettings.Pages.Add(page);
            return page;
        }

        public static void RegisterPageDefaults(CompiledSettings globalSettings, SettingsValues pageValues)
        {
            if (globalSettings == null || pageValues == null)
            {
                return;
            }

            globalSettings.Pages ??= [];

            if (!globalSettings.Pages.Any(x => string.Equals(x.PageKey, pageValues.PageKey, StringComparison.OrdinalIgnoreCase)))
            {
                globalSettings.Pages.Add(pageValues);
            }
        }

        public static void RegisterDisplayDefaults(CompiledSettings globalSettings, DisplaySettingsPage display)
        {
            if (globalSettings == null || display == null || string.IsNullOrWhiteSpace(display.PageKey))
            {
                return;
            }

            globalSettings.Displays ??= [];

            if (!globalSettings.Displays.Any(x => string.Equals(x.PageKey, display.PageKey, StringComparison.OrdinalIgnoreCase)))
            {
                globalSettings.Displays.Add(display);
            }
        }

        public static List<SettingEntry> BuildEntriesForPage(CompiledSettings globalSettings, string pageKey, Action onChanged)
        {
            var entries = new List<SettingEntry>();
            if (globalSettings == null || string.IsNullOrWhiteSpace(pageKey))
            {
                return entries;
            }

            var values = globalSettings.Pages.FirstOrDefault(x => string.Equals(x.PageKey, pageKey, StringComparison.OrdinalIgnoreCase));
            var display = globalSettings.Displays.FirstOrDefault(x => string.Equals(x.PageKey, pageKey, StringComparison.OrdinalIgnoreCase));

            if (values == null || display == null)
            {
                return entries;
            }

            entries.Add(new SettingEntry(display.Title, () => string.Empty, _ => { }, IsEditable: false));

            foreach (var descriptor in display.Entries)
            {
                if (!descriptor.IsEditable)
                {
                    entries.Add(new SettingEntry(descriptor.Label, () => string.Empty, _ => { }, IsEditable: false));
                    continue;
                }

                var property = values.GetType().GetProperty(descriptor.FieldKey, BindingFlags.Public | BindingFlags.Instance);
                if (property == null || !property.CanRead || !property.CanWrite)
                {
                    continue;
                }

                entries.Add(new SettingEntry(
                    descriptor.Label,
                    Getter: () => ConvertToString(property.GetValue(values)),
                    Setter: raw =>
                    {
                        if (!TryConvertFromString(property.PropertyType, raw, out var parsed))
                        {
                            return;
                        }

                        property.SetValue(values, parsed);
                        onChanged?.Invoke();
                    },
                    IsEditable: true,
                    PromptText: descriptor.PromptText,
                    ToggleBooleanDirectly: descriptor.ToggleBooleanDirectly));
            }

            entries.Add(new SettingEntry(string.Empty, () => string.Empty, _ => { }, IsEditable: false));
            return entries;
        }

        private static CompiledSettings EnsureCoreMongoDefaults(CompiledSettings compiled)
        {
            compiled ??= new CompiledSettings();
            compiled.Pages ??= [];
            compiled.Displays ??= [];

            if (!compiled.Pages.OfType<CoreMongoSettingsValues>().Any())
            {
                compiled.Pages.Add(new CoreMongoSettingsValues());
            }

            if (!compiled.Displays.Any(d => string.Equals(d.PageKey, CoreMongoSettingsValues.Key, StringComparison.OrdinalIgnoreCase)))
            {
                compiled.Displays.Add(new DisplaySettingsPage
                {
                    PageKey = CoreMongoSettingsValues.Key,
                    Title = "=== MongoDB (Core) ===",
                    Entries =
                    [
                        new SettingDisplayEntry { FieldKey = nameof(CoreMongoSettingsValues.MongoEnabled), Label = "Mongo enabled", PromptText = "Enable MongoDB?", ToggleBooleanDirectly = true },
                        new SettingDisplayEntry { FieldKey = nameof(CoreMongoSettingsValues.MongoHost), Label = "Mongo host", PromptText = "Mongo host" },
                        new SettingDisplayEntry { FieldKey = nameof(CoreMongoSettingsValues.MongoPort), Label = "Mongo port", PromptText = "Mongo port" },
                        new SettingDisplayEntry { FieldKey = nameof(CoreMongoSettingsValues.MongoUser), Label = "Mongo user", PromptText = "Mongo user" },
                        new SettingDisplayEntry { FieldKey = nameof(CoreMongoSettingsValues.MongoDbPassword), Label = "Mongo password", PromptText = "Mongo password" },
                        new SettingDisplayEntry { FieldKey = nameof(CoreMongoSettingsValues.MongoDatabase), Label = "Mongo database", PromptText = "Mongo database" },
                        new SettingDisplayEntry { FieldKey = nameof(CoreMongoSettingsValues.MongoCollection), Label = "Mongo collection", PromptText = "Mongo collection" }
                    ]
                });
            }

            return compiled;
        }

        private static string ConvertToString(object? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value.ToString() ?? string.Empty;
        }

        private static bool TryConvertFromString(Type propertyType, string input, out object? parsed)
        {
            parsed = null;
            var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (targetType == typeof(string))
            {
                parsed = string.IsNullOrWhiteSpace(input) ? null : input;
                return true;
            }

            if (targetType == typeof(bool))
            {
                if (bool.TryParse(input, out var b))
                {
                    parsed = b;
                    return true;
                }

                return false;
            }

            if (targetType == typeof(int))
            {
                if (int.TryParse(input, out var i))
                {
                    parsed = i;
                    return true;
                }

                return false;
            }

            try
            {
                parsed = Convert.ChangeType(input, targetType);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public SettingsComponent(List<SettingEntry> entries, Action? onFinish = null, string? title = null)
        {
            this.entries = entries ?? [];
            this.onFinish = onFinish;
            Title = string.IsNullOrWhiteSpace(title) ? "=== PARAMETRES ===" : title;
        }

        public override void Render()
        {
            Helpers.ClearConsole();
            Console.WriteLine(Title);
            Console.WriteLine();

            editableEntries = entries.Where(x => x.IsEditable).ToList();

            int visibleIndex = 1;
            foreach (var e in entries)
            {
                if (!e.IsEditable)
                {
                    Console.WriteLine(e.Label);
                    continue;
                }

                Console.WriteLine($"{visibleIndex}. {e.Label} : {e.Getter()}");
                visibleIndex++;
            }

            Console.WriteLine();
            Console.WriteLine("Entrez le numéro du champ à modifier (ex: 10), puis Entrée. Q pour revenir.");
        }

        public override void ProcessInput()
        {
            while (true)
            {
                var input = Helpers.ReadLineWithShortcuts("> ", inline: true);
                if (input == null)
                {
                    return;
                }

                input = input.Trim();
                if (string.Equals(input, "q", StringComparison.OrdinalIgnoreCase))
                {
                    onFinish?.Invoke();
                    return;
                }

                if (!int.TryParse(input, out int idx) || idx < 1 || idx > editableEntries.Count)
                {
                    Logger.Warn("Entrée invalide dans paramètres");
                    Console.WriteLine("Entrée invalide.");
                    continue;
                }

                var entry = editableEntries[idx - 1];

                // Bool settings toggle directly when selected (no extra prompt).
                var currentValue = entry.Getter();
                if (entry.ToggleBooleanDirectly && bool.TryParse(currentValue, out var currentBool))
                {
                    var toggled = !currentBool;
                    entry.Setter(toggled.ToString());
                    Logger.Log($"Valeur booléenne inversée: {entry.Label}={toggled}");
                    Console.WriteLine($"[OK] '{entry.Label}' => {toggled}");
                    Run();
                    return;
                }

                string prompt = string.IsNullOrWhiteSpace(entry.PromptText)
                    ? $"Nouvelle valeur pour '{entry.Label}' (vide = annuler) : "
                    : $"{entry.PromptText} (vide = annuler) : ";

                var val = Helpers.ReadLineWithShortcuts(prompt, inline: true);
                if (val == null)
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(val))
                {
                    entry.Setter(val);
                    Logger.Log("Valeur mise à jour.");
                    Console.WriteLine("[OK] Valeur mise à jour.");
                }
                else
                {
                    Logger.Log("Modification annulée.");
                    Console.WriteLine("[INFO] Modification annulée.");
                }
                Run();
                return;
            }
        }
    }
}
