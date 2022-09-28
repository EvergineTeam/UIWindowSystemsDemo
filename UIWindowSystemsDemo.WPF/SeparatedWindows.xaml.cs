using System.Linq;
using System.Windows;
using Window = System.Windows.Window;

namespace UIWindowSystemsDemo.WPF
{
    /// <summary>
    /// Interaction logic for SeparatedWindows.xaml
    /// </summary>
    public partial class SeparatedWindows : Window
    {
        private readonly EvergineDisplayHelper displayHelper;
        private InteractionService interactionService;

        public SeparatedWindows()
        {
            InitializeComponent();
            displayHelper = new EvergineDisplayHelper(WaveContainer);
            displayHelper.Load("DefaultDisplay");

            RegisterInteractionService();
        }

        private void RegisterInteractionService()
        {
            var application = ((App)Application.Current).EvergineApplication;
            interactionService = new InteractionService();
            application.Container.RegisterInstance(interactionService);
        }

        private void ResetCameraClick(object sender, RoutedEventArgs e)
        {
            interactionService.ResetCamera();
        }

        private void DisplacementChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            interactionService.Displacement = (float)e.NewValue;
        }

        private void OpenSecondaryWindow_Click(object sender, RoutedEventArgs e)
        {
            if (!App.Current.Windows.OfType<SecondaryWindow>().Any())
            {
                var window = new SecondaryWindow();
                window.Show();
            }
        }
    }
}
