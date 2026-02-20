using System;
using System.Collections.Generic;

namespace EmilsWork.EmilsCMS
{
    public class RepositoryOuvrages : RepositoryBase<Ouvrage>
    {
        private readonly IService<Ouvrage> service;

        public RepositoryOuvrages(IService<Ouvrage> svc)
        {
            service = svc;
            Items = [];
        }

        public override void Add(Ouvrage item)
        {
            service.Add(item);
            Items.Add(item);
        }

        public override void Remove(Ouvrage item)
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
            Items = service.GetAll();
        }

        public void GetAllOuvrages()
        {
            Load();
        }
        
        public List<Ouvrage> GetOuvragesByType<T>() where T : Ouvrage
        {
            return Items.OfType<T>().Cast<Ouvrage>().ToList();
        }

        public List<Ouvrage> GetOuvragesByQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return [.. Items];
            // Very simple query: search in titre
            return Items.Where(o => o.Titre != null && o.Titre.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public Ouvrage? GetOuvrageById(int id)
        {
            // IDs are stored as strings; try to match numeric string or index
            string sid = id.ToString();
            var found = Items.FirstOrDefault(o => o.Id == sid);
            if (found != null) return found;
            // fallback: treat id as 1-based index
            if (id > 0 && id <= Items.Count) return Items[id - 1];
            return null;
        }

        public void AddOuvrage(Ouvrage item)
        {
            // assign next numeric id if empty
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                item.Id = (Items.Count + 1).ToString();
            }
            Items.Add(item);
            Save();
        }

        public void UpdateOuvrage(Ouvrage item)
        {
            var idx = Items.FindIndex(o => o.Id == item.Id);
            if (idx >= 0) Items[idx] = item;
            Save();
        }

        public void RemoveOuvrageById(int id)
        {
            var o = GetOuvrageById(id);
            if (o != null)
            {
                Items.Remove(o);
                Save();
            }
        }
    }
}
