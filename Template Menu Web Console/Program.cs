/// =============================================================================
/// 
/// Ce template fournit une structure de base pour une application console avec :
/// - Système de menu navigable par touches
/// - Chargement/sauvegarde automatique des paramètres (JSON)
/// - Gestion des erreurs avec redémarrage automatique
/// - Structure modulaire pour ajouter facilement de nouvelles fonctionnalités
/// 
/// Dépendance : Newtonsoft.Json (NuGet)
/// https://www.newtonsoft.com/json
/// 
/// =============================================================================

using EmilsWork.EmilsCMS;

internal class Program
{
    private static void Main(string[] _)
    {
        CMSCore app = new();

        var OuvragesApp = new OuvragesApp(app);
        var textEditorApp = new TextEditorApp(app);
        var router = new Router(app, OuvragesApp, textEditorApp);

        // register only root app(s); sub-app hierarchy is managed by each App
        app.RegisterChildApp(router);
        app.RegisterUserMainMenu(() => router.ShowMainMenu());

        app.RunApp();
    }
}
