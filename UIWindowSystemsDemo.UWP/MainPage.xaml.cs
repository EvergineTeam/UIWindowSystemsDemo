using System.Diagnostics;
using WaveEngine.Common.Graphics;
using WaveEngine.DirectX11;
using WaveEngine.Framework;
using WaveEngine.Framework.Graphics;
using WaveEngine.Framework.Services;
using WaveEngine.UWPView;
using Windows.UI.Xaml.Controls;

namespace UIWindowSystemsDemo.UWP
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            SwapChainPanel.Loaded += OnSwapChainPanelLoaded;
        }

        private void OnSwapChainPanelLoaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // Create app
            MyApplication application = new MyApplication();

            // Create Services
            UWPWindowsSystem windowsSystem = new UWPWindowsSystem();
            application.Container.RegisterInstance(windowsSystem);
            var surface = (UWPSurface)windowsSystem.CreateSurface(SwapChainPanel);
            var surface2 = (UWPSurface)windowsSystem.CreateSurface(SwapChainPanel2);

            ConfigureGraphicsContext(application, surface, surface2);

            // Creates XAudio device
            var xaudio = new WaveEngine.XAudio2.XAudioDevice();
            application.Container.RegisterInstance(xaudio);

            Stopwatch clockTimer = Stopwatch.StartNew();
            windowsSystem.Run(
            () =>
            {
                application.Initialize();
            },
            () =>
            {
                var gameTime = clockTimer.Elapsed;
                clockTimer.Restart();

                application.UpdateFrame(gameTime);
                application.DrawFrame(gameTime);
            });
        }

        private static void ConfigureGraphicsContext(Application application, UWPSurface surface, UWPSurface surface2)
        {
            GraphicsContext graphicsContext = new DX11GraphicsContext();
            graphicsContext.CreateDevice();

            var swapChain = CreateSwapChain(surface, graphicsContext);
            var swapChain2 = CreateSwapChain(surface2, graphicsContext);

            surface.NativeSurface.SwapChain = swapChain;
            surface2.NativeSurface.SwapChain = swapChain2;

            var graphicsPresenter = application.Container.Resolve<GraphicsPresenter>();
            var firstDisplay = new Display(surface, swapChain);
            var secondDisplay = new Display(surface2, swapChain2);

            graphicsPresenter.AddDisplay("DefaultDisplay", firstDisplay);

            // Take care about camera entity names, if there are more cameras using same name that the DisplayTag Camera, it will take the first camera with the found name
            graphicsPresenter.AddDisplay("Display2", secondDisplay);

            application.Container.RegisterInstance(graphicsContext);
        }

        private static SwapChain CreateSwapChain(UWPSurface surface, GraphicsContext graphicsContext)
        {
            SwapChainDescription swapChainDescription = new SwapChainDescription()
            {
                SurfaceInfo = surface.SurfaceInfo,
                Width = surface.Width,
                Height = surface.Height,
                ColorTargetFormat = PixelFormat.R8G8B8A8_UNorm,
                ColorTargetFlags = TextureFlags.RenderTarget | TextureFlags.ShaderResource,
                DepthStencilTargetFormat = PixelFormat.D24_UNorm_S8_UInt,
                DepthStencilTargetFlags = TextureFlags.DepthStencil,
                SampleCount = TextureSampleCount.None,
                IsWindowed = true,
                RefreshRate = 60
            };

            var swapChain = graphicsContext.CreateSwapChain(swapChainDescription);
            swapChain.VerticalSync = true;

            return swapChain;
        }
    }
}
