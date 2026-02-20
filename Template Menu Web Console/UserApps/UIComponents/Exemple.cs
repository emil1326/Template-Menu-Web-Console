
using EmilsWork.EmilsCMS;

public class Exemple : UIComponent
{
    public Exemple() : base()
    {
    }

    public override void ProcessInput()
    {
        throw new NotImplementedException();
    }

    public override void Render()
    {
        Console.WriteLine("Exemple de composant UI");
    }
}