using System.Diagnostics;
using System.Windows;

namespace UIWindowSystemsDemo.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public MyApplication WaveApplication { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Create app
            WaveApplication = new MyApplication();

            // Create Window System
            var windowsSystem = new WaveEngine.WPF.WPFWindowsSystem(this);
            WaveApplication.Container.RegisterInstance(windowsSystem);

            // Create Graphic context
            var graphicsContext = new WaveEngine.DirectX11.DX11GraphicsContext();
            graphicsContext.CreateDevice();
            WaveApplication.Container.RegisterInstance(graphicsContext);

            // Creates XAudio device
            var xaudio = new WaveEngine.XAudio2.XAudioDevice();
            WaveApplication.Container.RegisterInstance(xaudio);

            Stopwatch clockTimer = Stopwatch.StartNew();
            windowsSystem.Run(
            () =>
            {
                WaveApplication.Initialize();
            },
            () =>
            {
                var gameTime = clockTimer.Elapsed;
                clockTimer.Restart();

                WaveApplication.UpdateFrame(gameTime);
                WaveApplication.DrawFrame(gameTime);
            });
        }
    }
}
