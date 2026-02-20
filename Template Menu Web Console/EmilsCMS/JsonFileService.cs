using Newtonsoft.Json;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Service de persistance JSON local implémentant les mêmes opérations que MongoDBService
    /// </summary>
    internal class JsonFileService<T> : IService<T> where T : class
    {
        private readonly string _filePath;
        private static readonly JsonSerializerSettings _settings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented
        };

        public JsonFileService(string filePath = "data.json")
        {
            _filePath = filePath;
        }

        public List<T> LoadAllAsync() => GetAll();

        public void SaveAll(List<T> items)
        {
            var data = items ?? [];
            string json = JsonConvert.SerializeObject(data, _settings);
            File.WriteAllText(_filePath, json);
        }

        public void Save(T item)
        {
            if (item == null)
                return;

            var all = GetAll();
            all.Add(item);
            SaveAll(all);
        }

        public void Delete(string id)
        {
            var all = GetAll();
            var removed = all.RemoveAll(o => HasMatchingId(o, id)) > 0;
            if (removed)
            {
                SaveAll(all);
            }
        }

        public void Delete(T item)
        {
            if (item == null)
                return;

            Delete(GetIdAsString(item));
        }

        public void Update(string id)
        {
            var item = GetById(id);
            if (item != null)
            {
                Update(item);
            }
        }

        public void Update(T item)
        {
            if (item == null)
                return;

            var all = GetAll();
            var index = all.FindIndex(o => AreSameId(o, item));

            if (index >= 0)
            {
                all[index] = item;
            }
            else
            {
                all.Add(item);
            }

            SaveAll(all);
        }

        public T? GetById(string id)
        {
            return GetAll().FirstOrDefault(o => AreSameId(o, id));
        }

        public List<T> GetAll()
        {
            if (!File.Exists(_filePath))
                return [];

            try
            {
                string json = File.ReadAllText(_filePath);
                return JsonConvert.DeserializeObject<List<T>>(json, _settings) ?? [];
            }
            catch
            {
                return [];
            }
        }

        public List<T> GetByQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return GetAll();

            // Filtrage avancé laissé à l'appelant
            return GetAll();
        }

        public List<T> GetByType(string type)
        {
            var all = GetAll();

            if (string.IsNullOrWhiteSpace(type))
                return all;

            return [.. all.Where(o => o?.GetType().Name.Contains(type, StringComparison.OrdinalIgnoreCase) == true)];
        }

        private static bool HasMatchingId(T item, string id)
        {
            var prop = typeof(T).GetProperty("Id");
            if (prop == null)
                return false;

            var value = prop.GetValue(item)?.ToString();
            return value != null && value == id;
        }

        private static bool AreSameId(T left, T right)
        {
            var prop = typeof(T).GetProperty("Id");
            if (prop == null)
                return false;

            var l = prop.GetValue(left)?.ToString();
            var r = prop.GetValue(right)?.ToString();
            return l != null && r != null && l == r;
        }

        private static bool AreSameId(T item, string id)
        {
            var prop = typeof(T).GetProperty("Id");
            var value = prop?.GetValue(item)?.ToString();
            return value != null && value == id;
        }

        private static string GetIdAsString(T item)
        {
            var prop = typeof(T).GetProperty("Id");
            var value = prop?.GetValue(item);
            return value?.ToString() ?? string.Empty;
        }
    }
}
