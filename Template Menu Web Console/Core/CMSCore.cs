using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
// using static imports removed to avoid duplicate-type/ambiguous references

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
            // Use the UI component abstraction to show editable settings grouped under a MongoDB category
            var entries = new List<SettingsComponent.SettingEntry>
            {
                // Category header
                new SettingsComponent.SettingEntry("=== MongoDB Settings ===", () => string.Empty, _ => { }),

                new SettingsComponent.SettingEntry(
                    "Enabled (true/false)",
                    () => Globals.Settings.MongoEnabled.ToString(),
                    v => Globals.Settings.MongoEnabled = bool.TryParse(v, out var b) ? b : Globals.Settings.MongoEnabled
                ),

                new SettingsComponent.SettingEntry(
                    "Host",
                    () => Globals.Settings.MongoHost ?? string.Empty,
                    v => Globals.Settings.MongoHost = string.IsNullOrWhiteSpace(v) ? null : v
                ),

                new SettingsComponent.SettingEntry(
                    "Port",
                    () => Globals.Settings.MongoPort.ToString(),
                    v => { if (int.TryParse(v, out var p)) Globals.Settings.MongoPort = p; }
                ),

                new SettingsComponent.SettingEntry(
                    "User",
                    () => Globals.Settings.MongoUser ?? string.Empty,
                    v => Globals.Settings.MongoUser = string.IsNullOrWhiteSpace(v) ? null : v
                ),

                new SettingsComponent.SettingEntry(
                    "Password",
                    () => Globals.Settings.MongoDbPassword ?? string.Empty,
                    v => Globals.Settings.MongoDbPassword = string.IsNullOrWhiteSpace(v) ? null : v
                ),

                new SettingsComponent.SettingEntry(
                    "Database",
                    () => Globals.Settings.MongoDatabase ?? string.Empty,
                    v => Globals.Settings.MongoDatabase = string.IsNullOrWhiteSpace(v) ? null : v
                ),

                new SettingsComponent.SettingEntry(
                    "Collection",
                    () => Globals.Settings.MongoCollection ?? string.Empty,
                    v => Globals.Settings.MongoCollection = string.IsNullOrWhiteSpace(v) ? null : v
                ),

                // Test connection action (user enters anything to trigger)
                new SettingsComponent.SettingEntry(
                    "[ACTION] Test MongoDB connection (press any key then Enter)",
                    () => string.Empty,
                    _ => { TestMongoConnection(); }
                )
            };

            var comp = new SettingsComponent(entries, onFinish: () => { SaveSettings(); MainMenu(); });
            comp.Run();
        }

        void TestMongoConnection()
        {
            Helpers.ClearConsole();
            Console.WriteLine("=== TEST MONGODB CONNECTION ===");
            Console.WriteLine();

            if (!Globals.Settings.MongoEnabled)
            {
                Console.WriteLine("MongoDB is disabled in settings.");
                Console.WriteLine("Press Enter to return to settings...");
                Console.ReadLine();
                SettingsMenu();
                return;
            }

            try
            {
                var s = new MongoDBServiceSettings
                {
                    AppName = Globals.AppName,
                    User = Globals.Settings.MongoUser ?? string.Empty,
                    Host = Globals.Settings.MongoHost ?? string.Empty,
                    Password = Globals.Settings.MongoDbPassword ?? string.Empty,
                    DatabaseName = Globals.Settings.MongoDatabase ?? string.Empty,
                    CollectionName = Globals.Settings.MongoCollection ?? string.Empty
                };

                // Attempt to create the service and fetch zero or more items as a connectivity test
                var svc = new MongoDBService<Ouvrage>(s, ConfigureOuvrageBsonMaps);
                var items = svc.ReadAll();
                Logger.Log("MongoDB service instantiated successfully.");
                Console.WriteLine("[OK] MongoDB service instantiated successfully.");
                Console.WriteLine($"Items fetched: {items.Value?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                // log full exception details
                Logger.LogException(ex);
                Console.WriteLine($"[ERROR] Connection test failed: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("Press Enter to return to settings...");
            Console.ReadLine();
            SettingsMenu();
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
                    Logger.Log($"Paramètres chargés ({Globals.SettingsFile})", severity:100);
                }
                else
                {
                    Logger.Warn("Aucun fichier de paramètres trouvé, utilisation des valeurs par défaut", severity:100);
                    Globals.Settings = ApplyDefaults(Globals.Settings);
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, severity:1000);
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
                Logger.LogException(ex);
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
                Logger.LogException(ex);
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
