namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Repository for persisting a single <see cref="AppSettings"/> instance through an <see cref="IService{TEntity}"/>.
    /// </summary>
    public class SettingsRepository : RepositoryBase<AppSettings>
    {
        /// <summary>
        /// Initialises the repository with the given settings service.
        /// </summary>
        /// <param name="svc">The service used to read and write settings. Typically a <see cref="JsonFileService{TEntity}"/>.</param>
        public SettingsRepository(IService<AppSettings> svc)
            : base(svc)
        {
        }

        /// <summary>
        /// Returns the first stored settings instance, or a default <see cref="AppSettings"/> if the service cache is empty.
        /// </summary>
        /// <returns>The current <see cref="AppSettings"/>, never <c>null</c>.</returns>
        public AppSettings GetSettingsOrDefault()
        {
            return Items.Count == 0 ? new AppSettings() : Items[0];
        }

        /// <summary>
        /// Replaces all stored settings with a single instance and persists immediately to the data source.
        /// </summary>
        /// <param name="setting">The settings instance to save. Must not be <c>null</c>.</param>
        public void SaveSingle(AppSettings setting)
        {
            service.WriteAll([setting]);
        }
    }
}
