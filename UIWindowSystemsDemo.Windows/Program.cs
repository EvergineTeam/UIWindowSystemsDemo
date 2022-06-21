using System;
using System.Diagnostics;
using Evergine.Common.Graphics;
using Evergine.Forms;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;

namespace UIWindowSystemsDemo.Windows
{
    class Program
    {
        /// <summary>
        /// Depending on the value of this variable, you can see a different sample behavior:
        /// - True: Evergine renders in full window size.
        /// - False: Evergine renders in defined area within the window, and can be used together
        /// with other native UI elements.
        /// </summary>
        public static bool ShowSingleWindowSample = true;

        [STAThread]
        static void Main(string[] args)
        {
            // Create app
            var application = new MyApplication();

            // Create Services
            uint width = 1280;
            uint height = 720;
            var windowsSystem = new FormsWindowsSystem()
            {
                AutoRegisterWindow = ShowSingleWindowSample,
            };
            application.Container.RegisterInstance(windowsSystem);

            Surface surface;

            if (ShowSingleWindowSample)
            {
                surface = windowsSystem.CreateWindow("Evergine Forms sample", width, height);
            }
            else
            {
                var window = new CustomForm
                {
                    Text = "Evergine Forms sample",
                    Width = (int)width,
                    Height = (int)height,
                };
                window.Initialize(application); 
                
                // When rendered as user control, Evergine surface should be registered manually
                var formsSurface = windowsSystem.CreateSurface(0, 0) as FormsSurface;
                window.SetEvergineControl(formsSurface.NativeControl);
                windowsSystem.RegisterLoopThreadControl(formsSurface.NativeControl);
                window.Show();

                surface = formsSurface;
            }

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

        private static void ConfigureGraphicsContext(Application application, Surface surface)
        {
            GraphicsContext graphicsContext = new Evergine.DirectX11.DX11GraphicsContext();
            graphicsContext.CreateDevice();
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
                RefreshRate = 0
            };
            var swapChain = graphicsContext.CreateSwapChain(swapChainDescription);
            swapChain.VerticalSync = true;

            var graphicsPresenter = application.Container.Resolve<GraphicsPresenter>();
            var firstDisplay = new Display(surface, swapChain);
            graphicsPresenter.AddDisplay("DefaultDisplay", firstDisplay);

            application.Container.RegisterInstance(graphicsContext);
        }
    }
}
