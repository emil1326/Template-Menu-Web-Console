namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Lightweight interface for console UI components.
    /// Allows non-hierarchical types (like AppError) to participate in the same rendering
    /// and input loop as other components.
    /// </summary>
    public interface IUIComponent
    {
        /// <summary>Optional title displayed above the component.</summary>
        string? Title { get; }

        /// <summary>Render the component to the console (or other surface).</summary>
        void Render();

        /// <summary>Process input/interaction for the component. Return when done.</summary>
        void ProcessInput();

        /// <summary>Convenience runner: render then process input.</summary>
        void Run() { Render(); ProcessInput(); }
    }
}