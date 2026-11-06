using UIWindowSystemsDemo.MAUI.Evergine;
using Evergine.Android;
using Evergine.Common.Graphics;
using Evergine.Common.Helpers;
using Evergine.Framework.Services;
using Evergine.Vulkan;
using UIWindowSystemsDemo;
using Microsoft.Maui.Handlers;
using System.Diagnostics;

namespace UIWindowSystemsDemo.MAUI.Evergine
{
    public partial class EvergineViewHandler : ViewHandler<EvergineView, AndroidSurfaceView>
    {
        private static global::Evergine.Framework.Application sharedApplication;
        private static bool sharedApplicationInitialized;
        private static VKGraphicsContext sharedGraphicsContext;
        private static global::Evergine.OpenAL.ALAudioDevice sharedAudioDevice;

        private AndroidSurface androidSurface;
        private AndroidWindowsSystem windowsSystem;
        private bool surfaceReady;
        private bool surfaceInvalidated;
        private bool isEvergineRunConfigured;
        private bool isEvergineInitialized;
        private string displayName;

        private SwapChain swapChain;

        public EvergineViewHandler(IPropertyMapper mapper, CommandMapper commandMapper = null)
           : base(mapper, commandMapper)
        {
        }

        public static void MapApplication(EvergineViewHandler handler, EvergineView evergineView)
        {
            handler.UpdateApplication(evergineView, evergineView.DisplayName);
        }

        internal void UpdateApplication(EvergineView view, string displayName)
        {
            if (view.Application is not MyApplication application ||
                this.isEvergineRunConfigured)
            {
                return;
            }

            if (!ReferenceEquals(this.VirtualView, view) || this.PlatformView == null)
            {
                return;
            }

            this.RegisterApplicationServices(application);

            this.isEvergineRunConfigured = true;
            this.displayName = displayName;
            this.JoinSharedLoop(view);

            if (loopOwner != null)
            {
                // Another view runs the shared loop; it adds this view's display once its surface exists.
                this.androidSurface.LoadAction = () => this.surfaceReady = true;
                return;
            }

            loopOwner = this;
            application.Container.RegisterInstance(this.windowsSystem);

            var clockTimer = Stopwatch.StartNew();
            this.windowsSystem.Run(
            () =>
            {
                this.surfaceReady = true;
                EnsureEvergineInitialized(application);
            },
            () =>
            {
                // SurfaceCreated can run before WindowsSystem.Run installs its LoadAction.
                // In that case the one-shot load callback is missed, so recover it on the render thread.
                this.surfaceReady = true;
                EnsureEvergineInitialized(application);
                if (!sharedApplicationInitialized || !ReferenceEquals(loopOwner, this))
                {
                    return;
                }

                var gameTime = clockTimer.Elapsed;
                clockTimer.Restart();

                application.UpdateFrame(gameTime);
                application.DrawFrame(gameTime);
            });
        }

        private static void EnsureEvergineInitialized(MyApplication application)
        {
            foreach (var handler in connectedHandlers.ToArray())
            {
                handler.EnsureDisplayConfigured(application);
            }

            if (!sharedApplicationInitialized && connectedHandlers.Exists(h => h.isEvergineInitialized))
            {
                application.Initialize();
                sharedApplicationInitialized = true;
            }

            if (sharedApplicationInitialized)
            {
                NotifyReadyHandlers();
            }
        }

        private void EnsureDisplayConfigured(MyApplication application)
        {
            if (this.isEvergineInitialized || this.surfaceInvalidated || !this.surfaceReady)
            {
                return;
            }

            this.ConfigureGraphicsContext(application, this.androidSurface, this.displayName);
            this.isEvergineInitialized = true;
        }

