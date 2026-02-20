using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace EmilsWork.EmilsCMS
{
    public class JsonFileService<T> : IService<T>
    {
        private readonly string filePath;

        public JsonFileService(string path)
        {
            filePath = path;
        }

        public List<T> GetAll()
        {
            if (!File.Exists(filePath)) return new List<T>();
            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<T>>(json) ?? new List<T>();
        }

        public void Add(T item)
        {
            var items = GetAll();
            items.Add(item);
            SaveAll(items);
        }

        public void Remove(T item)
        {
            var items = GetAll();
            items.Remove(item!);
            SaveAll(items);
        }

        public void SaveAll(List<T> items)
        {
            string json = JsonConvert.SerializeObject(items, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }
}
