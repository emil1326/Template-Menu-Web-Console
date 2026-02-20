namespace EmilsWork.EmilsCMS
{
    public class MenuChar
    {
        public List<string> MenuNames { get; set; } = new List<string>();
        public List<char> Chars { get; set; } = new List<char>();
        public List<Action> Actions { get; set; } = new List<Action>();
        public Action? OnError { get; set; }

        public MenuChar() { }

        public MenuChar(List<string> menuNames, List<char> chars, List<Action> actions, Action? onError = null)
        {
            MenuNames = menuNames ?? new List<string>();
            Chars = chars ?? new List<char>();
            Actions = actions ?? new List<Action>();
            OnError = onError;
        }

        /// <summary>
        /// Affiche le menu et traite l'entrée utilisateur
        /// </summary>
        /// <param name="menu">Configuration du menu à afficher</param>
        public void ProcessMenuInput()
        {
            // Affiche toutes les lignes du menu
            foreach (string line in MenuNames)
            {
                Console.WriteLine(line);
            }

            // Attend une touche et la convertit en minuscule
            char input = char.ToLower(Console.ReadKey(true).KeyChar);
            Console.WriteLine();

            // Cherche l'action correspondante
            for (int i = 0; i < Chars.Count; i++)
            {
                if (Chars[i] == input)
                {
                    Actions[i]();
                    return;
                }
            }

            if (OnError != null)
            {
                OnError();
                return;
            }
            else
            {
                Console.WriteLine("[WARN] Invalid menu input.");
                return;
            }
        }
    }
}
