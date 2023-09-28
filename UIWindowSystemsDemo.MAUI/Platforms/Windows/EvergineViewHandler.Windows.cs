using Evergine.Common.Graphics;
using Evergine.DirectX11;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.WinUI;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using WinUIGrid = Microsoft.UI.Xaml.Controls.Grid;

namespace UIWindowSystemsDemo.MAUI.Evergine
{
    public partial class EvergineViewHandler : ViewHandler<EvergineView, WinUIGrid>
    {
        private bool isViewLoaded;

        private SwapChainPanel swapChainPanel;

        private bool isEvergineInitialized;

        public EvergineViewHandler(IPropertyMapper mapper, CommandMapper commandMapper = null)
            : base(mapper, commandMapper)
        {
        }

        public static void MapApplication(EvergineViewHandler handler, EvergineView evergineView)
        {
            if (!handler.isViewLoaded)
            {
                return;
            }

            handler.UpdateApplication(handler.swapChainPanel, evergineView, evergineView.DisplayName);
        }

        protected override WinUIGrid CreatePlatformView()
        {
            var platformView = new WinUIGrid();

            this.swapChainPanel = new SwapChainPanel
            {
                IsHitTestVisible = true,
                IsTabStop = true,
            };

            platformView.Children.Add(this.swapChainPanel);

            return platformView;
        }

        protected override void ConnectHandler(WinUIGrid platformView)
        {
            base.ConnectHandler(platformView);

            this.isViewLoaded = false;

            platformView.Loaded += this.OnPlatformViewLoaded;

            this.swapChainPanel.PointerPressed += this.OnPlatformViewPointerPressed;
            this.swapChainPanel.PointerMoved += this.OnPlatformViewPointerMoved;
            this.swapChainPanel.PointerReleased += this.OnPlatformViewPointerReleased;
        }

        protected override void DisconnectHandler(WinUIGrid platformView)
        {
            base.DisconnectHandler(platformView);

            platformView.Loaded -= this.OnPlatformViewLoaded;

            this.swapChainPanel.PointerPressed -= this.OnPlatformViewPointerPressed;
            this.swapChainPanel.PointerMoved -= this.OnPlatformViewPointerMoved;
            this.swapChainPanel.PointerReleased -= this.OnPlatformViewPointerReleased;

            this.swapChainPanel = null;
        }

        private void OnPlatformViewLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            this.isViewLoaded = true;
            this.UpdateValue(nameof(EvergineView.Application));
        }

        private void OnPlatformViewPointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            ((SwapChainPanel)sender).ReleasePointerCaptures();
            this.VirtualView.StartInteraction();
        }

        private void OnPlatformViewPointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            this.VirtualView.MovedInteraction();
        }

        private void OnPlatformViewPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            ((SwapChainPanel)sender).Focus(FocusState.Pointer);
            ((SwapChainPanel)sender).CapturePointer(e.Pointer);
            this.VirtualView.EndInteraction();
        }

        private void UpdateApplication(SwapChainPanel swapChainPanel, EvergineView view, string displayName)
        {
            if (view.Application is null || this.isEvergineInitialized)
            {
                return;
            }

            var graphicsContext = view.Application.Container.Resolve<GraphicsContext>();
            if (graphicsContext == null)
            {
                graphicsContext = new DX11GraphicsContext();
                view.Application.Container.RegisterInstance(graphicsContext);
                graphicsContext.CreateDevice();
            }

            // Create Services
            var windowSystemCreated = false;
            var windowsSystem = view.Application.Container.Resolve<WinUIWindowsSystem>();
            if (windowsSystem == null)
            {
                windowsSystem = new WinUIWindowsSystem();
                view.Application.Container.RegisterInstance(windowsSystem);
                windowSystemCreated = true;
            }

            var surface = (WinUISurface)windowsSystem.CreateSurface(swapChainPanel);
            this.ConfigureGraphicsContext(view.Application, surface, displayName);

            if (windowSystemCreated)
            {
                var clockTimer = Stopwatch.StartNew();
                windowsSystem.Run(
                    view.Application.Initialize,
                    () =>
                    {
                        var gameTime = clockTimer.Elapsed;
                        clockTimer.Restart();

                        view.Application.UpdateFrame(gameTime);
                        view.Application.DrawFrame(gameTime);
                    });
            }

            this.isEvergineInitialized = true;
        }

        private void ConfigureGraphicsContext(global::Evergine.Framework.Application application, WinUISurface surface, string displayName)
        {
            var graphicsContext = application.Container.Resolve<GraphicsContext>();
            var swapChainDescription = new SwapChainDescription()
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
                RefreshRate = 60,
            };

            var swapChain = graphicsContext.CreateSwapChain(swapChainDescription);
            swapChain.VerticalSync = true;
            surface.NativeSurface.SwapChain = swapChain;

            var graphicsPresenter = application.Container.Resolve<GraphicsPresenter>();
            var firstDisplay = new Display(surface, swapChain);
            graphicsPresenter.AddDisplay(displayName, firstDisplay);
        }
    }
}
