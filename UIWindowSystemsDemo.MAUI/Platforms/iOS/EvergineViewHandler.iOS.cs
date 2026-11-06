using UIWindowSystemsDemo.MAUI.Platforms.iOS;
using Evergine.Common.Graphics;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.iOS;
using Microsoft.Maui.Handlers;
using System.Diagnostics;
using UIKit;

namespace UIWindowSystemsDemo.MAUI.Evergine
{
    public partial class EvergineViewHandler : ViewHandler<EvergineView, UIView>
    {
        private static global::Evergine.Framework.Application sharedApplication;
        private static GraphicsContext sharedGraphicsContext;
        private static bool sharedApplicationInitialized;

        private EvergineAppViewController evergineViewController;
        private IOSWindowsSystem windowsSystem;
        private Surface surface;
        private bool isViewLoaded;
        private bool isEvergineInitialized;

        public static void MapApplication(EvergineViewHandler handler, EvergineView evergineView)
        {
            if (!handler.isViewLoaded)
            {
                return;
            }

            handler.UpdateApplication(evergineView);
        }

        protected override UIView CreatePlatformView()
        {
            this.evergineViewController = new EvergineAppViewController();
            this.windowsSystem = new IOSWindowsSystem(this.evergineViewController);
            this.ViewController = this.evergineViewController;
            return this.ViewController.View;
        }

        protected override void ConnectHandler(UIView platformView)
        {
            base.ConnectHandler(platformView);
            this.isViewLoaded = false;
            this.evergineViewController.OnViewDidLayoutSubviews += this.EvergineViewController_OnViewDidLayoutSubviews;
        }

        protected override void DisconnectHandler(UIView platformView)
        {
            var currentView = this.VirtualView;

            base.DisconnectHandler(platformView);
            this.evergineViewController.OnViewDidLayoutSubviews -= this.EvergineViewController_OnViewDidLayoutSubviews;

            this.isViewLoaded = false;
            this.isEvergineInitialized = false;
            this.LeaveSharedLoop();
            currentView?.NotifyDisconnected();
            this.surface = null;

            if (currentView?.Application != null)
            {
                this.RemoveDisplay(currentView);
            }
        }

        private void EvergineViewController_OnViewDidLayoutSubviews(object sender, EventArgs e)
        {
            this.isViewLoaded = true;
            this.UpdateValue(nameof(EvergineView.Application));
        }

        private void UpdateApplication(EvergineView view)
        {
            if (view.Application is null || this.isEvergineInitialized)
            {
                return;
            }

            var application = view.Application;

            this.RegisterApplicationServices(application);

            this.surface = this.windowsSystem.CreateSurface(0, 0);
            this.JoinSharedLoop(view);

            if (loopOwner != null)
            {
                // Another view runs the shared loop: this view only adds its display.
                this.ConfigureGraphicsContext(application, this.surface, view.DisplayName);
                this.isEvergineInitialized = true;
                if (sharedApplicationInitialized)
                {
                    NotifyReadyHandlers();
                }

                return;
            }

            loopOwner = this;
            application.Container.RegisterInstance(this.windowsSystem as WindowsSystem);

            var clockTimer = Stopwatch.StartNew();
            this.windowsSystem.Run(
            () =>
            {
                if (!ReferenceEquals(this.VirtualView, view))
                {
                    return;
                }

                this.ConfigureGraphicsContext(application, this.surface, view.DisplayName);
                this.isEvergineInitialized = true;

                if (!sharedApplicationInitialized)
                {
                    application.Initialize();
                    sharedApplicationInitialized = true;
                }

                NotifyReadyHandlers();
            },
            () =>
            {
                if (!sharedApplicationInitialized || !ReferenceEquals(loopOwner, this))
                {
                    return;
                }

                var gameTime = clockTimer.Elapsed;
                clockTimer.Restart();

                application.UpdateFrame(gameTime);
                application.DrawFrame(gameTime);
            });

            this.evergineViewController.LoadAction?.Invoke();
        }

        private void RegisterApplicationServices(global::Evergine.Framework.Application application)
        {
            if (!ReferenceEquals(sharedApplication, application))
            {
                sharedApplication = application;
                sharedGraphicsContext = null;
                sharedApplicationInitialized = false;
                ResetSharedLoop();
            }

            this.windowsSystem ??= new IOSWindowsSystem(this.evergineViewController);

            if (loopOwner == null && application.Container.IsRegistered<WindowsSystem>())
            {
                application.Container.Unregister<WindowsSystem>();
            }

            if (sharedGraphicsContext is null)
            {
                var graphicsContext = new global::Evergine.Metal.MTLGraphicsContext();
                application.Container.RegisterInstance(graphicsContext);
                graphicsContext.CreateDevice();
                sharedGraphicsContext = graphicsContext;
            }
        }

        private void ConfigureGraphicsContext(global::Evergine.Framework.Application application, Surface surface, string displayName)
        {
            var graphicsContext = application.Container.Resolve<GraphicsContext>();

            SwapChainDescription swapChainDescription = new SwapChainDescription()
            {
                SurfaceInfo = surface.SurfaceInfo,
                Width = surface.Width,
                Height = surface.Height,
                ColorTargetFormat = PixelFormat.R8G8B8A8_UNorm_SRgb,
                ColorTargetFlags = TextureFlags.RenderTarget | TextureFlags.ShaderResource,
                DepthStencilTargetFormat = PixelFormat.D32_Float,
                DepthStencilTargetFlags = TextureFlags.DepthStencil,
                SampleCount = TextureSampleCount.None,
                IsWindowed = true,
                RefreshRate = 60,
            };

            var swapChain = graphicsContext.CreateSwapChain(swapChainDescription);
            swapChain.VerticalSync = true;
            swapChain.FrameBuffer.IntermediateBufferAssociated = false;

            var graphicsPresenter = application.Container.Resolve<GraphicsPresenter>();
            var display = new Display(surface, swapChain);
            graphicsPresenter.RemoveDisplay(displayName)?.Dispose();
            graphicsPresenter.AddDisplay(displayName, display);
            graphicsPresenter.FocusedDisplay ??= display;
        }
    }
}
