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
        private static global::Evergine.Framework.Application sharedApplication;
        private static GraphicsContext sharedGraphicsContext;
        private static WinUIWindowsSystem sharedWindowsSystem;
        private static bool sharedApplicationInitialized;
        private static bool isSharedLoopRunning;

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

            // WinUI needs IsTabStop to raise keyboard events on the panel (WASD camera movement).
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
            var currentView = this.VirtualView;

            base.DisconnectHandler(platformView);

            platformView.Loaded -= this.OnPlatformViewLoaded;

            this.swapChainPanel.PointerPressed -= this.OnPlatformViewPointerPressed;
            this.swapChainPanel.PointerMoved -= this.OnPlatformViewPointerMoved;
            this.swapChainPanel.PointerReleased -= this.OnPlatformViewPointerReleased;

            this.isViewLoaded = false;
            this.isEvergineInitialized = false;
            this.LeaveSharedLoop();
            currentView?.NotifyDisconnected();

            if (currentView?.Application != null)
            {
                this.RemoveDisplay(currentView);
            }

            this.swapChainPanel = null;
        }

        private void OnPlatformViewLoaded(object sender, RoutedEventArgs e)
        {
            this.isViewLoaded = true;
            this.UpdateValue(nameof(EvergineView.Application));
        }

        private void OnPlatformViewPointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            ((SwapChainPanel)sender).ReleasePointerCaptures();
            this.VirtualView.EndInteraction();
        }

        private void OnPlatformViewPointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            this.VirtualView.MovedInteraction();
        }

        private void OnPlatformViewPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            ((SwapChainPanel)sender).Focus(FocusState.Pointer);
            ((SwapChainPanel)sender).CapturePointer(e.Pointer);
            this.VirtualView.StartInteraction();
        }

        private void UpdateApplication(SwapChainPanel swapChainPanel, EvergineView view, string displayName)
        {
            if (view.Application is null || this.isEvergineInitialized)
            {
                return;
            }

            var application = view.Application;

            this.RegisterApplicationServices(application);

            var surface = (WinUISurface)sharedWindowsSystem.CreateSurface(swapChainPanel);
            this.ConfigureGraphicsContext(application, surface, displayName);
            this.isEvergineInitialized = true;
            this.JoinSharedLoop(view);

            if (isSharedLoopRunning)
            {
                // The shared loop is already running: this view only adds its display.
                NotifyReadyHandlers();
                return;
            }

            // On WinUI the loop belongs to the shared windows system, so it keeps running for any view.
            isSharedLoopRunning = true;
            loopOwner = this;
            var clockTimer = Stopwatch.StartNew();
            sharedWindowsSystem.Run(
                () =>
                {
                    if (!sharedApplicationInitialized)
                    {
                        application.Initialize();
                        sharedApplicationInitialized = true;
                    }

                    NotifyReadyHandlers();
                },
                () =>
                {
                    if (!sharedApplicationInitialized)
                    {
                        return;
                    }

                    var gameTime = clockTimer.Elapsed;
                    clockTimer.Restart();

                    application.UpdateFrame(gameTime);
                    application.DrawFrame(gameTime);
                });
        }

        private void RegisterApplicationServices(global::Evergine.Framework.Application application)
        {
            if (!ReferenceEquals(sharedApplication, application))
            {
                sharedWindowsSystem?.Dispose();
                sharedWindowsSystem = null;
                isSharedLoopRunning = false;
                sharedApplication = application;
                sharedGraphicsContext = null;
                sharedApplicationInitialized = false;
                ResetSharedLoop();
            }

            if (sharedWindowsSystem is null)
            {
                if (application.Container.IsRegistered<WinUIWindowsSystem>())
                {
                    application.Container.Unregister<WinUIWindowsSystem>();
                }

                sharedWindowsSystem = new WinUIWindowsSystem();
                application.Container.RegisterInstance(sharedWindowsSystem);
            }

            if (sharedGraphicsContext is null)
            {
                var graphicsContext = new DX11GraphicsContext();
                application.Container.RegisterInstance(graphicsContext);
                graphicsContext.CreateDevice();
                sharedGraphicsContext = graphicsContext;
            }
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
                DepthStencilTargetFormat = PixelFormat.D32_Float_S8X24_UInt,
                DepthStencilTargetFlags = TextureFlags.DepthStencil,
                SampleCount = TextureSampleCount.None,
                IsWindowed = true,
                RefreshRate = 60,
            };

            var swapChain = graphicsContext.CreateSwapChain(swapChainDescription);
            swapChain.VerticalSync = true;
            surface.NativeSurface.SwapChain = swapChain;

            var graphicsPresenter = application.Container.Resolve<GraphicsPresenter>();
            var display = new Display(surface, swapChain);
            graphicsPresenter.RemoveDisplay(displayName)?.Dispose();
            graphicsPresenter.AddDisplay(displayName, display);
            graphicsPresenter.FocusedDisplay ??= display;
        }
    }
}
