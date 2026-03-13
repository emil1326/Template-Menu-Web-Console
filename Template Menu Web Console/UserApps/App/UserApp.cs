using System;
using System.Linq;
using System.Collections.Generic;
using EmilsWork.EmilsCMS;
using static EmilsWork.EmilsCMS.Helpers;

internal class UserApp : App
{
    public UserApp(CMSCore core)
        : base(core)
    {
    }

    public override string DisplayName => "Ouvrage";

    public override IEnumerable<SettingsValues> GetSettingsValues()
    {
        return
        [
            new UserAppSettingsValues()
        ];
    }

    public override IEnumerable<DisplaySettingsPage> GetDisplaySettingsPages()
    {
        return
        [
            new DisplaySettingsPage
            {
                PageKey = UserAppSettingsValues.Key,
                Title = "=== Ouvrage ===",
                Entries =
                [
                    new SettingDisplayEntry
                    {
                        FieldKey = nameof(UserAppSettingsValues.ShowStatistics),
                        Label = "Afficher statistiques de liste",
                        PromptText = "Afficher statistiques de liste?",
                        ToggleBooleanDirectly = true
                    },
                    new SettingDisplayEntry
                    {
                        FieldKey = nameof(UserAppSettingsValues.ShowHiddenMenu),
                        Label = "Afficher menu caché",
                        PromptText = "Afficher menu caché?",
                        ToggleBooleanDirectly = true
                    }
                ]
            }
        ];
    }

    public override IEnumerable<string> GetInfoLines()
    {
        var settings = SettingsComponent.GetOrCreatePage<UserAppSettingsValues>(Globals.GlobalSettings);

        return
        [
            "Module métier Ouvrages: CRUD + recherche.",
            "Navigation en sous-pages: Parcourir et Gestion.",
            $"Config actuelle: stats={settings.ShowStatistics}, hiddenMenu={settings.ShowHiddenMenu}.",
            $"Application: {Globals.AppName} v{Globals.AppVersion}",
            $"Créateur: {Globals.Createur}",
            $"Stockage config: {Globals.SettingsFile}"
        ];
    }
    // Public wrapper so the host/core can register and call the user's menu
    // =================================================================
    // MENU PRINCIPAL
    // =================================================================
    // Point d'entrée de l'application après le chargement
    // Modifiez MenuNames, Chars et Actions pour personnaliser le menu
    // =================================================================

    public void ShowOuvragesMenu(bool showError = false)
    {
        var settings = SettingsComponent.GetOrCreatePage<UserAppSettingsValues>(Globals.GlobalSettings);

        var page = new MenuPage(
            title: "=== OUVRAGES ===",
            description: "Choisissez une section:",
            options:
            [
                new MenuPage.MenuOption('1', "Parcourir / Rechercher", ListAll),
                new MenuPage.MenuOption('2', "Gérer (Ajouter / Modifier / Supprimer)", ShowManageMenu),
                settings.ShowHiddenMenu
                    ? new MenuPage.MenuOption('h', "Menu caché", HiddenMenu)
                    : MenuPage.MenuOption.Space(),
                new MenuPage.MenuOption('s', "Paramètres (scope Ouvrage)", () => ShowScopedSettingsPage(() => ShowOuvragesMenu())),
                new MenuPage.MenuOption('i', "Informations (scope Ouvrage)", () => ShowScopedInfoPage(() => ShowOuvragesMenu())),
                MenuPage.MenuOption.Space(),
                MenuPage.MenuOption.Separator(),
                new MenuPage.MenuOption('q', "Retour menu principal", () => Core.MainMenu())
            ]);

        if (showError)
        {
            page.RunWithError();
            return;
        }

        page.Run();
    }

    private void ShowManageMenu()
    {
        new MenuPage(
            title: "=== GESTION OUVRAGES ===",
            description: "Actions de maintenance:",
            options:
            [
                new MenuPage.MenuOption('1', "Ajouter un ouvrage", CreateOuvrage),
                new MenuPage.MenuOption('2', "Modifier un ouvrage", ModifyItem),
                new MenuPage.MenuOption('3', "Supprimer un ouvrage", DeleteItem),
                MenuPage.MenuOption.Space(),
                new MenuPage.MenuOption('q', "Retour menu ouvrages", () => ShowOuvragesMenu())
            ])
            .Run();
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
        new MenuPage(
            title: "=== PARCOURIR / RECHERCHER ===",
            description: "Explorez votre catalogue:",
            options:
            [
                new MenuPage.MenuOption('1', "Voir tous les items", () => ShowAllItems()),
                new MenuPage.MenuOption('2', "Filtrer par type", SearchByType),
                new MenuPage.MenuOption('3', "Recherche personnalisée", SearchByQuery),
                MenuPage.MenuOption.Space(),
                new MenuPage.MenuOption('q', "Retour menu ouvrages", () => ShowOuvragesMenu())
            ])
            .Run();
    }

