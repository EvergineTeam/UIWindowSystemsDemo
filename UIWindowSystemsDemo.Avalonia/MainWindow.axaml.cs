using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace UIWindowSystemsDemo.Avalonia
{
    public partial class MainWindow : Window
    {
        private EvergineControl? renderControl;
        private EvergineControl? renderControl2;
        private InteractionService? interactionService;

        internal bool HasReadyRenderSurface =>
            (this.renderControl?.IsReady ?? false) || (this.renderControl2?.IsReady ?? false);

        public MainWindow()
        {
            InitializeComponent();

            this.renderControl = this.FindControl<EvergineControl>("RenderControl");
            this.renderControl2 = this.FindControl<EvergineControl>("RenderControl2");

            this.RegisterInteractionService();
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            this.renderControl?.Unload();
            this.renderControl2?.Unload();
        }

        private void RegisterInteractionService()
        {
            var app = (App)global::Avalonia.Application.Current!;
            var evergineApplication = app.EvergineApplication;
            if (evergineApplication == null)
            {
                return;
            }

            this.interactionService = new InteractionService();
            evergineApplication.Container.RegisterInstance(this.interactionService);
        }

        private void ResetCameraClick(object? sender, RoutedEventArgs e)
        {
            this.interactionService?.ResetCamera();
        }

        private void DisplacementChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            if (this.interactionService != null)
            {
                this.interactionService.Displacement = (float)e.NewValue;
            }
        }
    }
}
