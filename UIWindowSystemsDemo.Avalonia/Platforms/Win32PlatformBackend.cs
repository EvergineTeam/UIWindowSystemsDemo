using Avalonia.Platform;
using Evergine.Common.Graphics;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace UIWindowSystemsDemo.Avalonia.Platform
{
    /// <summary>
    /// Windows Win32 implementation of <see cref="INativePlatformBackend"/>.
    /// Uses a child HWND and stores the backend instance in GWLP_USERDATA to avoid
    /// dictionary lookups and global locks on the WndProc hot path.
    /// </summary>
    internal sealed class Win32PlatformBackend : INativePlatformBackend
    {
        private const string WindowClassName = "EvergineRenderWindow";
        private static readonly Win32NativeMethods.WndProc WndProcDelegate = CustomWndProc;
        private static readonly object ClassRegistrationSync = new();
        private static bool windowClassRegistered;

        private IntPtr hwnd;
        private NativeInputCallbacks? callbacks;
        private GCHandle selfHandle;
        private int disposed;

        public IntPtr NativeHandle => this.hwnd;

        public SurfaceInfo.SurfaceTypes SurfaceType => SurfaceInfo.SurfaceTypes.Forms;

        public IPlatformHandle CreateView(
            IPlatformHandle parent,
            int width,
            int height,
            NativeInputCallbacks inputCallbacks)
        {
            if (this.hwnd != IntPtr.Zero)
            {
                throw new InvalidOperationException("The Win32 view has already been created.");
            }

            this.callbacks = inputCallbacks;
            EnsureWindowClassRegistered();

            int safeWidth = Math.Max(1, width);
            int safeHeight = Math.Max(1, height);

            this.hwnd = Win32NativeMethods.CreateWindowEx(
                0,
                WindowClassName,
                "Evergine Render",
                Win32NativeMethods.WS_CHILD | Win32NativeMethods.WS_VISIBLE,
                0,
                0,
                safeWidth,
                safeHeight,
                parent.Handle,
                IntPtr.Zero,
                Win32NativeMethods.GetModuleHandle(null),
                IntPtr.Zero);

            if (this.hwnd == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"Failed to create Win32 child window. Error: {Marshal.GetLastWin32Error()}");
            }

            this.selfHandle = GCHandle.Alloc(this);
            IntPtr handlePtr = GCHandle.ToIntPtr(this.selfHandle);

            if (Win32NativeMethods.SetWindowLongPtr(this.hwnd, Win32NativeMethods.GWLP_USERDATA, handlePtr) == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                if (error != 0)
                {
                    Win32NativeMethods.DestroyWindow(this.hwnd);
                    this.hwnd = IntPtr.Zero;
                    this.selfHandle.Free();

                    throw new InvalidOperationException(
                        $"Failed to associate backend with HWND. Error: {error}");
                }
            }

            return new PlatformHandle(this.hwnd, "HWND");
        }

        public void DestroyView()
        {
            if (Interlocked.Exchange(ref this.disposed, 1) != 0)
            {
                return;
            }

            IntPtr hwnd = this.hwnd;
            this.hwnd = IntPtr.Zero;

            if (hwnd != IntPtr.Zero)
            {
                Win32NativeMethods.SetWindowLongPtr(hwnd, Win32NativeMethods.GWLP_USERDATA, IntPtr.Zero);
                Win32NativeMethods.DestroyWindow(hwnd);
            }

            if (this.selfHandle.IsAllocated)
            {
                this.selfHandle.Free();
            }

            this.callbacks = null;
        }

        public void Resize(int width, int height)
        {
            if (this.hwnd == IntPtr.Zero)
            {
                return;
            }

            Win32NativeMethods.SetWindowPos(
                this.hwnd,
                IntPtr.Zero,
                0,
                0,
                Math.Max(1, width),
                Math.Max(1, height),
                Win32NativeMethods.SWP_NOZORDER | Win32NativeMethods.SWP_NOACTIVATE);
        }

        public void Dispose()
        {
            this.DestroyView();
            GC.SuppressFinalize(this);
        }

        private static void EnsureWindowClassRegistered()
        {
            if (windowClassRegistered)
            {
                return;
            }

            lock (ClassRegistrationSync)
            {
                if (windowClassRegistered)
                {
                    return;
                }

                var wndClass = new Win32NativeMethods.WNDCLASSEX
                {
                    cbSize = (uint)Marshal.SizeOf<Win32NativeMethods.WNDCLASSEX>(),
                    style = Win32NativeMethods.CS_OWNDC,
                    lpfnWndProc = Marshal.GetFunctionPointerForDelegate(WndProcDelegate),
                    hInstance = Win32NativeMethods.GetModuleHandle(null),
                    lpszClassName = WindowClassName,
                };

                ushort atom = Win32NativeMethods.RegisterClassEx(ref wndClass);
                if (atom == 0)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException($"Failed to register Win32 class. Error: {error}");
                }

                windowClassRegistered = true;
            }
        }

        private static IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            IntPtr ptr = Win32NativeMethods.GetWindowLongPtr(hWnd, Win32NativeMethods.GWLP_USERDATA);
            if (ptr != IntPtr.Zero)
            {
                GCHandle handle = GCHandle.FromIntPtr(ptr);
                if (handle.Target is Win32PlatformBackend host && host.callbacks is NativeInputCallbacks cb)
                {
                    int x = LowWordSigned(lParam);
                    int y = HighWordSigned(lParam);

                    switch (msg)
                    {
                        case 0x0200: // WM_MOUSEMOVE
                            cb.MouseMove?.Invoke(x, y);
                            break;

                        case 0x0201: // WM_LBUTTONDOWN
                            Win32NativeMethods.SetCapture(hWnd);
                            Win32NativeMethods.SetFocus(hWnd);
                            cb.FocusRequested?.Invoke();
                            cb.MouseDown?.Invoke(0, x, y);
                            return IntPtr.Zero;

                        case 0x0202: // WM_LBUTTONUP
                            Win32NativeMethods.ReleaseCapture();
                            cb.MouseUp?.Invoke(0, x, y);
                            return IntPtr.Zero;

                        case 0x0204: // WM_RBUTTONDOWN
                            Win32NativeMethods.SetCapture(hWnd);
                            Win32NativeMethods.SetFocus(hWnd);
                            cb.FocusRequested?.Invoke();
                            cb.MouseDown?.Invoke(1, x, y);
                            return IntPtr.Zero;

                        case 0x0205: // WM_RBUTTONUP
                            Win32NativeMethods.ReleaseCapture();
                            cb.MouseUp?.Invoke(1, x, y);
                            return IntPtr.Zero;

                        case 0x0207: // WM_MBUTTONDOWN
                            Win32NativeMethods.SetCapture(hWnd);
                            Win32NativeMethods.SetFocus(hWnd);
                            cb.FocusRequested?.Invoke();
                            cb.MouseDown?.Invoke(2, x, y);
                            return IntPtr.Zero;

                        case 0x0208: // WM_MBUTTONUP
                            Win32NativeMethods.ReleaseCapture();
                            cb.MouseUp?.Invoke(2, x, y);
                            return IntPtr.Zero;

                        case 0x020A: // WM_MOUSEWHEEL
                            cb.MouseWheel?.Invoke(HighWordSigned(wParam));
                            return IntPtr.Zero;

                        case 0x0100: // WM_KEYDOWN
                        case 0x0104: // WM_SYSKEYDOWN
                            cb.KeyDown?.Invoke((int)wParam);
                            return IntPtr.Zero;

                        case 0x0101: // WM_KEYUP
                        case 0x0105: // WM_SYSKEYUP
                            cb.KeyUp?.Invoke((int)wParam);
                            return IntPtr.Zero;
                    }
                }
            }

            return Win32NativeMethods.DefWindowProcW(hWnd, msg, wParam, lParam);
        }

        private static int LowWordSigned(IntPtr value) => (short)(value.ToInt64() & 0xFFFF);

        private static int HighWordSigned(IntPtr value) => (short)((value.ToInt64() >> 16) & 0xFFFF);
    }

    internal static class Win32NativeMethods
    {
        public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public const uint CS_OWNDC = 0x0020;
        public const uint WS_CHILD = 0x40000000;
        public const uint WS_VISIBLE = 0x10000000;

        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;

        public const int GWLP_USERDATA = -21;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string? lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateWindowEx(
            uint dwExStyle,
            string lpClassName,
            string lpWindowName,
            uint dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            uint uFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
    }
}