using Avalonia.Controls;
using Avalonia.Interactivity;

namespace UIWindowSystemsDemo.Avalonia
{
    /// <summary>
    /// The main application window that hosts the Evergine render control.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// The Evergine render control used for rendering within this window.
        /// </summary>
        private EvergineControl? renderControl;

        /// <summary>
        /// Gets the Evergine render control instance associated with this window.
        /// </summary>
        internal EvergineControl? EvergineRenderControl => renderControl;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// Finds and assigns the <see cref="EvergineControl"/> defined in the AXAML layout.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            renderControl = this.FindControl<EvergineControl>("RenderControl");
        }

        /// <summary>
        /// Called when the window is unloaded. Ensures the Evergine render control
        /// is properly unloaded to release resources.
        /// </summary>
        /// <param name="e">The routed event arguments.</param>
        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            renderControl?.Unload();
        }
    }
}