using System.Collections.Generic;

namespace EmilsWork.EmilsCMS
{
    public interface IRepository<T>
    {
        List<T> Items { get; }
        void Add(T item);
        void Remove(T item);
        void Save();
        void Load();
    }
}