        private void RegisterApplicationServices(MyApplication application)
        {
            if (!ReferenceEquals(sharedApplication, application))
            {
                sharedApplication = application;
                sharedApplicationInitialized = false;
                sharedGraphicsContext = null;
                sharedAudioDevice = null;
                ResetSharedLoop();
            }

            if (loopOwner == null && application.Container.IsRegistered<AndroidWindowsSystem>())
            {
                application.Container.Unregister<AndroidWindowsSystem>();
            }

            if (!sharedApplicationInitialized)
            {
                if (application.Container.IsRegistered<global::Evergine.OpenAL.ALAudioDevice>())
                {
                    application.Container.Unregister<global::Evergine.OpenAL.ALAudioDevice>();
                }

                sharedAudioDevice ??= new global::Evergine.OpenAL.ALAudioDevice();
                application.Container.RegisterInstance(sharedAudioDevice);
            }
        }

        protected override AndroidSurfaceView CreatePlatformView()
        {
            var context = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity ?? this.Context;
            this.windowsSystem = new AndroidWindowsSystem(context);
            this.androidSurface = this.windowsSystem.CreateSurface(0, 0) as AndroidSurface;
            return this.androidSurface.NativeSurface;
        }

        protected override void ConnectHandler(AndroidSurfaceView platformView)
        {
            base.ConnectHandler(platformView);
            this.surfaceInvalidated = false;
            this.androidSurface.OnSurfaceInfoChanged += this.AndroidSurface_OnSurfaceInfoChanged;
            this.androidSurface.Closing += this.AndroidSurface_OnClosing;
            this.androidSurface.OnScreenSizeChanged += this.AndroidSurface_OnScreenSizeChanged;
        }

        protected override void DisconnectHandler(AndroidSurfaceView platformView)
        {
            var currentView = this.VirtualView;

            base.DisconnectHandler(platformView);
            this.androidSurface.OnScreenSizeChanged -= this.AndroidSurface_OnScreenSizeChanged;
            this.androidSurface.OnSurfaceInfoChanged -= this.AndroidSurface_OnSurfaceInfoChanged;
            this.androidSurface.Closing -= this.AndroidSurface_OnClosing;

            this.surfaceInvalidated = true;
            this.surfaceReady = false;
            this.isEvergineRunConfigured = false;
            this.isEvergineInitialized = false;
            this.LeaveSharedLoop();
            currentView?.NotifyDisconnected();

            if (currentView?.Application != null)
            {
                this.RemoveDisplay(currentView);
            }
        }

        private void AndroidSurface_OnClosing(object sender, EventArgs e)
        {
            this.surfaceInvalidated = true;
        }

        private void AndroidSurface_OnSurfaceInfoChanged(object sender, SurfaceInfo surfaceInfo)
        {
            this.swapChain?.RefreshSurfaceInfo(surfaceInfo);
            this.swapChain?.ResizeSwapChain(this.androidSurface.Width, this.androidSurface.Height);
            this.surfaceInvalidated = false;
            this.surfaceReady = true;
        }

        private void AndroidSurface_OnScreenSizeChanged(object sender, SizeEventArgs e)
        {
            if (!this.surfaceInvalidated)
            {
                this.swapChain?.ResizeSwapChain(e.Width, e.Height);
            }
        }

        private void ConfigureGraphicsContext(MyApplication application, Surface surface, string displayName)
        {
            if (sharedGraphicsContext == null)
            {
                sharedGraphicsContext = new VKGraphicsContext();
                sharedGraphicsContext.CreateDevice();
                application.Container.RegisterInstance(sharedGraphicsContext);
            }

            SwapChainDescription swapChainDescription = new SwapChainDescription()
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
            this.swapChain = sharedGraphicsContext.CreateSwapChain(swapChainDescription);
            this.swapChain.VerticalSync = true;
            this.swapChain.FrameBuffer.IntermediateBufferAssociated = false;

            var graphicsPresenter = application.Container.Resolve<GraphicsPresenter>();
            var display = new global::Evergine.Framework.Graphics.Display(surface, this.swapChain);
            graphicsPresenter.RemoveDisplay(displayName)?.Dispose();
            graphicsPresenter.AddDisplay(displayName, display);
            graphicsPresenter.FocusedDisplay ??= display;
        }
    }
}
