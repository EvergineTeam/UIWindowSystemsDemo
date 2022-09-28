using System.Diagnostics;
using System.Windows;

namespace UIWindowSystemsDemo.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public MyApplication EvergineApplication { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Create app
            EvergineApplication = new MyApplication();

            // Create Window System
            var windowsSystem = new Evergine.WPF.WPFWindowsSystem(this);
            EvergineApplication.Container.RegisterInstance(windowsSystem);

            // Create Graphic context
            var graphicsContext = new Evergine.DirectX11.DX11GraphicsContext();
            graphicsContext.CreateDevice();
            EvergineApplication.Container.RegisterInstance(graphicsContext);

            // Creates XAudio device
            var xaudio = new Evergine.XAudio2.XAudioDevice();
            EvergineApplication.Container.RegisterInstance(xaudio);

            Stopwatch clockTimer = Stopwatch.StartNew();
            windowsSystem.Run(
            () =>
            {
                EvergineApplication.Initialize();
            },
            () =>
            {
                var gameTime = clockTimer.Elapsed;
                clockTimer.Restart();

                EvergineApplication.UpdateFrame(gameTime);
                EvergineApplication.DrawFrame(gameTime);

                graphicsContext.DXDeviceContext.Flush();
            });
        }
    }
}
