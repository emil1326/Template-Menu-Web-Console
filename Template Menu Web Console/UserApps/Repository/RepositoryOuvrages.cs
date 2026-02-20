using System;
using System.Collections.Generic;
using System.Linq;

namespace EmilsWork.EmilsCMS
{
    // User-side repository implementation for Ouvrages.
    public class RepositoryOuvrages : RepositoryBase<Ouvrage>
    {
        private readonly IService<Ouvrage> service;

        public RepositoryOuvrages(IService<Ouvrage> svc)
        {
            service = svc ?? throw new ArgumentNullException(nameof(svc));
            Items = new List<Ouvrage>();
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
            Items = service.GetAll() ?? new List<Ouvrage>();
        }

        public void GetAllOuvrages() => Load();

        public List<Ouvrage> GetOuvragesByType<T>() where T : Ouvrage
        {
            return Items.OfType<T>().Cast<Ouvrage>().ToList();
        }

        public List<Ouvrage> GetOuvragesByQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<Ouvrage>(Items);
            return Items.Where(o => !string.IsNullOrWhiteSpace(o.Titre) && o.Titre.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public Ouvrage? GetOuvrageById(int id)
        {
            string sid = id.ToString();
            var found = Items.FirstOrDefault(o => o.Id == sid);
            if (found != null) return found;
            if (id > 0 && id <= Items.Count) return Items[id - 1];
            return null;
        }

        public void AddOuvrage(Ouvrage item)
        {
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
