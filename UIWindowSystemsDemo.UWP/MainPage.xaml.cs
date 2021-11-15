using System.Diagnostics;
using Evergine.Common.Graphics;
using Evergine.DirectX11;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.UWPView;
using Windows.UI.Xaml.Controls;

namespace UIWindowSystemsDemo.UWP
{
    public sealed partial class MainPage : Page
    {
        private InteractionService interactionService;

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
            
            interactionService = new InteractionService();
            application.Container.RegisterInstance(interactionService);

            var surface = (UWPSurface)windowsSystem.CreateSurface(SwapChainPanel);

            ConfigureGraphicsContext(application, surface);

            // Creates XAudio device
            var xaudio = new Evergine.XAudio2.XAudioDevice();
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

        private static void ConfigureGraphicsContext(Application application, UWPSurface surface)
        {
            GraphicsContext graphicsContext = new DX11GraphicsContext();
            graphicsContext.CreateDevice();

            var swapChain = CreateSwapChain(surface, graphicsContext);

            surface.NativeSurface.SwapChain = swapChain;

            var graphicsPresenter = application.Container.Resolve<GraphicsPresenter>();
            var firstDisplay = new Display(surface, swapChain);

            graphicsPresenter.AddDisplay("DefaultDisplay", firstDisplay);

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

        private void DisplacementChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            interactionService.Displacement = (float)e.NewValue;
        }

        private void ResetCameraClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            interactionService.ResetCamera();
        }
    }
}
