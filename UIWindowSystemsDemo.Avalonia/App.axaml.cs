using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Evergine.Avalonia;
using Evergine.Common.Graphics;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace UIWindowSystemsDemo.Avalonia
{
    /// <summary>
    /// The main application class for the Avalonia-hosted Evergine application.
    /// Responsible for bootstrapping the Evergine engine, registering platform-specific
    /// graphics and audio devices, and driving the main update/render loop.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Enumerates the supported host operating system platforms.
        /// </summary>
        private enum HostPlatform
        {
            /// <summary>Microsoft Windows.</summary>
            Windows,

            /// <summary>Apple macOS.</summary>
            MacOS,

            /// <summary>Linux.</summary>
            Linux,

            /// <summary>An unrecognized or unsupported platform.</summary>
            Unknown,
        }

        /// <summary>
        /// Gets the Evergine application instance created during framework initialization.
        /// Returns <see langword="null"/> before <see cref="OnFrameworkInitializationCompleted"/> has run.
        /// </summary>
        public MyApplication? EvergineApplication { get; private set; }

        /// <inheritdoc/>
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Called when the Avalonia framework has finished initializing.
        /// Sets up the Evergine application, registers platform-specific devices,
        /// creates the main window, and starts the Evergine update/render loop.
        /// </summary>
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                EvergineApplication = new MyApplication();
                var windowsSystem = new AvaloniaWindowsSystem();
                EvergineApplication.Container.RegisterInstance(windowsSystem);

                // Create platform-specific graphics context
                var graphicsContext = CreateGraphicsContext();
                graphicsContext.CreateDevice();
                EvergineApplication.Container.RegisterInstance(graphicsContext);
                CreateAndRegisterAudioDevice();

                // Create main window, this will create display in its constructor
                // before windowsSystem.Run() calls Initialize()
                desktop.MainWindow = new MainWindow();
                var clockTimer = Stopwatch.StartNew();
                windowsSystem.Run(
                    () =>
                    {
                        EvergineApplication.Initialize();
                    },
                    () =>
                    {
                        var gameTime = clockTimer.Elapsed;
                        clockTimer.Restart();

                        // Guard against rendering before display is ready
                        var mainWindow = (MainWindow)desktop.MainWindow;
                        if (mainWindow.EvergineRenderControl != null && mainWindow.EvergineRenderControl.IsReady)
                        {
                            EvergineApplication.UpdateFrame(gameTime);
                            EvergineApplication.DrawFrame(gameTime);
                        }
                    });
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Creates and returns a platform-appropriate <see cref="GraphicsContext"/> instance.
        /// </summary>
        /// <returns>A <see cref="GraphicsContext"/> suited to the current operating system.</returns>
        /// <exception cref="NotImplementedException">
        /// Thrown when the current platform does not yet have a graphics context implementation.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        /// Thrown when the current platform is entirely unrecognized.
        /// </exception>
        private GraphicsContext CreateGraphicsContext()
        {
            switch (DetectHostPlatform())
            {
                case HostPlatform.Windows:
                    return new Evergine.DirectX11.DX11GraphicsContext();
                case HostPlatform.MacOS:
                    throw new NotImplementedException("macOS graphics context path is not implemented yet for Avalonia hosting.");
                case HostPlatform.Linux:
                    throw new NotImplementedException("Linux graphics context path is not implemented yet for Avalonia hosting.");
                default:
                    throw new PlatformNotSupportedException($"Current platform is not supported. OS: {RuntimeInformation.OSDescription}");
            }
        }

        /// <summary>
        /// Creates a platform-appropriate audio device and registers it with the
        /// <see cref="MyApplication.Container"/> of <see cref="EvergineApplication"/>.
        /// Does nothing if <see cref="EvergineApplication"/> is <see langword="null"/>.
        /// </summary>
        /// <exception cref="NotImplementedException">
        /// Thrown when the current platform does not yet have an audio device implementation.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        /// Thrown when the current platform is entirely unrecognized.
        /// </exception>
        private void CreateAndRegisterAudioDevice()
        {
            if (EvergineApplication == null) return;

            switch (DetectHostPlatform())
            {
                case HostPlatform.Windows:
                    var xaudio = new Evergine.XAudio2.XAudioDevice();
                    EvergineApplication.Container.RegisterInstance(xaudio);
                    break;
                case HostPlatform.MacOS:
                    throw new NotImplementedException("macOS audio device path is not implemented yet for Avalonia hosting.");
                case HostPlatform.Linux:
                    throw new NotImplementedException("Linux audio device path is not implemented yet for Avalonia hosting.");
                default:
                    throw new PlatformNotSupportedException($"Current platform is not supported. OS: {RuntimeInformation.OSDescription}");
            }
        }

        /// <summary>
        /// Detects the current host operating system and maps it to a <see cref="HostPlatform"/> value.
        /// Uses multiple detection strategies to maximise reliability across different runtime environments.
        /// </summary>
        /// <returns>The <see cref="HostPlatform"/> that best represents the current operating system.</returns>
        private static HostPlatform DetectHostPlatform()
        {
            if (OperatingSystem.IsWindows() ||
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                RuntimeInformation.OSDescription.Contains("Windows", StringComparison.OrdinalIgnoreCase))
            {
                return HostPlatform.Windows;
            }

            if (OperatingSystem.IsMacOS() || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return HostPlatform.MacOS;
            }

            if (OperatingSystem.IsLinux() || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return HostPlatform.Linux;
            }

            return HostPlatform.Unknown;
        }
    }
}