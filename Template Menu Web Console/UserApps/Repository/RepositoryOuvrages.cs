using System;
using System.Collections.Generic;
using System.Linq;

namespace EmilsWork.EmilsCMS
{
    // User-side repository implementation for Ouvrages.
    public class RepositoryOuvrages : RepositoryBase<Ouvrage>
    {
        public RepositoryOuvrages(IService<Ouvrage> svc)
            : base(svc)
        {
        }

        public Result GetAllOuvrages()
        {
            var result = GetAll();
            return result.IsSuccess ? Result.Success() : Result.Failure(result.Error!);
        }

        public List<Ouvrage> GetOuvragesByType<T>() where T : Ouvrage
        {
            return Items.OfType<T>().Cast<Ouvrage>().ToList();
        }

        public List<Ouvrage> GetOuvragesByQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<Ouvrage>(Items);
            return Items.Where(o => !string.IsNullOrWhiteSpace(o.Titre) && o.Titre.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public Ouvrage? GetOuvrageById(int id, bool useCache = true)
        {
            string sid = id.ToString();
            var fromKey = GetById(sid, useCache);
            if (!fromKey.IsSuccess)
            {
                return null;
            }

            if (fromKey.Value != null)
            {
                return fromKey.Value;
            }

            if (id > 0 && id <= Items.Count)
            {
                return Items[id - 1];
            }

            return null;
        }

        public Result AddOuvrage(Ouvrage item)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                item.Id = (Items.Count + 1).ToString();
            }

            return Add(item);
        }

        public Result UpdateOuvrage(Ouvrage item)
        {
            return Update(item);
        }

        public Result RemoveOuvrageById(int id)
        {
            var o = GetOuvrageById(id);
            if (o != null)
            {
                return Delete(o);
            }

            return Result.Failure(new AppError(ErrorCode.NotFound, $"Ouvrage avec ID '{id}' introuvable."));
        }
    }
}
