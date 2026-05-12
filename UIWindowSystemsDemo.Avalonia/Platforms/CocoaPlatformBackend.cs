using Avalonia.Platform;
using Evergine.Common.Graphics;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace UIWindowSystemsDemo.Avalonia.Platform
{
    /// <summary>
    /// macOS Cocoa implementation of <see cref="INativePlatformBackend"/>.
    /// Creates a child NSView suitable to host a rendering surface.
    /// </summary>
    internal sealed class CocoaPlatformBackend : INativePlatformBackend
    {
        private const string LibObjC = "/usr/lib/libobjc.A.dylib";

        private const nuint NSViewWidthSizable = 1u << 1;
        private const nuint NSViewHeightSizable = 1u << 4;

        private static readonly IntPtr SelAlloc = sel_registerName("alloc");
        private static readonly IntPtr SelInitWithFrame = sel_registerName("initWithFrame:");
        private static readonly IntPtr SelAddSubview = sel_registerName("addSubview:");
        private static readonly IntPtr SelRemoveFromSuperview = sel_registerName("removeFromSuperview");
        private static readonly IntPtr SelRelease = sel_registerName("release");
        private static readonly IntPtr SelSetFrame = sel_registerName("setFrame:");
        private static readonly IntPtr SelSetAutoresizingMask = sel_registerName("setAutoresizingMask:");
        private static readonly IntPtr SelSetWantsLayer = sel_registerName("setWantsLayer:");
        private static readonly IntPtr SelContentView = sel_registerName("contentView");

        private IntPtr nsView;
        private int disposed;

        public IntPtr NativeHandle => this.nsView;

        public SurfaceInfo.SurfaceTypes SurfaceType => SurfaceInfo.SurfaceTypes.SDL;

        public IPlatformHandle CreateView(
            IPlatformHandle parent,
            int width,
            int height,
            NativeInputCallbacks callbacks)
        {
            _ = callbacks;

            if (this.nsView != IntPtr.Zero)
            {
                throw new InvalidOperationException("The Cocoa view has already been created.");
            }

            IntPtr parentView = parent.Handle;
            if (string.Equals(parent.HandleDescriptor, "NSWindow", StringComparison.OrdinalIgnoreCase))
            {
                parentView = IntPtr_objc_msgSend(parent.Handle, SelContentView);
            }

            if (parentView == IntPtr.Zero)
            {
                throw new InvalidOperationException("Invalid Cocoa parent handle.");
            }

            IntPtr nsViewClass = objc_getClass("NSView");
            if (nsViewClass == IntPtr.Zero)
            {
                throw new InvalidOperationException("NSView class was not found.");
            }

            IntPtr allocated = IntPtr_objc_msgSend(nsViewClass, SelAlloc);
            if (allocated == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to allocate NSView.");
            }

            var frame = new NSRect(0, 0, Math.Max(1, width), Math.Max(1, height));
            this.nsView = IntPtr_objc_msgSend_NSRect(allocated, SelInitWithFrame, frame);

            if (this.nsView == IntPtr.Zero)
            {
                Void_objc_msgSend(allocated, SelRelease);
                throw new InvalidOperationException("Failed to initialize NSView.");
            }

            Void_objc_msgSend_Bool(this.nsView, SelSetWantsLayer, true);
            Void_objc_msgSend_UIntPtr(this.nsView, SelSetAutoresizingMask, NSViewWidthSizable | NSViewHeightSizable);
            Void_objc_msgSend_IntPtr(parentView, SelAddSubview, this.nsView);

            return new PlatformHandle(this.nsView, "NSView");
        }

        public void DestroyView()
        {
            if (Interlocked.Exchange(ref this.disposed, 1) != 0)
            {
                return;
            }

            IntPtr view = this.nsView;
            this.nsView = IntPtr.Zero;

            if (view == IntPtr.Zero)
            {
                return;
            }

            Void_objc_msgSend(view, SelRemoveFromSuperview);
            Void_objc_msgSend(view, SelRelease);
        }

        public void Resize(int width, int height)
        {
            if (this.nsView == IntPtr.Zero)
            {
                return;
            }

            var frame = new NSRect(0, 0, Math.Max(1, width), Math.Max(1, height));
            Void_objc_msgSend_NSRect(this.nsView, SelSetFrame, frame);
        }

        public void Dispose()
        {
            this.DestroyView();
            GC.SuppressFinalize(this);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NSRect
        {
            public double X;
            public double Y;
            public double Width;
            public double Height;

            public NSRect(double x, double y, double width, double height)
            {
                this.X = x;
                this.Y = y;
                this.Width = width;
                this.Height = height;
            }
        }

        [DllImport(LibObjC, EntryPoint = "objc_getClass")]
        private static extern IntPtr objc_getClass(string name);

        [DllImport(LibObjC, EntryPoint = "sel_registerName")]
        private static extern IntPtr sel_registerName(string name);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static extern IntPtr IntPtr_objc_msgSend_NSRect(IntPtr receiver, IntPtr selector, NSRect rect);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static extern void Void_objc_msgSend(IntPtr receiver, IntPtr selector);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static extern void Void_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static extern void Void_objc_msgSend_UIntPtr(IntPtr receiver, IntPtr selector, nuint arg1);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static extern void Void_objc_msgSend_Bool(
            IntPtr receiver,
            IntPtr selector,
            [MarshalAs(UnmanagedType.I1)] bool value);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static extern void Void_objc_msgSend_NSRect(IntPtr receiver, IntPtr selector, NSRect rect);
    }
}