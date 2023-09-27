using UIWindowSystemsDemo.MAUI.Evergine;
using Evergine.AndroidView;
using Evergine.Common.Graphics;
using Evergine.Framework.Services;
using Evergine.Vulkan;
using Microsoft.Maui.Handlers;
using Evergine.OpenAL;
using Evergine.Framework.Threading;

namespace UIWindowSystemsDemo.MAUI.Evergine
{
    public partial class EvergineViewHandler : ViewHandler<EvergineView, AndroidSurfaceView>
    {
        private AndroidSurface androidSurface;
        private static AndroidWindowsSystem windowsSystem;

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
            if (view.Application is null)
            {
                return;
            }

            // Register Windows system
            var windowSystemCreated = false;
            var registeredWindowSystem = view.Application.Container.Resolve<AndroidWindowsSystem>();
            if (registeredWindowSystem == null)
            {
                view.Application.Container.RegisterInstance(windowsSystem);
                windowSystemCreated = true;
            }

            // Creates XAudio device
            var xaudio = view.Application.Container.Resolve<ALAudioDevice>();
            if (xaudio == null)
            {
                xaudio = new ALAudioDevice();
                view.Application.Container.RegisterInstance(xaudio);
            }

            if (windowSystemCreated)
            {
                System.Diagnostics.Stopwatch clockTimer = System.Diagnostics.Stopwatch.StartNew();
                windowsSystem.Run(
                () =>
                {
                    this.ConfigureGraphicsContext(view.Application as MyApplication, this.androidSurface, displayName);
                    view.Application.Initialize();
                },
                () =>
                {
                    var gameTime = clockTimer.Elapsed;
                    clockTimer.Restart();

                    view.Application.UpdateFrame(gameTime);
                    view.Application.DrawFrame(gameTime);
                });
            }
            else
            {
                EvergineForegroundTask.Run(() =>
                {
                    this.ConfigureGraphicsContext(view.Application as MyApplication, this.androidSurface, displayName);
                });
            }
        }

        protected override AndroidSurfaceView CreatePlatformView()
        {
            if (windowsSystem == null)
            {
                windowsSystem = new AndroidWindowsSystem(this.Context);
            }
            
            this.androidSurface = windowsSystem.CreateSurface(0, 0) as AndroidSurface;
            return this.androidSurface.NativeSurface;
        }

        private void ConfigureGraphicsContext(MyApplication application, Surface surface, string displayName)
        {
            var graphicsContext = application.Container.Resolve<VKGraphicsContext>();
            if (graphicsContext == null)
            {
                graphicsContext = new VKGraphicsContext();
                graphicsContext.CreateDevice();

                application.Container.RegisterInstance(graphicsContext);
            }

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
                RefreshRate = 60,
            };
            var swapChain = graphicsContext.CreateSwapChain(swapChainDescription);
            swapChain.VerticalSync = true;

            var graphicsPresenter = application.Container.Resolve<GraphicsPresenter>();
            var firstDisplay = new global::Evergine.Framework.Graphics.Display(surface, swapChain);
            graphicsPresenter.RemoveDisplay(displayName);
            graphicsPresenter.AddDisplay(displayName, firstDisplay);

            surface.OnScreenSizeChanged += (_, args) =>
            {
                swapChain.ResizeSwapChain(args.Height, args.Width);
            };
        }
    }
}
