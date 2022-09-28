﻿using Evergine.Common.Graphics;
using Evergine.DirectX11;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.WPF;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UIWindowSystemsDemo.WPF
{
    internal class EvergineDisplayHelper
    {
        private readonly ContentControl control;
        private DX11GraphicsContext dX11GraphicsContext;
        private GraphicsPresenter graphicsPresenter;
        private WPFSurface surface;
        private Display display;
        private string displayTag;

        public EvergineDisplayHelper(ContentControl control)
        {
            this.control = control;
        }

        public void Load(string displayTag)
        {
            var application = ((App)Application.Current).EvergineApplication;
            graphicsPresenter = application.Container.Resolve<GraphicsPresenter>();
            dX11GraphicsContext = application.Container.Resolve<DX11GraphicsContext>();

            surface = new WPFSurface(0, 0) { SurfaceUpdatedAction = s => SurfaceUpdated(s, display) };
            display = new Display(surface, (FrameBuffer)null);

            control.Content = surface.NativeControl;

            surface.NativeControl.MouseDown += NativeControlMouseDown;
            surface.NativeControl.MouseUp += NativeControlMouseUp;

            this.displayTag = displayTag;
            graphicsPresenter.AddDisplay(displayTag, this.display);
        }

        public void Unload()
        {
            graphicsPresenter.RemoveDisplay(this.displayTag);
            displayTag = null;
            surface.NativeControl.MouseDown -= NativeControlMouseDown;
            surface.NativeControl.MouseUp -= NativeControlMouseUp;
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

        private void SurfaceUpdated(IntPtr surfaceHandle, Display display)
        {
            var sharedObject = new SharpGen.Runtime.ComObject(surfaceHandle);
            var sharedResource = sharedObject.QueryInterface<Vortice.DXGI.IDXGIResource>();
            var nativeRexture = dX11GraphicsContext.DXDevice.OpenSharedResource<Vortice.Direct3D11.ID3D11Texture2D>(sharedResource.SharedHandle);

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
            frameBuffer.IntermediateBufferAssociated = true;
            display.FrameBuffer?.Dispose();
            display.UpdateFrameBuffer(frameBuffer);
        }
    }
}
