using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
using static EmilsWork.EmilsCMS.CMSClasses;
using static EmilsWork.EmilsCMS.Helpers;
using Formatting = Newtonsoft.Json.Formatting;

namespace EmilsWork.EmilsCMS
{
    internal class EmilsCMSCore
    {
        MenuChar currentMenu = new();           // Menu actuellement affiché
        AppSettings settings = new();              // Paramètres de l'application
        bool needLoad = true;                   // Flag pour charger les données au démarrage
        bool noCrashMod = false;

        // Global data
        RepositoryOuvrages ouvrages = null!; // Liste des ouvrages
        // =================================================================
        // HELPER METHODS
        // =================================================================

        /// <summary>
        /// Efface la console de manière plus robuste
        /// </summary>
        private static void ClearConsole()
        {
            Console.Clear();
            Console.WriteLine("\x1b[3J");
        }

        // =================================================================
        // POINT D'ENTRÉE - Gestion des erreurs fatales
        // =================================================================
        // Le try/catch global permet de redémarrer l'app en cas de crash
        // =================================================================


        public void Run()
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
                if (noCrashMod)
                    Run();
                else
                    MainMenu();
            }

            ExitApp(); // Sécurité : ne devrait jamais être atteint
        }

        // =================================================================
        // MENU PRINCIPAL
        // =================================================================
        // Point d'entrée de l'application après le chargement
        // Modifiez MenuNames, Chars et Actions pour personnaliser le menu
        // =================================================================

        void MainMenu(bool showError = false)
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

            // --- Configuration du menu principal ---
            // MenuNames : texte affiché pour chaque option
            // Chars : touche associée à chaque option (en minuscule)
            // Actions : fonction à exécuter pour chaque option
            // IMPORTANT : le nombre d'éléments doit correspondre entre les trois listes

            currentMenu = new MenuChar
            {
                MenuNames =
                [
                    "=== MENU PRINCIPAL ===",
                    "",
                    "1. Lister tous les ouvrages",
                    "2. Ajouter un ouvrage",
                    "3. Modifier un ouvrage",
                    "4. Détruire un item",
                    "",
                    "",
                    "S. Paramètres",
                    "I. Informations",
                    "",
                    "Q. Quitter"
                ],
                Chars = ['1', '2', '3', '4', 'h', 's', 'i', 'q'],
                Actions =
                [
                    () => ListAll(),
                    () => CreateOuvrage(),
                    () => ModifyItem(),
                    () => DeleteItem(),
                    () => HiddenMenu(),
                    () => SettingsMenu(),
                    () => ShowInfo(),
                    () => ExitApp()
                ]
            };

            // Affiche le menu et attend une entrée utilisateur
            ProcessMenuInput(currentMenu);
        }

        // =================================================================
        // MODULES - Ajoutez votre logique métier ici
        // =================================================================
        // Chaque module est une fonction séparée pour garder le code organisé
        // Terminez toujours par un appel au menu (MainMenu() ou autre)
        // =================================================================

        #region show items

        void ListAll()
        {
            ClearConsole();
            Console.WriteLine("=== Rechercher des ouvrages ===");
            Console.WriteLine();
            Console.WriteLine("Rechercher à travers les ouvrages de la librairie.");
            Console.WriteLine();

            // --- Exemple de sous-menu ---
            currentMenu = new MenuChar
            {
                MenuNames =
                [
                    "1. Voir tous les items",
                    "2. Rechercher par type",
                    "3. Rechercher par requête personnalisée",
                    "",
                    "Q. Retour"
                ],
                Chars = ['1', '2', '3', 'q'],
                Actions =
                [
                    () => { ShowAllItems(); },
                    () => { SearchByType(); },
                    () => { SearchByQuery(); },
                    () => MainMenu()
                ],
                OnError = () => { ListAll(); }
            };

            ProcessMenuInput(currentMenu);
        }

        void SearchByType()
        {
            ClearConsole();
            Console.WriteLine("=== Rechercher par type ===");
            Console.WriteLine();
            Console.WriteLine("Sélectionnez le type d'ouvrage à afficher :");
            Console.WriteLine();

            currentMenu = new MenuChar
            {
                MenuNames =
                [
                    "1. Livres uniquement",
                    "2. Bandes dessinées uniquement",
                    "3. Périodiques uniquement",
                    "",
                    "Q. Retour"
                ],
                Chars = ['1', '2', '3', 'q'],
                Actions =
                [
                    () => { ShowItemsByType<Livre>(); },
                    () => { ShowItemsByType<BandeDessine>(); },
                    () => { ShowItemsByType<Periodique>(); },
                    () => ListAll()
                ],
                OnError = () => { SearchByType(); }
            };

            ProcessMenuInput(currentMenu);
        }

        void ShowItemsByType<T>() where T : Ouvrage
        {
            ClearConsole();
            string typeName = typeof(T).Name switch
            {
                nameof(Livre) => "Livres",
                nameof(BandeDessine) => "Bandes dessinées",
                nameof(Periodique) => "Périodiques",
                _ => "Ouvrages"
            };

            Console.WriteLine($"=== {typeName.ToUpper()} ===");
            Console.WriteLine();

            var items = ouvrages.GetOuvragesByType<T>();

            if (items.Count == 0)
            {
                Console.WriteLine($"Aucun {typeName.ToLower()} enregistré.");
            }
            else
            {
                decimal avgPrice = items.Average(o => o.Prix);
                Console.WriteLine($"Nombre de {typeName.ToLower()} : {items.Count}");
                Console.WriteLine($"Prix moyen : {avgPrice:C}");
                Console.WriteLine();

                foreach (var o in items)
                {
                    Console.WriteLine($"#{o.Id} - {o.Titre} ({o.Dispo} dispo) - {o.Prix:C}");
                    switch (o)
                    {
                        case BandeDessine bd:
                            Console.WriteLine($"  Auteur : {bd.Auteur} | Dessinateur : {bd.Dessinateur}");
                            if (bd.Exemplaires?.Count > 0) Console.WriteLine($"  Exemplaires: {string.Join(", ", bd.Exemplaires)}");
                            if (bd.Annee.HasValue) Console.WriteLine($"  Année : {bd.Annee}");
                            break;
                        case Livre livre:
                            Console.WriteLine($"  Auteur : {livre.Auteur}");
                            if (livre.Exemplaires?.Count > 0) Console.WriteLine($"  Exemplaires: {string.Join(", ", livre.Exemplaires)}");
                            if (livre.Annee.HasValue) Console.WriteLine($"  Année : {livre.Annee}");
                            break;
                        case Periodique p:
                            Console.WriteLine($"  Périodicité : {p.Periodicite} | Date : {p.Date?.ToShortDateString()}");
                            break;
                    }
                    Console.WriteLine();
                }
            }

            Console.WriteLine("Appuyez sur Entrée pour revenir...");
            Console.ReadLine();
            SearchByType();
        }

        void SearchByQuery()
        {
            ClearConsole();
            Console.WriteLine("=== Recherche personnalisée ===");
            Console.WriteLine();
            Console.WriteLine("Entrez votre recherche (vide = tout afficher)");
            Console.WriteLine();
            Console.WriteLine("--- COMMANDES DISPONIBLES ---");
            Console.WriteLine("  Généraux:    id:1  titre:harry  dispo:5  prix:19.99");
            Console.WriteLine("  Livres/BD:   auteur:rowling  annee:2024  edition:gallimard  exemplaire:poche");
            Console.WriteLine("  BD seules:   dessinateur:hergé");
            Console.WriteLine("  Périodiques: date:2024-01-15  periodicite:mensuel");
            Console.WriteLine();
            Console.WriteLine("--- SYNTAXE ---");
            Console.WriteLine("  Simple:      harry                    (cherche partout)");
            Console.WriteLine("  Filtre:      titre:harry              (cherche dans le titre)");
            Console.WriteLine("  Multiple:    titre:harry auteur:rowling annee:2001");
            Console.WriteLine("  Wildcard:    titre:%pr%  auteur:row%  (% = tout, _ = 1 char)");
            Console.WriteLine();
            Console.WriteLine("--- EXEMPLES ---");
            Console.WriteLine("  auteur:tolkien annee:1954");
            Console.WriteLine("  dessinateur:hergé titre:tintin");
            Console.WriteLine("  prix:%19%  (tous les prix contenant '19')");
            Console.WriteLine();

            string query = AskUsers<string>("Recherche : ");

            ShowAllItems(query);
        }

        void ShowAllItems(string searchQuery = "")
        {
            ClearConsole();
            Console.WriteLine("=== LISTE DES OUVRAGES ===");
            Console.WriteLine();

            List<Ouvrage> allOuvrages = ouvrages.GetOuvragesByQuery(searchQuery);

            if (allOuvrages.Count == 0)
            {
                Console.WriteLine("Aucun ouvrage enregistré.");
                Console.WriteLine("Appuyez sur Entrée pour revenir...");
                Console.ReadLine();
            }
            else
            {
                var livres = allOuvrages.OfType<Livre>().Where(l => l is not BandeDessine).ToList();
                var bds = allOuvrages.OfType<BandeDessine>().ToList();
                var periodiques = allOuvrages.OfType<Periodique>().ToList();

                decimal avgAll = allOuvrages.Count > 0 ? allOuvrages.Average(o => o.Prix) : 0;
                decimal avgLivres = livres.Count > 0 ? livres.Average(o => o.Prix) : 0;
                decimal avgBds = bds.Count > 0 ? bds.Average(o => o.Prix) : 0;
                decimal avgPeriodiques = periodiques.Count > 0 ? periodiques.Average(o => o.Prix) : 0;

                Console.WriteLine($"Prix moyen des ouvrages : {avgAll:C}");
                Console.WriteLine($"Prix moyen des livres : {avgLivres:C}");
                Console.WriteLine($"Prix moyen des bandes dessinées : {avgBds:C}");
                Console.WriteLine($"Prix moyen des périodiques : {avgPeriodiques:C}");
                Console.WriteLine();

                foreach (var o in allOuvrages)
                {
                    Console.WriteLine($"#{o.Id} - {o.Titre} ({o.Dispo} dispo) - {o.Prix:C}");
                    switch (o)
                    {
                        case BandeDessine bd:
                            Console.WriteLine($"  Type : Bande Dessinée | Auteur : {bd.Auteur} | Dessinateur : {bd.Dessinateur}");
                            if (bd.Exemplaires?.Count > 0) Console.WriteLine($"  Exemplaires: {string.Join(", ", bd.Exemplaires)}");
                            if (bd.Annee.HasValue) Console.WriteLine($"  Année : {bd.Annee}");
                            break;
                        case Livre livre:
                            Console.WriteLine($"  Type : Livre | Auteur : {livre.Auteur}");
                            if (livre.Exemplaires?.Count > 0) Console.WriteLine($"  Exemplaires: {string.Join(", ", livre.Exemplaires)}");
                            if (livre.Annee.HasValue) Console.WriteLine($"  Année : {livre.Annee}");
                            break;
                        case Periodique p:
                            Console.WriteLine($"  Type : Périodique | Périodicité : {p.Periodicite} | Date : {p.Date?.ToShortDateString()}");
                            break;
                        default:
                            Console.WriteLine("  Type : Inconnu");
                            break;
                    }
                    Console.WriteLine();
                }
                Console.WriteLine("Appuyez sur Entrée pour revenir...");
                Console.ReadLine();
            }

            ListAll();
        }

        #endregion show items

        #region add items

        void CreateOuvrage()
        {
            ClearConsole();
            Console.WriteLine("=== Ajouter un ouvrage ===");
            Console.WriteLine("Enregistrer dans notre base de données votre livre maintenant!");
            Console.WriteLine();

            string titre = AskUsers<string>("Titre de l'ouvrage : ");

            if (!TryAskUsers<int>("Nombre d'exemplaires disponibles : ", out int dispo))
            {
                Console.WriteLine("Entrée invalide, veuillez réessayer.");
                Console.WriteLine("Appuyez sur Entrée pour continuer...");
                Console.ReadLine();
                CreateOuvrage();
                return;
            }

            if (!TryAskUsers<decimal>("Prix de l'ouvrage (en $) : ", out decimal prix))
            {
                Console.WriteLine("Entrée invalide, veuillez réessayer.");
                Console.WriteLine("Appuyez sur Entrée pour continuer...");
                Console.ReadLine();
                CreateOuvrage();
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Quel est le type d'ouvrage ?");

            currentMenu = new MenuChar
            {
                MenuNames =
                [
                    "1. Ajouter un livre",
                    "2. Ajouter une bande dessinée",
                    "3. Enregistrer un périodique",
                ],
                Chars = ['1', '2', '3'],
                Actions =
                [
                    () => { AddSub(new Livre(){Titre = titre, Dispo = dispo, Prix = prix}); },
                    () => { AddSub(new BandeDessine(){Titre = titre, Dispo = dispo, Prix = prix}); },
                    () => { AddSub(new Periodique(){Titre = titre, Dispo = dispo, Prix = prix}); },
                ],
                OnError = () => { CreateOuvrage(); }
            };

            ProcessMenuInput(currentMenu);
        }

        void AddSub(Ouvrage BaseOuvrage)
        {
            // Collect specific fields based on concrete type
            switch (BaseOuvrage)
            {
                case BandeDessine bd:
                    bd.Auteur = AskUsers<string>("Auteur de la BD : ");
                    bd.Dessinateur = AskUsers<string>("Dessinateur de la BD : ");
                    var exBd = AskUsers<string>("Exemplaires (séparés par des virgules, vide si aucun) : ");
                    if (!string.IsNullOrWhiteSpace(exBd)) bd.Exemplaires = [.. exBd.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];

                    TryAskUsers<int?>("Année (laisser vide si inconnue) : ", out int? anneeBd);
                    bd.Annee = anneeBd;
                    break;

                case Livre livre:
                    livre.Auteur = AskUsers<string>("Auteur du livre : ");
                    var ex = AskUsers<string>("Exemplaires (séparés par des virgules, vide si aucun) : ");
                    if (!string.IsNullOrWhiteSpace(ex)) livre.Exemplaires = [.. ex.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];

                    TryAskUsers<int?>("Année (laisser vide si inconnue) : ", out int? anneeLivre);
                    livre.Annee = anneeLivre;
                    livre.MaisonEdition = AskUsers<string>("Maison d'édition : ");
                    break;

                case Periodique periodique:
                    periodique.Periodicite = AskUsers<string>("Périodicité du périodique (ex: hebdomadaire, mensuel, trimestriel, journalier) : ");

                    TryAskUsers<DateTime?>("Date de publication (yyyy-mm-dd, vide si inconnue) : ", out DateTime? datePer);
                    periodique.Date = datePer;
                    break;
            }

            // Assign Id and store
            ouvrages.AddOuvrage(BaseOuvrage);

            Console.WriteLine("Ouvrage ajouté avec succès.");
            Console.WriteLine("Appuyez sur Entrée pour revenir au menu principal...");
            Console.ReadLine();
            MainMenu();
        }

        #endregion add items

        #region modify item

        void ModifyItem()
        {
            ClearConsole();
            Console.WriteLine("=== Modifier un ouvrage ===");
            Console.WriteLine("Modifier l'un des ouvrages de la base de données, veuillez préparer votre ID d'ouvrage");
            Console.WriteLine();

            if (!TryAskUsers<int>("ID de l'ouvrage à modifier : ", out int ouvrageID))
            {
                Console.WriteLine("ID invalide.");
                Console.WriteLine("Appuyez sur Entrée pour revenir...");
                Console.ReadLine();
                MainMenu();
                return;
            }

            Ouvrage? currOuvrage = ouvrages.GetOuvrageById(ouvrageID);

            if (currOuvrage == null)
            {
                Console.WriteLine("Aucun ouvrage trouvé avec cet ID.");
                Console.WriteLine("Appuyez sur Entrée pour revenir...");
                Console.ReadLine();
                MainMenu();
            }
            else
            {
                bool isEditing = true;

                while (isEditing)
                {
                    ClearConsole();
                    Console.WriteLine("=== MODIFIER L'OUVRAGE ===");
                    Console.WriteLine();
                    Console.WriteLine($"ID : {currOuvrage.Id}");
                    Console.WriteLine($"1. Titre : {currOuvrage.Titre}");
                    Console.WriteLine($"2. Disponibilité : {currOuvrage.Dispo}");
                    Console.WriteLine($"3. Prix : {currOuvrage.Prix:C}");
                    switch (currOuvrage)
                    {
                        case BandeDessine bd:
                            Console.WriteLine($"4. Auteur : {bd.Auteur}");
                            Console.WriteLine($"5. Dessinateur : {bd.Dessinateur}");
                            Console.WriteLine($"6. Année : {bd.Annee}");
                            break;
                        case Livre livre:
                            Console.WriteLine($"4. Auteur : {livre.Auteur}");
                            Console.WriteLine($"5. Année : {livre.Annee}");
                            Console.WriteLine($"6. Maison d'édition : {livre.MaisonEdition}");
                            break;
                        case Periodique p:
                            Console.WriteLine($"4. Périodicité : {p.Periodicite}");
                            Console.WriteLine($"5. Date : {p.Date?.ToShortDateString()}");
                            break;
                    }
                    Console.WriteLine();
                    char input = AskUsers<char>("Sélectionnez le numéro du champ à modifier (ou Q pour quitter) : ", true);
                    if (input == 'q')
                    {
                        isEditing = false;
                        ouvrages.UpdateOuvrage(currOuvrage);
                        MainMenu();
                    }
                    else
                    {
                        Console.WriteLine();
                        switch (input)
                        {
                            case '1':
                                currOuvrage.Titre = AskUsers<string>("Nouveau titre : ");
                                break;
                            case '2':
                                if (TryAskUsers<int>("Nouvelle disponibilité : ", out int dispo))
                                    currOuvrage.Dispo = dispo;
                                else
                                    Console.WriteLine("Valeur invalide, aucun changement effectué.");
                                break;
                            case '3':
                                if (TryAskUsers<decimal>("Nouveau prix : ", out decimal prix))
                                    currOuvrage.Prix = prix;
                                else
                                    Console.WriteLine("Valeur invalide, aucun changement effectué.");
                                break;
                            case '4':
                                if (currOuvrage is BandeDessine bd4)
                                    bd4.Auteur = AskUsers<string>("Nouvel auteur : ");
                                else if (currOuvrage is Livre livre4)
                                    livre4.Auteur = AskUsers<string>("Nouvel auteur : ");
                                else if (currOuvrage is Periodique p4)
                                    p4.Periodicite = AskUsers<string>("Nouvelle périodicité : ");
                                break;
                            case '5':
                                if (currOuvrage is BandeDessine bd5)
                                    bd5.Dessinateur = AskUsers<string>("Nouveau dessinateur : ");
                                else if (currOuvrage is Livre livre5)
                                {
                                    if (TryAskUsers<int?>("Nouvelle année : ", out int? annee))
                                        livre5.Annee = annee;
                                    else
                                        Console.WriteLine("Valeur invalide, aucun changement effectué.");
                                }
                                else if (currOuvrage is Periodique p5)
                                {
                                    if (TryAskUsers<DateTime?>("Nouvelle date (yyyy-mm-dd) : ", out DateTime? date))
                                        p5.Date = date;
                                    else
                                        Console.WriteLine("Valeur invalide, aucun changement effectué.");
                                }
                                break;
                            case '6':
                                if (currOuvrage is BandeDessine bd6)
                                {
                                    if (TryAskUsers<int?>("Nouvelle année : ", out int? anneeBd))
                                        bd6.Annee = anneeBd;
                                    else
                                        Console.WriteLine("Valeur invalide, aucun changement effectué.");
                                }
                                else if (currOuvrage is Livre livre6)
                                    livre6.MaisonEdition = AskUsers<string>("Nouvelle maison d'édition : ");
                                else
                                    Console.WriteLine("Champ invalide pour ce type d'ouvrage.");
                                break;
                            default:
                                Console.WriteLine("Champ invalide.");
                                break;
                        }
                    }
                }
            }
        }

        #endregion modify item

        #region delete item

        void DeleteItem()
        {
            ClearConsole();
            Console.WriteLine("=== Supprimer un ouvrage ===");
            Console.WriteLine();

            if (!TryAskUsers<int>("ID de l'ouvrage à supprimer : ", out int ouvrageID))
            {
                Console.WriteLine("ID invalide.");
                Console.WriteLine("Appuyez sur Entrée pour revenir...");
                Console.ReadLine();
                MainMenu();
                return;
            }

            Ouvrage? currOuvrage = ouvrages.GetOuvrageById(ouvrageID);

            if (currOuvrage == null)
            {
                Console.WriteLine("Aucun ouvrage trouvé avec cet ID.");
                Console.WriteLine("Appuyez sur Entrée pour revenir...");
                Console.ReadLine();
                MainMenu();
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine($"Ouvrage à supprimer : {currOuvrage.Titre} (ID: {currOuvrage.Id})");
                Console.WriteLine();
                Console.Write("Êtes-vous sûr de vouloir supprimer cet ouvrage ? (O/N) : ");

                char confirmation = char.ToLower(Console.ReadKey().KeyChar);
                Console.WriteLine();

                if (confirmation == 'o')
                {
                    ouvrages.RemoveOuvrageById(ouvrageID);
                    Console.WriteLine();
                    Console.WriteLine("Ouvrage supprimé avec succès.");
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("Suppression annulée.");
                }

                Console.WriteLine("Appuyez sur Entrée pour revenir...");
                Console.ReadLine();
                MainMenu();
            }
        }

        #endregion

        void HiddenMenu()
        {
            ClearConsole();
            Console.WriteLine("=== MODULE caché :p ===");
            Console.WriteLine();
            Console.WriteLine("Bonjour!");
            Console.WriteLine();
            Console.WriteLine("Appuyez sur Entrée pour revenir...");
            Console.ReadLine();
            MainMenu();
        }

        void ShowInfo()
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

        void SettingsMenu(bool showError = false)
        {
            ClearConsole();

            if (showError)
            {
                Console.WriteLine("[!] Commande non reconnue");
                Console.WriteLine();
            }

            Console.WriteLine("=== PARAMÈTRES ===");
            Console.WriteLine();

            string mongoStatus = settings.MongoDbPassword != null ? "Configuré ✓" : "Non configuré ✗";
            Console.WriteLine($"MongoDB : {mongoStatus}");
            Console.WriteLine();

            currentMenu = new MenuChar
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

        void ConfigureMongoPassword()
        {
            ClearConsole();
            Console.WriteLine("=== CONFIGURATION MONGODB ===");
            Console.WriteLine();

            string password = AskUsers<string>("Mot de passe MongoDB : ");

            if (!string.IsNullOrWhiteSpace(password))
            {
                settings.MongoDbPassword = password;
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
                        settings = ApplyDefaults(loaded);
                        Console.WriteLine($"[OK] Paramètres chargés ({Globals.SettingsFile})");
                    }
                }
                else
                {
                    Console.WriteLine("[INFO] Aucun fichier de paramètres trouvé, utilisation des valeurs par défaut");
                    settings = ApplyDefaults(settings);
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
                settings = ApplyDefaults(settings);
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
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
            current.JsonFilePath ??= "ouvrages.json";
            return current;
        }

        /// <summary>
        /// Initialise le repository via IService<Ouvrage> (MongoDB par défaut)
        /// </summary>
        void InitializeMongoDBRepository()
        {
            Console.WriteLine("[INFO] Chargement depuis MongoDB...");

            if (string.IsNullOrWhiteSpace(settings.MongoDbPassword))
            {
                Console.WriteLine("[WARN] MongoDB non configuré");
                return;
            }

            MongoDBServiceSettings settingsService = new()
            {
                AppName = "AppName",
                Password = settings.MongoDbPassword ?? "Empty",
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

        static void ExitApp()
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
        void ProcessMenuInput(MenuChar menu)
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
                // Si aucune touche valide, retour au menu principal avec erreur
                MainMenu(true);
        }
    }
}

