using System.Collections.Generic;

using static EmilsWork.EmilsCMS.CMSClasses;

namespace EmilsWork.EmilsCMS
{
    internal interface IService<T> where T : class
    {
        List<T> LoadAllAsync();
        void SaveAll(List<T> ouvrages);
        void Save(T ouvrage);
        void Delete(string id);
        void Delete(T ouvrage);
        void Update(string id);
        void Update(T ouvrage);
        T? GetById(string id);
        List<T> GetAll();
        List<T> GetByQuery(string query);
        List<T> GetByType(string type);
    }
}