    void SearchByType()
    {
        ClearConsole();
        Console.WriteLine("=== Rechercher par type ===");
        Console.WriteLine();
        Console.WriteLine("Sélectionnez le type d'ouvrage à afficher :");
        Console.WriteLine();

        new MenuPage(
            title: "=== RECHERCHE PAR TYPE ===",
            description: "Choisissez une catégorie:",
            options:
            [
                new MenuPage.MenuOption('1', "Livres", () => ShowItemsByType<Livre>()),
                new MenuPage.MenuOption('2', "Bandes dessinées", () => ShowItemsByType<BandeDessine>()),
                new MenuPage.MenuOption('3', "Périodiques", () => ShowItemsByType<Periodique>()),
                MenuPage.MenuOption.Space(),
                new MenuPage.MenuOption('q', "Retour recherche", ListAll)
            ])
            .Run();
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

        var items = Core.Ouvrages.GetOuvragesByType<T>();

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

        if (!WaitForContinue("Appuyez sur Entrée pour revenir..."))
            return;
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
        var settings = SettingsComponent.GetOrCreatePage<UserAppSettingsValues>(Globals.GlobalSettings);

        ClearConsole();
        Console.WriteLine("=== LISTE DES OUVRAGES ===");
        Console.WriteLine();

        List<Ouvrage> allOuvrages = Core.Ouvrages.GetOuvragesByQuery(searchQuery);

        if (allOuvrages.Count == 0)
        {
            Console.WriteLine("Aucun ouvrage enregistré.");
            if (!WaitForContinue("Appuyez sur Entrée pour revenir..."))
                return;
        }
        else
        {
            var livres = allOuvrages.OfType<Livre>().ToList();
            var bds = allOuvrages.OfType<BandeDessine>().ToList();
            var periodiques = allOuvrages.OfType<Periodique>().ToList();

            if (settings.ShowStatistics)
            {
                decimal avgAll = allOuvrages.Count > 0 ? allOuvrages.Average(o => o.Prix) : 0;
                decimal avgLivres = livres.Count > 0 ? livres.Average(o => o.Prix) : 0;
                decimal avgBds = bds.Count > 0 ? bds.Average(o => o.Prix) : 0;
                decimal avgPeriodiques = periodiques.Count > 0 ? periodiques.Average(o => o.Prix) : 0;

                Console.WriteLine($"Prix moyen des ouvrages : {avgAll:C}");
                Console.WriteLine($"Prix moyen des livres : {avgLivres:C}");
                Console.WriteLine($"Prix moyen des bandes dessinées : {avgBds:C}");
                Console.WriteLine($"Prix moyen des périodiques : {avgPeriodiques:C}");
                Console.WriteLine();
            }

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
            if (!WaitForContinue("Appuyez sur Entrée pour revenir..."))
                return;
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

        if (!TryAskUsers("Nombre d'exemplaires disponibles : ", out int dispo))
        {
            Console.WriteLine("Entrée invalide, veuillez réessayer.");
            if (!WaitForContinue("Appuyez sur Entrée pour continuer..."))
                return;
            CreateOuvrage();
            return;
        }

        if (!TryAskUsers("Prix de l'ouvrage (en $) : ", out decimal prix))
        {
            Console.WriteLine("Entrée invalide, veuillez réessayer.");
            if (!WaitForContinue("Appuyez sur Entrée pour continuer..."))
                return;
            CreateOuvrage();
            return;
        }

        new MenuPage(
            title: "=== TYPE D'OUVRAGE ===",
            description: "Quel type voulez-vous créer ?",
            options:
            [
                new MenuPage.MenuOption('1', "Ajouter un livre", () => AddSub(new Livre() { Titre = titre, Dispo = dispo, Prix = prix })),
                new MenuPage.MenuOption('2', "Ajouter une bande dessinée", () => AddSub(new BandeDessine() { Titre = titre, Dispo = dispo, Prix = prix })),
                new MenuPage.MenuOption('3', "Enregistrer un périodique", () => AddSub(new Periodique() { Titre = titre, Dispo = dispo, Prix = prix })),
                MenuPage.MenuOption.Space(),
                new MenuPage.MenuOption('q', "Retour gestion", ShowManageMenu)
            ])
            .Run();
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
                if (!string.IsNullOrWhiteSpace(exBd)) bd.Exemplaires = exBd.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

                TryAskUsers("Année (laisser vide si inconnue) : ", out int? anneeBd);
                bd.Annee = anneeBd;
                break;

            case Livre livre:
                livre.Auteur = AskUsers<string>("Auteur du livre : ");
                var ex = AskUsers<string>("Exemplaires (séparés par des virgules, vide si aucun) : ");
                if (!string.IsNullOrWhiteSpace(ex)) livre.Exemplaires = ex.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

                TryAskUsers("Année (laisser vide si inconnue) : ", out int? anneeLivre);
                livre.Annee = anneeLivre;
                livre.MaisonEdition = AskUsers<string>("Maison d'édition : ");
                break;

            case Periodique periodique:
                periodique.Periodicite = AskUsers<string>("Périodicité du périodique (ex: hebdomadaire, mensuel, trimestriel, journalier) : ");

                TryAskUsers("Date de publication (yyyy-mm-dd, vide si inconnue) : ", out DateTime? datePer);
                periodique.Date = datePer;
                break;
        }

        // Assign Id and store
        var addResult = Core.Ouvrages.AddOuvrage(BaseOuvrage);
        if (addResult.IsSuccess)
        {
            Console.WriteLine("Ouvrage ajouté avec succès.");
        }
        else
        {
            Console.WriteLine(addResult.Error?.ToUserMessage() ?? "Erreur lors de l'ajout de l'ouvrage.");
        }
        if (!WaitForContinue("Appuyez sur Entrée pour revenir au menu principal..."))
            return;
        ShowManageMenu();
    }

