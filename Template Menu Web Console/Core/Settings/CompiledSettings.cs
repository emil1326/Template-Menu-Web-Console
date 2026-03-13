using System;
using System.Collections.Generic;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Base class for page-owned settings values.
    /// </summary>
    public abstract class SettingsValues
    {
        public abstract string PageKey { get; }
    }

    /// <summary>
    /// Core MongoDB settings values.
    /// </summary>
    public sealed class CoreMongoSettingsValues : SettingsValues
    {
        public const string Key = "core.mongo";
        public override string PageKey => Key;

        public bool MongoEnabled { get; set; } = false;
        public string? MongoHost { get; set; }
        public int MongoPort { get; set; } = 27017;
        public string? MongoUser { get; set; }
        public string? MongoDbPassword { get; set; }
        public string? MongoDatabase { get; set; }
        public string? MongoCollection { get; set; }
    }

    /// <summary>
    /// Describes how a settings field should be rendered/edited in menus.
    /// </summary>
    public sealed class SettingDisplayEntry
    {
        public string FieldKey { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string PromptText { get; set; } = string.Empty;
        public bool IsEditable { get; set; } = true;
        public bool ToggleBooleanDirectly { get; set; } = true;
    }

    /// <summary>
    /// Per-page display metadata, kept separate from page settings values.
    /// </summary>
    public sealed class DisplaySettingsPage
    {
        public string PageKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<SettingDisplayEntry> Entries { get; set; } = [];
    }

    /// <summary>
    /// Persisted root settings object that compiles all page values and display metadata.
    /// </summary>
    public sealed class CompiledSettings
    {
        [IsId]
        public string SettingsId { get; set; } = "compiled";
        public List<SettingsValues> Pages { get; set; } = [];
        public List<DisplaySettingsPage> Displays { get; set; } = [];
        public DateTime LastSavedUtc { get; set; } = DateTime.UtcNow;
    }
}
