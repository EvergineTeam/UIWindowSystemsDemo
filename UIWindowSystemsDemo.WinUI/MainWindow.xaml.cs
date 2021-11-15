using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Diagnostics;
using Evergine.Common.Graphics;
using Evergine.DirectX11;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.WinUI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UIWindowSystemsDemo.WinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Microsoft.UI.Xaml.Window
    {
        private InteractionService interactionService;

        public MainWindow()
        {
            this.InitializeComponent();
            SwapChainPanel.Loaded += OnSwapChainPanelLoaded;
        }

        private void OnSwapChainPanelLoaded(object sender, RoutedEventArgs e)
        {
            // Create app
            MyApplication application = new MyApplication();

            interactionService = new InteractionService();
            application.Container.RegisterInstance(interactionService);

            GraphicsContext graphicsContext = new DX11GraphicsContext();
            application.Container.RegisterInstance(graphicsContext);
            graphicsContext.CreateDevice();

            // Create Services
            WinUIWindowsSystem windowsSystem = new WinUIWindowsSystem();
            application.Container.RegisterInstance(windowsSystem);

            var surface = (WinUISurface)windowsSystem.CreateSurface(SwapChainPanel);
            var surface2 = (WinUISurface)windowsSystem.CreateSurface(SwapChainPanel2);

            ConfigureGraphicsContext(application, surface, "DefaultDisplay");
            ConfigureGraphicsContext(application, surface2, "Display2");

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

        private static void ConfigureGraphicsContext(Evergine.Framework.Application application, WinUISurface surface, string displayName)
        {
            GraphicsContext graphicsContext = application.Container.Resolve<GraphicsContext>();
            
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
            surface.NativeSurface.SwapChain = swapChain;

            var graphicsPresenter = application.Container.Resolve<GraphicsPresenter>();
            var firstDisplay = new Display(surface, swapChain);
            graphicsPresenter.AddDisplay(displayName, firstDisplay);
        }

        private void OnSwapChainPanelPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ((SwapChainPanel)sender).Focus(FocusState.Pointer);
            ((SwapChainPanel)sender).CapturePointer(e.Pointer);
        }

        private void OnSwapChainPanelPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ((SwapChainPanel)sender).ReleasePointerCaptures();
        }

        private void ResetCameraClick(object sender, RoutedEventArgs e)
        {
            interactionService.ResetCamera();
        }

        private void DisplacementChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            interactionService.Displacement = (float)e.NewValue;
        }
    }
}
