using System;

namespace EmilsWork.EmilsCMS
{
    /// <summary>
    /// Abstract UI component that encapsulates rendering and logic.
    /// Implementations must render themselves and handle input/logic.
    /// </summary>
    public abstract class UIComponent
    {
        /// <summary>Optional title displayed above the component.</summary>
        public string? Title { get; protected set; }

        /// <summary>Render the component to the console (or other surface).</summary>
        public abstract void Render();

        /// <summary>Process input/interaction for the component. Return when done.</summary>
        public abstract void ProcessInput();

        /// <summary>Convenience runner: render then process input.</summary>
        public virtual void Run()
        {
            Render();
            ProcessInput();
        }
    }
}
