using System.Globalization;

namespace EmilsWork.EmilsCMS
{
    internal partial class CMSClasses
    {
        // =============================================================================
        // CLASSES DE SUPPORT
        // =============================================================================
        // Classes utilisées par le template
        // Ajoutez vos propres classes en dessous
        // =============================================================================

        /// <summary>
        /// Structure d'un menu : noms affichés, touches et actions associées
        /// </summary>
        public class MenuChar
        {
            public List<string> MenuNames { get; set; } = [];
            public List<char> Chars { get; set; } = [];
            public List<Action> Actions { get; set; } = [];
            public Action? OnError { get; set; } = null;
        }

        /// <summary>
        /// Paramètres de l'application (sauvegardés en JSON)
        /// Ajoutez vos propres paramètres ici
        /// </summary>
        public class AppSettings
        {
            /// <summary>Information utilisateur sauvegardée localement et utilisée pour construire les settings MongoDB runtime.</summary>
            public string? MongoDbPassword { get; set; } = null;
        }

        // =============================================================================
        // VOS CLASSES PERSONNALISÉES
        // =============================================================================
        // Ouvrage représente tous les types (périodique, livre, BD) avec champs
        // communs et un objet générique `Details` pour les propriétés spécifiques.
        // =============================================================================

        public class Ouvrage
        {
            public int Id { get; set; }
            public string Titre { get; set; } = string.Empty;
            public int Dispo { get; set; }
            public decimal Prix { get; set; }
        }

        public class Periodique : Ouvrage
        {
            public DateTime? Date { get; set; }
            public string Periodicite { get; set; } = string.Empty;
        }

        public class Livre : Ouvrage
        {
            public List<string> Exemplaires { get; set; } = [];
            public int? Annee { get; set; }
            public string MaisonEdition { get; set; } = string.Empty;
            public string Auteur { get; set; } = string.Empty;
        }

        public class BandeDessine : Livre
        {
            public string Dessinateur { get; set; } = string.Empty;
        }
    }
}
