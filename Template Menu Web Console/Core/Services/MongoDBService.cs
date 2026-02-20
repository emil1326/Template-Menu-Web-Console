using System.Collections.Generic;

namespace EmilsWork.EmilsCMS
{
    public record MongoDBServiceSettings
    {
        public string AppName { get; init; } = "";
        public string Password { get; init; } = "";
        public string DatabaseName { get; init; } = "";
        public string CollectionName { get; init; } = "";
    }

    public class MongoDBService<T> : IService<T>
    {
        public MongoDBService(MongoDBServiceSettings settings, Action? configureMaps = null)
        {
            configureMaps?.Invoke();
        }

        public List<T> GetAll()
        {
            return new List<T>();
        }

        public void Add(T item)
        {
            // Impl spécifique à MongoDB
        }

        public void Remove(T item)
        {
            // Impl spécifique à MongoDB
        }

        public void SaveAll(List<T> items)
        {
            // Impl spécifique à MongoDB
        }
    }
}
