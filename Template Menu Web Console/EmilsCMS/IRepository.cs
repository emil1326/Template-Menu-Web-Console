using System;
using System.Collections.Generic;
using System.Linq;

namespace EmilsWork.EmilsCMS
{
    internal interface IRepository<T> where T : class
    {
        List<T> Items { get; set; }
        IService<T> Service { get; set; }
        int NextId { get; }

        void Add(T item);
        void Remove(T item);
        void RemoveById(int id);
        void Update(T item);
        T? GetById(int id);
        List<T> GetAll();
        List<T> GetByQuery(string query);
        List<T> GetByType(string type);
    }

    internal abstract class RepositoryBase<T> : IRepository<T> where T : class
    {
        public List<T> Items { get; set; } = [];
        public IService<T> Service { get; set; }

        protected RepositoryBase(IService<T> service)
        {
            Service = service;
        }

        public virtual int NextId
        {
            get
            {
                var prop = typeof(T).GetProperty("Id");
                if (Items.Count == 0 || prop == null)
                    return 1;

                var maxId = Items
                    .Select(i => prop.GetValue(i))
                    .Select(v => int.TryParse(v?.ToString(), out var parsed) ? parsed : 0)
                    .DefaultIfEmpty(0)
                    .Max();

                return maxId + 1;
            }
        }

        public virtual void Add(T item)
        {
            if (item == null)
                return;

            AssignIdIfMissing(item);
            Items.Add(item);
            Service?.Save(item);
        }

        public virtual void Remove(T item)
        {
            if (item == null)
                return;

            Items.Remove(item);
            Service?.Delete(item);
        }

        public virtual void RemoveById(int id)
        {
            var existing = GetById(id);
            if (existing != null)
            {
                Remove(existing);
            }
        }

        public virtual void Update(T item)
        {
            if (item == null)
                return;

            var existing = GetById(GetIdValue(item));
            if (existing != null)
            {
                int index = Items.IndexOf(existing);
                if (index >= 0)
                    Items[index] = item;
            }
            else
            {
                Items.Add(item);
            }

            Service?.Update(item);
        }

        public virtual T? GetById(int id)
        {
            var fromService = Service?.GetById(id.ToString());
            if (fromService != null)
            {
                var existing = Items.FirstOrDefault(i => HasMatchingId(i, id));
                if (existing != null)
                {
                    int index = Items.IndexOf(existing);
                    Items[index] = fromService;
                }
                else
                {
                    Items.Add(fromService);
                }
            }

            return Items.FirstOrDefault(i => HasMatchingId(i, id));
        }

        protected virtual T? GetById(string id)
        {
            return Service?.GetById(id);
        }

        public virtual List<T> GetAll()
        {
            Items = Service?.GetAll() ?? [];
            return Items;
        }

        public virtual List<T> GetByQuery(string query)
        {
            return Service?.GetByQuery(query) ?? GetAll();
        }

        public virtual List<T> GetByType(string type)
        {
            return Service?.GetByType(type) ?? GetAll();
        }

        protected virtual void AssignIdIfMissing(T item)
        {
            var prop = typeof(T).GetProperty("Id");
            if (prop == null || !prop.CanWrite)
                return;

            var current = prop.GetValue(item);
            if (current == null || (current is int intVal && intVal == 0))
            {
                prop.SetValue(item, NextId);
            }
        }

        protected virtual int GetIdValue(T item)
        {
            var prop = typeof(T).GetProperty("Id");
            var value = prop?.GetValue(item);
            return int.TryParse(value?.ToString(), out var parsed) ? parsed : 0;
        }

        protected virtual bool HasMatchingId(T item, int id)
        {
            return GetIdValue(item) == id;
        }
    }
}
