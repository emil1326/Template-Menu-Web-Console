using static EmilsWork.EmilsCMS.CMSClasses;

namespace EmilsWork.EmilsCMS
{
    internal class Globals
    {
        // =================================================================
        // VARIABLES GLOBALES
        // =================================================================
        // Ces variables sont accessibles par toutes les fonctions locales
        // Ajoutez vos propres listes/données ici pour les rendre persistantes
        // =================================================================

        public const bool noCrashMode = true; // Si true, l'application redémarrera automatiquement après une exception non gérée au lieu de fermer.
        public static AppSettings Settings = new();
        // infos
        public const string AppVersion = "1.0";        // Version de l'application
        public const string AppName = "Mongo console app";  // Nom de l'application
        public const string SettingsFile = "settings.json";  // Fichier de configuration
        public const string Createur = "Emilien Devauchelle et Jonathan Basque"; // Information sur le createur
        public const string Compagnie = "Emil's works"; // Information de compagnie
        public const string AppDate = "2026-02-05"; // Date de création ou de dernière mise à jour
        public const string AppHeader = $@"                                         
 /$$      /$$                                         /$$ /$$$$$$$  /$$$$$$$ 
| $$$    /$$$                                        | $$| $$__  $$| $$__  $$
| $$$$  /$$$$  /$$$$$$  /$$$$$$$   /$$$$$$   /$$$$$$ | $$| $$  \ $$| $$  \ $$
| $$ $$/$$ $$ /$$__  $$| $$__  $$ /$$__  $$ /$$__  $$| $$| $$  | $$| $$$$$$$ 
| $$  $$$| $$| $$  \ $$| $$  \ $$| $$  \ $$| $$  \ $$| $$| $$  | $$| $$__  $$
| $$\  $ | $$| $$  | $$| $$  | $$| $$  | $$| $$  | $$| $$| $$  | $$| $$  \ $$
| $$ \/  | $$|  $$$$$$/| $$  | $$|  $$$$$$$|  $$$$$$/| $$| $$$$$$$/| $$$$$$$/
|__/     |__/ \______/ |__/  |__/ \____  $$ \______/ |__/|_______/ |_______/ 
                                  /$$  \ $$                                  
                                 |  $$$$$$/                                  
                                  \______/                                   
                  /$$                                          {AppDate}     
                 /$$/                                                        
         /$$    /$$/                                                         
        |__/   /$$/                                                          
              /$$/                                                           
         /$$ /$$/                                                            
        |__//$$/                                                             
           |__/                                                              
";
    }
}
