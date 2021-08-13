using System;
using System.Windows;
using System.Windows.Input;
using WaveEngine.Common.Graphics;
using WaveEngine.DirectX11;
using WaveEngine.Framework.Graphics;
using WaveEngine.Framework.Services;
using WaveEngine.WPF;
using Window = System.Windows.Window;

namespace UIWindowSystemsDemo.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DX11GraphicsContext dX11GraphicsContext;
        private WPFSurface surface1;
        private Display display1;
        private WPFSurface surface2;
        private Display display2;
        private InteractionService interactionService;

        public MainWindow()
        {
            InitializeComponent();
            LoadWaveEngineControl();
        }

        private void LoadWaveEngineControl()
        {
            var application = ((App)Application.Current).WaveApplication;

            interactionService = new InteractionService();
            application.Container.RegisterInstance(interactionService);

            var graphicsPresenter = application.Container.Resolve<GraphicsPresenter>();
            dX11GraphicsContext = application.Container.Resolve<DX11GraphicsContext>();

            surface1 = new WPFSurface(0, 0) { SurfaceUpdatedAction = s => SurfaceUpdated(s, display1) };
            display1 = new Display(surface1, (FrameBuffer)null);
            surface2 = new WPFSurface(0, 0) { SurfaceUpdatedAction = s => SurfaceUpdated(s, display2) };
            display2 = new Display(surface2, (FrameBuffer)null);

            WaveContainer.Content = surface1.NativeControl;
            WaveContainer2.Content = surface2.NativeControl;
            surface1.NativeControl.MouseDown += NativeControlMouseDown;
            surface2.NativeControl.MouseDown += NativeControlMouseDown;

            surface1.NativeControl.MouseUp += NativeControlMouseUp;
            surface2.NativeControl.MouseUp += NativeControlMouseUp;
            graphicsPresenter.AddDisplay("DefaultDisplay", display1);
            graphicsPresenter.AddDisplay("Display2", display2);
        }

        private void NativeControlMouseUp(object sender, MouseButtonEventArgs e)
        {
            ((FrameworkElement)sender).ReleaseMouseCapture();
        }

        private void NativeControlMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ((FrameworkElement)sender).Focus();
            ((FrameworkElement)sender).CaptureMouse();
        }

        private void ResetCameraClick(object sender, RoutedEventArgs e)
        {
            interactionService.ResetCamera();
        }
        private void DisplacementChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            interactionService.Displacement = (float)e.NewValue;
        }

        private void SurfaceUpdated(IntPtr surfaceHandle, Display display)
        {
            SharpDX.ComObject sharedObject = new SharpDX.ComObject(surfaceHandle);
            SharpDX.DXGI.Resource sharedResource = sharedObject.QueryInterface<SharpDX.DXGI.Resource>();
            SharpDX.Direct3D11.Texture2D nativeRexture = dX11GraphicsContext.DXDevice.OpenSharedResource<SharpDX.Direct3D11.Texture2D>(sharedResource.SharedHandle);

            var texture = DX11Texture.FromDirectXTexture(dX11GraphicsContext, nativeRexture);
            var rTDepthTargetDescription = new TextureDescription()
            {
                Type = TextureType.Texture2D,
                Format = PixelFormat.D24_UNorm_S8_UInt,
                Width = texture.Description.Width,
                Height = texture.Description.Height,
                Depth = 1,
                ArraySize = 1,
                Faces = 1,
                Flags = TextureFlags.DepthStencil,
                CpuAccess = ResourceCpuAccess.None,
                MipLevels = 1,
                Usage = ResourceUsage.Default,
                SampleCount = TextureSampleCount.None,
            };

            var rTDepthTarget = this.dX11GraphicsContext.Factory.CreateTexture(ref rTDepthTargetDescription, "SwapChain_Depth");
            var frameBuffer = this.dX11GraphicsContext.Factory.CreateFrameBuffer(new FrameBufferAttachment(rTDepthTarget, 0, 1), new[] { new FrameBufferAttachment(texture, 0, 1) });
            frameBuffer.SwapchainAssociated = true;
            display.FrameBuffer?.Dispose();
            display.UpdateFrameBuffer(frameBuffer);
        }
    }
}