    #endregion add items

    #region modify item

    void ModifyItem()
    {
        ClearConsole();
        Console.WriteLine("=== Modifier un ouvrage ===");
        Console.WriteLine("Modifier l'un des ouvrages de la base de données, veuillez préparer votre ID d'ouvrage");
        Console.WriteLine();

        if (!TryAskUsers("ID de l'ouvrage à modifier : ", out int ouvrageID))
        {
            Console.WriteLine("ID invalide.");
            if (!WaitForContinue("Appuyez sur Entrée pour revenir..."))
                return;
            ShowManageMenu();
            return;
        }

        Ouvrage? currOuvrage = Core.Ouvrages.GetOuvrageById(ouvrageID);

        if (currOuvrage == null)
        {
            Console.WriteLine("Aucun ouvrage trouvé avec cet ID.");
            if (!WaitForContinue("Appuyez sur Entrée pour revenir..."))
                return;
            ShowManageMenu();
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
                char input = AskUsers<char>("Sélectionnez le numéro du champ à modifier (ou Q pour revenir) : ", true);
                if (input == 'q')
                {
                    isEditing = false;
                    var updateResult = Core.Ouvrages.UpdateOuvrage(currOuvrage);
                    if (!updateResult.IsSuccess)
                    {
                        Console.WriteLine(updateResult.Error?.ToUserMessage() ?? "Erreur lors de la mise à jour de l'ouvrage.");
                        if (!WaitForContinue("Appuyez sur Entrée pour continuer..."))
                            return;
                    }
                    ShowManageMenu();
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
                            if (TryAskUsers("Nouvelle disponibilité : ", out int dispo))
                                currOuvrage.Dispo = dispo;
                            else
                                Console.WriteLine("Valeur invalide, aucun changement effectué.");
                            break;
                        case '3':
                            if (TryAskUsers("Nouveau prix : ", out decimal prix))
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
                                if (TryAskUsers("Nouvelle année : ", out int? annee))
                                    livre5.Annee = annee;
                                else
                                    Console.WriteLine("Valeur invalide, aucun changement effectué.");
                            }
                            else if (currOuvrage is Periodique p5)
                            {
                                if (TryAskUsers("Nouvelle date (yyyy-mm-dd) : ", out DateTime? date))
                                    p5.Date = date;
                                else
                                    Console.WriteLine("Valeur invalide, aucun changement effectué.");
                            }
                            break;
                        case '6':
                            if (currOuvrage is BandeDessine bd6)
                            {
                                if (TryAskUsers("Nouvelle année : ", out int? anneeBd))
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

        if (!TryAskUsers("ID de l'ouvrage à supprimer : ", out int ouvrageID))
        {
            Console.WriteLine("ID invalide.");
            if (!WaitForContinue("Appuyez sur Entrée pour revenir..."))
                return;
            ShowManageMenu();
            return;
        }

        Ouvrage? currOuvrage = Core.Ouvrages.GetOuvrageById(ouvrageID);

        if (currOuvrage == null)
        {
            Console.WriteLine("Aucun ouvrage trouvé avec cet ID.");
            if (!WaitForContinue("Appuyez sur Entrée pour revenir..."))
                return;
            ShowManageMenu();
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
                var removeResult = Core.Ouvrages.RemoveOuvrageById(ouvrageID);
                Console.WriteLine();
                if (removeResult.IsSuccess)
                {
                    Console.WriteLine("Ouvrage supprimé avec succès.");
                }
                else
                {
                    Console.WriteLine(removeResult.Error?.ToUserMessage() ?? "Erreur lors de la suppression de l'ouvrage.");
                }
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Suppression annulée.");
            }

            if (!WaitForContinue("Appuyez sur Entrée pour revenir..."))
                return;
            ShowManageMenu();
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
        if (!WaitForContinue("Appuyez sur Entrée pour revenir..."))
            return;
        ShowOuvragesMenu();
    }
}

internal sealed class UserAppSettingsValues : SettingsValues
{
    public const string Key = "userapp.ouvrages";
    public override string PageKey => Key;
    public bool ShowStatistics { get; set; } = true;
    public bool ShowHiddenMenu { get; set; } = true;
}
