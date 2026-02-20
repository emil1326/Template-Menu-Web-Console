using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using static EmilsWork.EmilsCMS.CMSClasses;

namespace EmilsWork.EmilsCMS
{
    internal partial class CMSClasses
    {
        public class RepositoryOuvrages : RepositoryBase<Ouvrage>
        {
            public List<Ouvrage> Ouvrages
            {
                get => Items;
                set => Items = value;
            }

            [JsonIgnore]
            public override int NextId => base.NextId;

            public RepositoryOuvrages(IService<Ouvrage> service) : base(service)
            {

            }

            public void AddOuvrage(Ouvrage ouvrage)
            {
                Add(ouvrage);
            }

            public void RemoveOuvrage(Ouvrage ouvrage)
            {
                Remove(ouvrage);
            }

            public void RemoveOuvrageById(int id)
            {
                RemoveById(id);
            }

            public void UpdateOuvrage(Ouvrage updatedOuvrage)
            {
                Update(updatedOuvrage);
            }

            public Ouvrage? GetOuvrageById(int id)
            {
                return GetById(id);
            }

            public List<Ouvrage> GetAllOuvrages()
            {
                return GetAll();
            }

            public List<Ouvrage> GetOuvragesByQuery(string query)
            {
                if (string.IsNullOrWhiteSpace(query))
                    return GetAllOuvrages();

                Ouvrages = Service.GetByQuery(query);

                var fieldFilters = ParseQuery(query.Trim());
                return [.. Ouvrages.Where(o => MatchesAllFilters(o, fieldFilters))];
            }

            #region regex back

            private Dictionary<string, List<string>> ParseQuery(string query)
            {
                var filters = new Dictionary<string, List<string>>();

                if (!query.Contains(':'))
                {
                    // Recherche simple globale
                    filters["global"] = [query];
                    return filters;
                }

                // Découper la requête en filtres individuels
                var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    if (!part.Contains(':'))
                    {
                        AddFilter(filters, "global", part);
                        continue;
                    }

                    var keyValue = part.Split(':', 2);
                    if (keyValue.Length == 2)
                    {
                        string field = keyValue[0].ToLower();
                        string value = keyValue[1].Trim();
                        AddFilter(filters, field, value);
                    }
                }

                return filters;
            }

            private void AddFilter(Dictionary<string, List<string>> filters, string field, string value)
            {
                if (!filters.ContainsKey(field))
                    filters[field] = new List<string>();

                filters[field].Add(value);
            }

            private bool MatchesAllFilters(Ouvrage ouvrage, Dictionary<string, List<string>> filters)
            {
                foreach (var filter in filters)
                {
                    if (!AllValuesMatch(ouvrage, filter.Key, filter.Value))
                        return false;
                }
                return true;
            }

            private bool AllValuesMatch(Ouvrage ouvrage, string field, List<string> values)
            {
                foreach (var value in values)
                {
                    if (!MatchesSingleFilter(ouvrage, field, value))
                        return false;
                }
                return true;
            }

            private bool Match(string fieldValue, string pattern)
            {
                if (string.IsNullOrEmpty(fieldValue))
                    return false;

                var regex = new System.Text.RegularExpressions.Regex(
                    ToRegexPattern(pattern),
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                return regex.IsMatch(fieldValue);
            }

            private bool MatchGlobal(Ouvrage ouvrage, string pattern)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(ouvrage, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                var regex = new System.Text.RegularExpressions.Regex(
                    ToRegexPattern(pattern),
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                return regex.IsMatch(json);
            }

            private string ToRegexPattern(string query)
            {
                // Convertir SQL LIKE vers regex: % → .* et _ → .
                string pattern = query.Replace("%", ".*").Replace("_", ".");

                // Si pas de wildcards, ajouter .* au début et fin
                if (!query.Contains('%') && !query.Contains('_'))
                {
                    pattern = ".*" + System.Text.RegularExpressions.Regex.Escape(pattern) + ".*";
                }
                else
                {
                    // Échapper les caractères spéciaux sauf .* déjà convertis
                    var parts = pattern.Split(new[] { ".*" }, StringSplitOptions.None);
                    for (int i = 0; i < parts.Length; i++)
                        parts[i] = System.Text.RegularExpressions.Regex.Escape(parts[i]);

                    pattern = string.Join(".*", parts);
                }

                return pattern;
            }

            #endregion regex back

            private bool MatchesSingleFilter(Ouvrage ouvrage, string field, string value)
            {
                return field switch
                {
                    // Champs communs
                    "id" => Match(ouvrage.Id.ToString(), value),
                    "titre" => Match(ouvrage.Titre, value),
                    "dispo" or "disponibilite" => Match(ouvrage.Dispo.ToString(), value),
                    "prix" => Match(ouvrage.Prix.ToString(), value),

                    // Champs Livre/BD
                    "auteur" => ouvrage is Livre l && Match(l.Auteur, value),
                    "annee" or "année" => ouvrage is Livre lv && lv.Annee.HasValue && Match(lv.Annee.Value.ToString(), value),
                    "maison" or "maisonedition" or "edition" => ouvrage is Livre liv && Match(liv.MaisonEdition, value),
                    "exemplaires" or "exemplaire" => ouvrage is Livre livre && livre.Exemplaires.Any(ex => Match(ex, value)),

                    // Champs BD uniquement
                    "dessinateur" => ouvrage is BandeDessine bd && Match(bd.Dessinateur, value),

                    // Champs Périodique
                    "date" => ouvrage is Periodique p && p.Date.HasValue && Match(p.Date.Value.ToString("yyyy-MM-dd"), value),
                    "periodicite" or "périodicité" => ouvrage is Periodique per && Match(per.Periodicite, value),

                    // Recherche globale
                    "global" => MatchGlobal(ouvrage, value),

                    _ => false
                };
            }


            public List<T> GetOuvragesByType<T>() where T : Ouvrage
            {
                Ouvrages = Service.GetByType(typeof(T).ToString());
                return [.. Ouvrages.OfType<T>()];
            }

            public List<Ouvrage> GetOuvragesByTypeAndQuery<T>(string query) where T : Ouvrage
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return [.. GetOuvragesByType<T>().Cast<Ouvrage>()];
                }

                // Utiliser le même système de filtres typés
                return [.. GetOuvragesByQuery(query).OfType<T>().Cast<Ouvrage>()];
            }
        }
    }
}
