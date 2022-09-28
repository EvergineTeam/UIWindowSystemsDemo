using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using System;
using System.Linq;
using System.Windows;
using Window = System.Windows.Window;

namespace UIWindowSystemsDemo.WPF
{
    /// <summary>
    /// Interaction logic for SecondaryWindow.xaml
    /// </summary>
    public partial class SecondaryWindow : Window
    {
        private const string Display2 = nameof(Display2);
        private readonly EvergineDisplayHelper displayHelper;

        public SecondaryWindow()
        {
            InitializeComponent();
            displayHelper = new EvergineDisplayHelper(WaveContainer);
            displayHelper.Load(Display2);

            this.RefreshDisplay();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            this.displayHelper.Unload();
        }

        private void RefreshDisplay()
        {
            var application = ((App)Application.Current).EvergineApplication;
            var manager = application.Container.Resolve<ScreenContextManager>();
            var scene = manager.CurrentContext.FindScene<MyScene>();
            var camera = scene.Managers.EntityManager
                .FindComponentsOfType<Camera3D>()
                .FirstOrDefault(camera => camera.DisplayTag == Display2);
            camera.DisplayTagDirty = true;
        }
    }
}
