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
        EmilsCMSCore app = new();

        app.Run();
    }
}
