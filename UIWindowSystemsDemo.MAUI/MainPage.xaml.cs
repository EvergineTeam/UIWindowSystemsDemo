using Evergine.Common.Input.Mouse;
using UIWindowSystemsDemo;

namespace UIWindowSystemsDemo.MAUI
{
    public partial class MainPage : ContentPage
    {
        private readonly MyApplication evergineApplication;
        private InteractionService interactionService;

        public MainPage()
        {
            InitializeComponent();
            this.evergineApplication = new MyApplication();
            this.evergineView1.DisplayName = "DefaultDisplay";
            this.evergineView1.Application = this.evergineApplication;

            RegisterInteractionService();
        }

        private void RegisterInteractionService()
        {
            interactionService = new InteractionService();
            evergineApplication.Container.RegisterInstance(interactionService);
        }

        private void ResetCameraClick(object sender, EventArgs e)
        {
            interactionService.ResetCamera();
        }

        private void DisplacementChanged(object sender, ValueChangedEventArgs e)
        {
            interactionService.Displacement = (float)e.NewValue;
        }
    }
}