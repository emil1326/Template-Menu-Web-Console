using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
// using static imports removed to avoid duplicate-type/ambiguous references

namespace EmilsWork.EmilsCMS
{
    internal class CMSCore
    {
        // static reference for cleanup in ExitApp
        public static CMSCore? Current { get; private set; }

        // Optional delegate to call the user-provided main menu
        private Action? userMainMenuAction;

        public CMSCore(Action? mainMenuAction = null)
        {
            Current = this;
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

        // applications registered by the user or router
        private readonly List<App> childApps = new List<App>();

        public void RegisterChildApp(App app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            childApps.Add(app);
        }

        private void InitializeApps()
        {
            foreach (var a in childApps)
                a.InitializeTree();
        }

        private void CleanupApps()
        {
            foreach (var a in childApps)
                a.CleanupTree();
        }

        public void RunApp()
        {
            InitializeApps();
            try
            {
                MainMenu();
            }
            catch (AppError appErr)
            {
                // show user-friendly screen and then restart if configured
                appErr.Render();
                if (Globals.noCrashMode)
                    RunApp();
            }
            catch (Exception ex)
            {

                // log the unexpected error with a stack trace
                Logger.LogException(ex);

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
                Logger.Warn("Commande non reconnue");
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
                Logger.Warn("No user application registered. Exiting.");
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

        void LoadSettings()
        {
            try
            {
                // Use a simple JsonFileService + SettingsRepository so settings persistence is centralized
                var svc = new JsonFileService<AppSettings>(new JsonFileServiceSettings { FilePath = Globals.SettingsFile });
                settingsRepo = new SettingsRepository(svc);
                settingsRepo.GetAll();

                if (settingsRepo.Items.Count > 0)
                {
                    Globals.Settings = ApplyDefaults(settingsRepo.GetSettingsOrDefault());
                    Logger.Log($"Paramètres chargés ({Globals.SettingsFile})", severity: 100);
                }
                else
                {
                    Logger.Warn("Aucun fichier de paramètres trouvé, utilisation des valeurs par défaut", severity: 100);
                    Globals.Settings = ApplyDefaults(Globals.Settings);
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                throw new AppError(ErrorCode.DataSource,
                    "Impossible de charger les paramètres",
                    ex,
                    userMessage: "Échec du chargement des paramètres",
                    severity:1000);
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
                    var svc = new JsonFileService<AppSettings>(new JsonFileServiceSettings { FilePath = Globals.SettingsFile });
                    settingsRepo = new SettingsRepository(svc);
                }

                settingsRepo.SaveSingle(Globals.Settings);
                Logger.Log($"Paramètres sauvegardés ({Globals.SettingsFile})");
                Console.WriteLine($"[OK] Paramètres sauvegardés ({Globals.SettingsFile})");
            }
            catch (Exception ex)
            {
                throw new AppError(ErrorCode.DataSource,
                    "Impossible de sauvegarder les paramètres",
                    ex,
                    userMessage: "Échec de la sauvegarde des paramètres");
            }
        }

        /// <summary>
        /// Persists current runtime settings to the configured settings store.
        /// </summary>
        public void PersistSettings()
        {
            SaveSettings();
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
            Logger.Log("Chargement depuis MongoDB...");
            Console.WriteLine("[INFO] Chargement depuis MongoDB...");

            if (string.IsNullOrWhiteSpace(Globals.Settings.MongoDbPassword))
            {
                Logger.Warn("MongoDB non configuré - utilisation du stockage local (JSON)");
                try
                {
                    var svcLocal = new JsonFileService<Ouvrage>(new JsonFileServiceSettings { FilePath = "ouvrages.json" });
                    ouvrages = new RepositoryOuvrages(svcLocal);
                    var load = ouvrages.GetAllOuvrages();
                    if (!load.IsSuccess)
                    {
                        Logger.Error(load.Error?.TechnicalMessage ?? string.Empty);
                        Console.WriteLine($"[ERREUR] {load.Error?.ToUserMessage()}");
                    }
                    Logger.Log($"{ouvrages.Items.Count} ouvrages chargés (JSON)");
                    Console.WriteLine($"[OK] {ouvrages.Items.Count} ouvrages chargés (JSON)");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Impossible d'initialiser le repository local : {ex.Message}");
                    Console.WriteLine($"[ERREUR] Impossible d'initialiser le repository local : {ex.Message}");
                }
                return;
            }

            MongoDBServiceSettings settingsService = new()
            {
                AppName = Globals.AppName,
                User = Globals.Settings.MongoUser ?? string.Empty,
                Host = Globals.Settings.MongoHost ?? string.Empty,
                Password = Globals.Settings.MongoDbPassword ?? "Empty",
                DatabaseName = Globals.Settings.MongoDatabase ?? "BibliothequeDB",
                CollectionName = Globals.Settings.MongoCollection ?? "Ouvrages",
            };

            try
            {
                IService<Ouvrage> service = new MongoDBService<Ouvrage>(settingsService, ConfigureOuvrageBsonMaps);
                ouvrages = new RepositoryOuvrages(service);
                var load = ouvrages.GetAllOuvrages();
                if (!load.IsSuccess)
                {
                    Logger.Error(load.Error?.TechnicalMessage ?? string.Empty);
                    Console.WriteLine($"[ERREUR] {load.Error?.ToUserMessage()}");
                }
                Logger.Log($"{ouvrages.Items.Count} ouvrages chargés (MongoDB)");
                Console.WriteLine($"[OK] {ouvrages.Items.Count} ouvrages chargés (MongoDB)");
            }
            catch (Exception ex)
            {
                throw new AppError(ErrorCode.DataSource,
                    "MongoDB initialisation failure",
                    ex,
                    userMessage: "Erreur lors de l'initialisation de MongoDB");
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
            // run any registered cleanup handlers first
            Current?.CleanupApps();

            Helpers.ClearConsole();
            Console.WriteLine("=== FERMETURE ===");
            Console.WriteLine();
            Console.WriteLine("Au revoir !");
            Environment.Exit(0);
        }

    }
}
