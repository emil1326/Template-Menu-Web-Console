using System.Collections.Generic;

namespace EmilsWork.EmilsCMS
{
    public abstract class RepositoryBase<T> : IRepository<T>
    {
        public List<T> Items { get; protected set; } = [];

        public abstract void Add(T item);
        public abstract void Remove(T item);
        public abstract void Save();
        public abstract void Load();
    }
}
