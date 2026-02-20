using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
// using static imports removed to avoid duplicate-type/ambiguous references
using Formatting = Newtonsoft.Json.Formatting;

namespace EmilsWork.EmilsCMS
{
    internal class CMSCore
    {
        // Optional delegate to call the user-provided main menu
        private Action? userMainMenuAction;

        public CMSCore(Action? mainMenuAction = null)
        {
            userMainMenuAction = mainMenuAction;
        }
        bool needLoad = true;                   // Flag pour charger les données au démarrage


        // Global data
        RepositoryOuvrages ouvrages = null!; // Liste des ouvrages
        SettingsRepository? settingsRepo;

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
                Helpers.ClearConsole();
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
            Helpers.ClearConsole();

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

        public void ShowInfo()
        {
            Helpers.ClearConsole();
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

        /// <summary>
        /// Simple settings menu placeholder exposed to user apps.
        /// </summary>
        public void SettingsMenu()
        {
            // Use the UI component abstraction to show editable settings
            List<SettingsComponent.SettingEntry> entries =
            [
                new SettingsComponent.SettingEntry(
                    "MongoDB password",
                    () => Globals.Settings.MongoDbPassword ?? string.Empty,
                    v => Globals.Settings.MongoDbPassword = string.IsNullOrWhiteSpace(v) ? null : v
                )
            ];

            var comp = new SettingsComponent(entries, onFinish: () => { SaveSettings(); MainMenu(); });
            comp.Run();
        }

        void LoadSettings()
        {
            try
            {
                // Use a simple JsonFileService + SettingsRepository so settings persistence is centralized
                var svc = new JsonFileService<AppSettings>(Globals.SettingsFile);
                settingsRepo = new SettingsRepository(svc);
                settingsRepo.Load();

                if (settingsRepo.Items.Count > 0)
                {
                    Globals.Settings = ApplyDefaults(settingsRepo.GetSettingsOrDefault());
                    Console.WriteLine($"[OK] Paramètres chargés ({Globals.SettingsFile})");
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

                // Ensure repository exists
                if (settingsRepo == null)
                {
                    var svc = new JsonFileService<AppSettings>(Globals.SettingsFile);
                    settingsRepo = new SettingsRepository(svc);
                }

                settingsRepo.SaveSingle(Globals.Settings);
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
                Console.WriteLine($"[OK] {ouvrages.Items.Count} ouvrages chargés (MongoDB)");
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
            Helpers.ClearConsole();
            Console.WriteLine("=== FERMETURE ===");
            Console.WriteLine();
            Console.WriteLine("Au revoir !");
            Environment.Exit(0);
        }

    }
}
