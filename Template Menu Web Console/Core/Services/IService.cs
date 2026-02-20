using System.Collections.Generic;

namespace EmilsWork.EmilsCMS
{
    public interface IService<T>
    {
        List<T> GetAll();
        void Add(T item);
        void Remove(T item);
        void SaveAll(List<T> items);
    }
}
