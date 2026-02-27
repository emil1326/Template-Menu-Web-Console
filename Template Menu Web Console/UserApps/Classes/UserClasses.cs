
namespace EmilsWork.EmilsCMS
{
    public class Ouvrage
    {
        [IsId]
        public string Id { get; set; } = string.Empty;
        public string Titre { get; set; } = string.Empty;
        public int Dispo { get; set; }
        public decimal Prix { get; set; }
        public List<string>? Exemplaires { get; set; }
    }

    public class Livre : Ouvrage
    {
        public string Auteur { get; set; } = string.Empty;
        public int? Annee { get; set; }
        public string? MaisonEdition { get; set; }
    }

    public class BandeDessine : Ouvrage
    {
        public string Auteur { get; set; } = string.Empty;
        public string Dessinateur { get; set; } = string.Empty;
        public int? Annee { get; set; }
    }

    public class Periodique : Ouvrage
    {
        public string Periodicite { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
    }
}
