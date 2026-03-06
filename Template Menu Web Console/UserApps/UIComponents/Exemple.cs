
using EmilsWork.EmilsCMS;

public class Exemple : UIComponent
{
    public Exemple() : base()
    {
    }

    public override void ProcessInput()
    {
        // replace standard NotImplementedException with AppError for consistency
        throw new AppError(ErrorCode.Unknown, "ProcessInput not implemented.");
    }

    public override void Render()
    {
        Console.WriteLine("Exemple de composant UI");
    }
}