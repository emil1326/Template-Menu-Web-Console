namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Abstract base class for UI components; implements <see cref="IUIComponent"/>.
    /// Implementations must render themselves and handle input/logic.
    /// </summary>
    public abstract class UIComponent : IUIComponent
    {
        /// <summary>Optional title displayed above the component.</summary>
        public string? Title { get; protected set; }

        /// <summary>Render the component to the console (or other surface).</summary>
        public abstract void Render();

        /// <summary>Process input/interaction for the component. Return when done.</summary>
        public abstract void ProcessInput();

        /// <inheritdoc/> 
        public virtual void Run()
        {
            Render();
            ProcessInput();
        }
    }
}
