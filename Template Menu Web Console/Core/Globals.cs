// static import removed
using System;
using System.IO;
using System.Reflection;

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
        public const string AppVersion = "1.1";        // Version de l'application
        public const string AppName = "Mongo console app";  // Nom de l'application
        public const string SettingsFile = "settings.json";  // Fichier de configuration
        public const string Createur = "Emilien Devauchelle"; // Information sur le createur
        public const string Compagnie = "Emil's works"; // Information de compagnie
        public static readonly string AppDate = GetBuildTime(); // Last build timestamp (computed at runtime)

        // logging verbosity threshold; messages with severity lower than this
        // value will not be shown on the console but still written to file.
        // higher number = more important; default 500 (mid).
        public static int LogSeverityThreshold = 500;

        public static readonly string AppHeader = $@"                                         
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

        private static string GetBuildTime()
        {
            try
            {
                var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var path = asm?.Location;
                if (string.IsNullOrEmpty(path))
                    return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                var dt = File.GetLastWriteTime(path);
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
    }
}
