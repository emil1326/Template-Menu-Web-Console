using System;
using System.Linq;
using System.Collections.Generic;
using EmilsWork.EmilsCMS;
using static EmilsWork.EmilsCMS.Helpers;

internal class UserApp
{
    private readonly CMSCore core;

    public UserApp(CMSCore core)
    {
        this.core = core ?? throw new ArgumentNullException(nameof(core));
    }
    // Public wrapper so the host/core can register and call the user's menu
    // =================================================================
    // MENU PRINCIPAL
    // =================================================================
    // Point d'entrée de l'application après le chargement
    // Modifiez MenuNames, Chars et Actions pour personnaliser le menu
    // =================================================================

    public void ShowMainMenu()
    {
        // --- Configuration du menu principal ---
        // MenuNames : texte affiché pour chaque option
        // Chars : touche associée à chaque option (en minuscule)
        // Actions : fonction à exécuter pour chaque option
        // IMPORTANT : le nombre d'éléments doit correspondre entre les trois listes

        new MenuChar(
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
            ['1', '2', '3', '4', 'h', 's', 'i', 'q'],
            [
                () => ListAll(),
                () => CreateOuvrage(),
                () => ModifyItem(),
                () => DeleteItem(),
                () => HiddenMenu(),
                () => core.SettingsMenu(),
                () => core.ShowInfo(),
                () => CMSCore.ExitApp()
            ],
            () => { core.MainMenu(true); }
        ).Run();
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
        new MenuChar(
            [
                "1. Voir tous les items",
                "2. Rechercher par type",
                "3. Rechercher par requête personnalisée",
                "",
                "Q. Retour"
            ],
            ['1', '2', '3', 'q'],
            [
                () => { ShowAllItems(); },
                () => { SearchByType(); },
                () => { SearchByQuery(); },
                () => core.MainMenu()
            ],
            () => { ListAll(); }
        ).Run();
    }

    void SearchByType()
    {
        ClearConsole();
        Console.WriteLine("=== Rechercher par type ===");
        Console.WriteLine();
        Console.WriteLine("Sélectionnez le type d'ouvrage à afficher :");
        Console.WriteLine();

        new MenuChar(
            [
                "1. Livres uniquement",
                "2. Bandes dessinées uniquement",
                "3. Périodiques uniquement",
                "",
                "Q. Retour"
            ],
            ['1', '2', '3', 'q'],
            [
                () => { ShowItemsByType<Livre>(); },
                () => { ShowItemsByType<BandeDessine>(); },
                () => { ShowItemsByType<Periodique>(); },
                () => ListAll()
            ],
            () => { SearchByType(); }
        ).Run();
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

        var items = core.Ouvrages.GetOuvragesByType<T>();

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

        List<Ouvrage> allOuvrages = core.Ouvrages.GetOuvragesByQuery(searchQuery);

        if (allOuvrages.Count == 0)
        {
            Console.WriteLine("Aucun ouvrage enregistré.");
            Console.WriteLine("Appuyez sur Entrée pour revenir...");
            Console.ReadLine();
        }
        else
        {
            var livres = allOuvrages.OfType<Livre>().ToList();
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

        if (!TryAskUsers("Nombre d'exemplaires disponibles : ", out int dispo))
        {
            Console.WriteLine("Entrée invalide, veuillez réessayer.");
            Console.WriteLine("Appuyez sur Entrée pour continuer...");
            Console.ReadLine();
            CreateOuvrage();
            return;
        }

        if (!TryAskUsers("Prix de l'ouvrage (en $) : ", out decimal prix))
        {
            Console.WriteLine("Entrée invalide, veuillez réessayer.");
            Console.WriteLine("Appuyez sur Entrée pour continuer...");
            Console.ReadLine();
            CreateOuvrage();
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Quel est le type d'ouvrage ?");

        new MenuChar(
            [
                "1. Ajouter un livre",
                "2. Ajouter une bande dessinée",
                "3. Enregistrer un périodique"
            ],
            ['1', '2', '3'],
            [
                () => { AddSub(new Livre(){ Titre = titre, Dispo = dispo, Prix = prix }); },
                () => { AddSub(new BandeDessine(){ Titre = titre, Dispo = dispo, Prix = prix }); },
                () => { AddSub(new Periodique(){ Titre = titre, Dispo = dispo, Prix = prix }); }
            ],
            () => { CreateOuvrage(); }
        ).Run();
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
        var addResult = core.Ouvrages.AddOuvrage(BaseOuvrage);
        if (addResult.IsSuccess)
        {
            Console.WriteLine("Ouvrage ajouté avec succès.");
        }
        else
        {
            Console.WriteLine(addResult.Error?.ToUserMessage() ?? "Erreur lors de l'ajout de l'ouvrage.");
        }
        Console.WriteLine("Appuyez sur Entrée pour revenir au menu principal...");
        Console.ReadLine();
        core.MainMenu();
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
            Console.WriteLine("Appuyez sur Entrée pour revenir...");
            Console.ReadLine();
            core.MainMenu();
            return;
        }

        Ouvrage? currOuvrage = core.Ouvrages.GetOuvrageById(ouvrageID);

        if (currOuvrage == null)
        {
            Console.WriteLine("Aucun ouvrage trouvé avec cet ID.");
            Console.WriteLine("Appuyez sur Entrée pour revenir...");
            Console.ReadLine();
            core.MainMenu();
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
                    var updateResult = core.Ouvrages.UpdateOuvrage(currOuvrage);
                    if (!updateResult.IsSuccess)
                    {
                        Console.WriteLine(updateResult.Error?.ToUserMessage() ?? "Erreur lors de la mise à jour de l'ouvrage.");
                        Console.WriteLine("Appuyez sur Entrée pour continuer...");
                        Console.ReadLine();
                    }
                    core.MainMenu();
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
            Console.WriteLine("Appuyez sur Entrée pour revenir...");
            Console.ReadLine();
            core.MainMenu();
            return;
        }

        Ouvrage? currOuvrage = core.Ouvrages.GetOuvrageById(ouvrageID);

        if (currOuvrage == null)
        {
            Console.WriteLine("Aucun ouvrage trouvé avec cet ID.");
            Console.WriteLine("Appuyez sur Entrée pour revenir...");
            Console.ReadLine();
            core.MainMenu();
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
                var removeResult = core.Ouvrages.RemoveOuvrageById(ouvrageID);
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

            Console.WriteLine("Appuyez sur Entrée pour revenir...");
            Console.ReadLine();
            core.MainMenu();
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
        core.MainMenu();
    }
}
