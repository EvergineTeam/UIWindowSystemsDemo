using Avalonia.Platform;
using System;

namespace UIWindowSystemsDemo.Avalonia.Platform
{
    /// <summary>
    /// Abstracts all OS-specific operations needed by <see cref="EvergineControl"/>:
    /// native window creation/destruction, surface resize, and raw input event delivery.
    /// </summary>
    internal interface INativePlatformBackend : IDisposable
    {
        /// <summary>Gets the raw native window handle (HWND / XID / NSView pointer).</summary>
        IntPtr NativeHandle { get; }

        /// <summary>
        /// Creates the native child window parented to <paramref name="parent"/> and
        /// starts delivering input events via the supplied callbacks.
        /// </summary>
        IPlatformHandle CreateView(
            IPlatformHandle parent,
            int width,
            int height,
            NativeInputCallbacks callbacks);

        /// <summary>Destroys the native window and releases OS resources.</summary>
        void DestroyView();

        /// <summary>Notifies the backend that the control has been resized.</summary>
        void Resize(int width, int height);

        /// <summary>
        /// Returns the Evergine <c>SurfaceInfo.SurfaceTypes</c> value appropriate for this platform.
        /// </summary>
        Evergine.Common.Graphics.SurfaceInfo.SurfaceTypes SurfaceType { get; }
    }
}