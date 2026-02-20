using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
using static EmilsWork.EmilsCMS.CMSClasses;
using static EmilsWork.EmilsCMS.Helpers;
using Formatting = Newtonsoft.Json.Formatting;

namespace EmilsWork.EmilsCMS
{
    internal class EmilsCMSCore
    {
        // Optional delegate to call the user-provided main menu
        private Action? userMainMenuAction;

        public EmilsCMSCore(Action? mainMenuAction = null)
        {
            userMainMenuAction = mainMenuAction;
        }
        bool needLoad = true;                   // Flag pour charger les données au démarrage


        // Global data
        RepositoryOuvrages ouvrages = null!; // Liste des ouvrages

        // Expose repository to user apps
        public RepositoryOuvrages Ouvrages => ouvrages;

        // =================================================================
        // POINT D'ENTRÉE - Gestion des erreurs fatales
        // =================================================================
        // Le try/catch global permet de redémarrer l'app en cas de crash
        // =================================================================

        public void RunApp()
        {
            try
            {
                MainMenu();
            }
            catch (Exception ex)
            {
                ClearConsole();
                Console.WriteLine("=== ERREUR FATALE ===");
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                Console.WriteLine("Appuyez sur une touche pour redémarrer...");
                Console.ReadKey();

                if (Globals.noCrashMode)
                    RunApp();
            }

            ExitApp();
        }

        // =================================================================
        // MENU PRINCIPAL
        // =================================================================
        // Point d'entrée de l'application après le chargement
        // Modifiez MenuNames, Chars et Actions pour personnaliser le menu
        // =================================================================

        public void MainMenu(bool showError = false)
        {
            ClearConsole();

            // Affiche un message d'erreur si la commande précédente était invalide
            if (showError)
            {
                Console.WriteLine("[!] Commande non reconnue");
                Console.WriteLine();
            }

            // --- En-tête de l'application ---
            Console.WriteLine(Globals.AppHeader);
            Console.WriteLine($"=== {Globals.AppName} v{Globals.AppVersion} ===");
            Console.WriteLine();

            // --- Chargement des données au premier affichage ---
            if (needLoad)
            {
                LoadSettings();
                InitializeMongoDBRepository();
                needLoad = false;
                Console.WriteLine();
            }

            // If a user app registered a main menu, call it; otherwise exit gracefully
            if (userMainMenuAction != null)
            {
                userMainMenuAction();
            }
            else
            {
                Console.WriteLine("[WARN] No user application registered. Exiting.");
                ExitApp();
            }
        }

        /// <summary>
        /// Register a user-provided menu action (allows manual registration).
        /// </summary>
        /// <param name="menuAction">An action that shows the user's main menu.</param>
        public void RegisterUserMainMenu(Action menuAction)
        {
            userMainMenuAction = menuAction ?? throw new ArgumentNullException(nameof(menuAction));
        }

        // Reflection-based discovery removed in favor of explicit registration.

        public void ShowInfo()
        {
            ClearConsole();
            Console.WriteLine("=== INFORMATIONS ===");
            Console.WriteLine();
            Console.WriteLine($"Application : {Globals.AppName}");
            Console.WriteLine($"Version : {Globals.AppVersion}");
            Console.WriteLine();
            Console.WriteLine("Ce template fournit :");
            Console.WriteLine("- Système de menu navigable");
            Console.WriteLine("- Sauvegarde/chargement JSON automatique");
            Console.WriteLine("- Structure modulaire extensible");
            Console.WriteLine();
            Console.WriteLine($"Application faite par {Globals.Createur} dans le but de faire une preuve de concept MongoDB");
            Console.WriteLine();
            Console.WriteLine("Appuyez sur Entrée pour revenir...");
            Console.ReadLine();
            MainMenu();
        }

        // =================================================================
        // PARAMÈTRES
        // =================================================================
        // Menu de configuration de l'application
        // Les paramètres sont sauvegardés dans settings.json
        // =================================================================

        public void SettingsMenu(bool showError = false)
        {
            ClearConsole();

            if (showError)
            {
                Console.WriteLine("[!] Commande non reconnue");
                Console.WriteLine();
            }

            Console.WriteLine("=== PARAMÈTRES ===");
            Console.WriteLine();

            string mongoStatus = Globals.Settings.MongoDbPassword != null ? "Configuré ✓" : "Non configuré ✗";
            Console.WriteLine($"MongoDB : {mongoStatus}");
            Console.WriteLine();

            MenuChar currentMenu = new()
            {
                MenuNames =
                [
                    "1. Configurer mot de passe MongoDB",
                    "",
                    "Q. Retour au menu principal"
                ],
                Chars = ['1', 'q'],
                Actions =
                [
                    () => ConfigureMongoPassword(),
                    () => MainMenu()
                ],
                OnError = () => { SettingsMenu(true); }
            };

            ProcessMenuInput(currentMenu);
        }

