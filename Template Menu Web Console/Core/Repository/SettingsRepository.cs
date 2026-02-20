using System;
using System.Collections.Generic;
using System.Linq;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Simple repository for application settings. Persists a single AppSettings
    /// instance via an IService&lt;AppSettings&gt; (e.g., JsonFileService).
    /// </summary>
    public class SettingsRepository : RepositoryBase<AppSettings>
    {
        private readonly IService<AppSettings> service;

        public SettingsRepository(IService<AppSettings> svc)
        {
            service = svc ?? throw new ArgumentNullException(nameof(svc));
            Items = new List<AppSettings>();
        }

        public override void Add(AppSettings item)
        {
            service.Add(item);
            Items.Add(item);
        }

        public override void Remove(AppSettings item)
        {
            service.Remove(item);
            Items.Remove(item);
        }

        public override void Save()
        {
            service.SaveAll(Items);
        }

        public override void Load()
        {
            Items = service.GetAll() ?? new List<AppSettings>();
        }

        /// <summary>
        /// Get the single settings instance (or default) from the repository.
        /// </summary>
        public AppSettings GetSettingsOrDefault()
        {
            if (Items == null || Items.Count == 0) return new AppSettings();
            return Items[0];
        }

        /// <summary>
        /// Replace the stored settings with the provided instance and persist.
        /// </summary>
        public void SaveSingle(AppSettings setting)
        {
            Items = new List<AppSettings> { setting };
            Save();
        }
    }
}
