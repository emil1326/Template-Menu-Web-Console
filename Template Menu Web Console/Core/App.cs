using System;
using System.Collections.Generic;
using System.Linq;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Base class for any "app" that plugs into the core.  Provides a reference
    /// to the hosting <see cref="CMSCore"/> and some lifecycle hooks.
    /// </summary>
    internal abstract class App
    {
        private readonly List<App> childApps = [];

        /// <summary>
        /// Human-friendly app/module name used in router settings and info screens.
        /// </summary>
        public virtual string DisplayName => GetType().Name;

        /// <summary>
        /// Child applications owned by this app.
        /// </summary>
        public IReadOnlyList<App> ChildApps => childApps;

        /// <summary>
        /// Core instance that owns this application component.
        /// </summary>
        protected CMSCore Core { get; }

        protected App(CMSCore core)
        {
            Core = core ?? throw new ArgumentNullException(nameof(core));
        }

        /// <summary>
        /// Register a child app under this app (supports unlimited hierarchy depth).
        /// </summary>
        public void RegisterSubApp(App app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (!ReferenceEquals(app.Core, Core))
            {
                throw new ArgumentException("Sub-app must use the same CMSCore instance.", nameof(app));
            }

            childApps.Add(app);
        }

        /// <summary>
        /// Called by the core during startup.  Override in derived types to perform
        /// any initialisation (e.g. register menu items, prepare state, etc.).
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Called by the core just before the program exits.  Useful for cleanup.
        /// </summary>
        public virtual void Cleanup() { }

        /// <summary>
        /// Optional settings value pages declared by this app.
        /// </summary>
        public virtual IEnumerable<SettingsValues> GetSettingsValues()
        {
            return [];
        }

        /// <summary>
        /// Optional settings display pages declared by this app.
        /// </summary>
        public virtual IEnumerable<DisplaySettingsPage> GetDisplaySettingsPages()
        {
            return [];
        }

        /// <summary>
        /// Optional informational lines exposed to the shared router info page.
        /// </summary>
        public virtual IEnumerable<string> GetInfoLines()
        {
            return [];
        }

        /// <summary>
        /// Enumerate this app and all descendants depth-first.
        /// </summary>
        public IEnumerable<(App app, int depth)> EnumerateHierarchy(bool includeSelf = true)
        {
            if (includeSelf)
            {
                yield return (this, 0);
            }

            foreach (var child in childApps)
            {
                foreach (var item in child.EnumerateHierarchy(includeSelf: true))
                {
                    yield return (item.app, item.depth + 1);
                }
            }
        }

        internal void InitializeTree()
        {
            RegisterSettingsPages(this);
            Initialize();
            foreach (var child in childApps)
            {
                child.InitializeTree();
            }
        }

        internal void CleanupTree()
        {
            foreach (var child in childApps)
            {
                child.CleanupTree();
            }

            Cleanup();
        }

        /// <summary>
        /// Shows a hierarchical settings page for this app and its descendants.
        /// </summary>
        protected void ShowScopedSettingsPage(Action onBack, string title = "=== PARAMETRES (HIERARCHIQUES) ===")
        {
            var entries = new List<SettingsComponent.SettingEntry>();

            foreach (var (app, depth) in EnumerateHierarchy())
            {
                AddSettingsSection(entries, app, depth);
            }

            var page = new SettingsComponent(entries, onFinish: () =>
            {
                Core.PersistSettings();
                onBack();
            }, title: title);
            page.Run();
        }

        /// <summary>
        /// Shows a hierarchical info page for this app and its descendants.
        /// </summary>
        protected void ShowScopedInfoPage(Action onBack, string title = "=== INFORMATIONS (HIERARCHIQUES) ===")
        {
            Helpers.ClearConsole();
            Console.WriteLine(title);
            Console.WriteLine();

            foreach (var (app, depth) in EnumerateHierarchy())
            {
                string indent = new string(' ', depth * 2);
                Console.WriteLine($"{indent}-> {app.DisplayName}");

                var lines = app.GetInfoLines().ToList();
                if (lines.Count == 0)
                {
                    Console.WriteLine($"{indent}   (aucune information)");
                }
                else
                {
                    foreach (var line in lines)
                    {
                        Console.WriteLine($"{indent}   {line}");
                    }
                }

                Console.WriteLine();
            }

            if (!Helpers.WaitForContinue("Appuyez sur Entrée pour revenir..."))
                return;
            onBack();
        }

        private static void AddSettingsSection(List<SettingsComponent.SettingEntry> entries, App app, int depth)
        {
            string indent = new(' ', depth * 2);
            entries.Add(new SettingsComponent.SettingEntry($"{indent}-> {app.DisplayName}", () => string.Empty, _ => { }, IsEditable: false));

            RegisterSettingsPages(app);

            var displayPages = app.GetDisplaySettingsPages().ToList();
            if (displayPages.Count == 0)
            {
                entries.Add(new SettingsComponent.SettingEntry($"{indent}   (aucun paramètre)", () => string.Empty, _ => { }, IsEditable: false));
            }
            else
            {
                foreach (var display in displayPages)
                {
                    var pageEntries = SettingsComponent.BuildEntriesForPage(Globals.GlobalSettings, display.PageKey, () => { });
                    if (pageEntries.Count == 0)
                    {
                        continue;
                    }

                    entries.AddRange(pageEntries);
                }
            }

            entries.Add(new SettingsComponent.SettingEntry(string.Empty, () => string.Empty, _ => { }, IsEditable: false));
        }

        private static void RegisterSettingsPages(App app)
        {
            foreach (var values in app.GetSettingsValues())
            {
                SettingsComponent.RegisterPageDefaults(Globals.GlobalSettings, values);
            }

            foreach (var display in app.GetDisplaySettingsPages())
            {
                SettingsComponent.RegisterDisplayDefaults(Globals.GlobalSettings, display);
            }
        }
    }
}