        public void ConfigureMongoPassword()
        {
            ClearConsole();
            Console.WriteLine("=== CONFIGURATION MONGODB ===");
            Console.WriteLine();

            string password = AskUsers<string>("Mot de passe MongoDB : ");

            if (!string.IsNullOrWhiteSpace(password))
            {
                Globals.Settings.MongoDbPassword = password;
                SaveSettings();
                Console.WriteLine();
                Console.WriteLine("[OK] Configuré ✓");
                Console.WriteLine();
                Console.WriteLine("Redémarrez l'application pour utiliser MongoDB");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("[ANNULÉ]");
            }

            Console.WriteLine("Appuyez sur Entrée...");
            Console.ReadLine();
            SettingsMenu();
        }

        // =================================================================
        // CHARGEMENT / SAUVEGARDE
        // =================================================================
        // Fonctions pour persister les données entre les sessions
        // Modifiez ces fonctions pour ajouter vos propres données
        // =================================================================

        void LoadSettings()
        {
            try
            {
                if (File.Exists(Globals.SettingsFile))
                {
                    string json = File.ReadAllText(Globals.SettingsFile);
                    AppSettings? loaded = JsonConvert.DeserializeObject<AppSettings>(json);
                    if (loaded != null)
                    {
                        Globals.Settings = ApplyDefaults(loaded);
                        Console.WriteLine($"[OK] Paramètres chargés ({Globals.SettingsFile})");
                    }
                }
                else
                {
                    Console.WriteLine("[INFO] Aucun fichier de paramètres trouvé, utilisation des valeurs par défaut");
                    Globals.Settings = ApplyDefaults(Globals.Settings);
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERREUR] Impossible de charger les paramètres : {ex.Message}");
            }
        }

        void SaveSettings()
        {
            try
            {
                Globals.Settings = ApplyDefaults(Globals.Settings);
                string json = JsonConvert.SerializeObject(Globals.Settings, Formatting.Indented);
                File.WriteAllText(Globals.SettingsFile, json);
                Console.WriteLine($"[OK] Paramètres sauvegardés ({Globals.SettingsFile})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERREUR] Impossible de sauvegarder : {ex.Message}");
            }
        }

        private static AppSettings ApplyDefaults(AppSettings current)
        {
            current ??= new AppSettings();
            return current;
        }

        /// <summary>
        /// Initialise le repository via IService<Ouvrage> (MongoDB par défaut)
        /// </summary>
        void InitializeMongoDBRepository()
        {
            Console.WriteLine("[INFO] Chargement depuis MongoDB...");

            if (string.IsNullOrWhiteSpace(Globals.Settings.MongoDbPassword))
            {
                Console.WriteLine("[WARN] MongoDB non configuré");
                return;
            }

            MongoDBServiceSettings settingsService = new()
            {
                AppName = "AppName",
                Password = Globals.Settings.MongoDbPassword ?? "Empty",
                DatabaseName = "BibliothequeDB",
                CollectionName = "Ouvrages",
            };

            try
            {
                IService<Ouvrage> service = new MongoDBService<Ouvrage>(settingsService, ConfigureOuvrageBsonMaps);
                ouvrages = new RepositoryOuvrages(service);
                ouvrages.GetAllOuvrages();
                Console.WriteLine($"[OK] {ouvrages.Ouvrages.Count} ouvrages chargés (MongoDB)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERREUR] MongoDB : {ex.Message}");
            }
        }

        private static void ConfigureOuvrageBsonMaps()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(Ouvrage)))
            {
                BsonClassMap.RegisterClassMap<Ouvrage>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIsRootClass(true);
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(Livre)))
            {
                BsonClassMap.RegisterClassMap<Livre>(cm => cm.AutoMap());
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(BandeDessine)))
            {
                BsonClassMap.RegisterClassMap<BandeDessine>(cm => cm.AutoMap());
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(Periodique)))
            {
                BsonClassMap.RegisterClassMap<Periodique>(cm => cm.AutoMap());
            }
        }

        public static void ExitApp()
        {
            ClearConsole();
            Console.WriteLine("=== FERMETURE ===");
            Console.WriteLine();
            Console.WriteLine("Au revoir !");
            Environment.Exit(0);
        }

        // =================================================================
        // SYSTÈME DE MENU
        // =================================================================
        // Ne pas modifier sauf si vous comprenez le fonctionnement
        // =================================================================

        /// <summary>
        /// Affiche le menu et traite l'entrée utilisateur
        /// </summary>
        /// <param name="menu">Configuration du menu à afficher</param>
        public static void ProcessMenuInput(MenuChar menu)
        {
            // Affiche toutes les lignes du menu
            foreach (string line in menu.MenuNames)
            {
                Console.WriteLine(line);
            }

            // Attend une touche et la convertit en minuscule
            char input = char.ToLower(Console.ReadKey(true).KeyChar);
            Console.WriteLine();

            // Cherche l'action correspondante
            for (int i = 0; i < menu.Chars.Count; i++)
            {
                if (menu.Chars[i] == input)
                {
                    menu.Actions[i]();
                    return;
                }
            }

            if (menu.OnError != null)
            {
                menu.OnError();
                return;
            }
            else
            {
                // If caller did not provide an OnError handler, simply return to allow the
                // caller to decide how to continue. Previously this attempted to call
                // the instance MainMenu from a static method which is invalid.
                Console.WriteLine("[WARN] Invalid menu input.");
                return;
            }
        }

    }
}

