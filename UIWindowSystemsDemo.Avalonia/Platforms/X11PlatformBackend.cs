using Avalonia.Platform;
using Avalonia.Threading;
using Evergine.Common.Graphics;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace UIWindowSystemsDemo.Avalonia.Platform
{
    /// <summary>
    /// Linux X11 implementation of <see cref="INativePlatformBackend"/>.
    /// Uses a dedicated blocking X11 event thread, avoids busy polling, and marshals
    /// callbacks to Avalonia's UI thread.
    /// </summary>
    internal sealed class X11PlatformBackend : INativePlatformBackend
    {
        private IntPtr display;
        private IntPtr window;
        private NativeInputCallbacks? callbacks;
        private Thread? eventThread;
        private readonly object syncRoot = new();
        private volatile bool running;
        private volatile bool destroyRequested;
        private int disposed;

        public IntPtr NativeHandle => this.window;

        public SurfaceInfo.SurfaceTypes SurfaceType => SurfaceInfo.SurfaceTypes.SDL;

        public IPlatformHandle CreateView(
            IPlatformHandle parent,
            int width,
            int height,
            NativeInputCallbacks inputCallbacks)
        {
            if (this.window != IntPtr.Zero)
            {
                throw new InvalidOperationException("The X11 view has already been created.");
            }

            this.callbacks = inputCallbacks;

            this.display = X11.XOpenDisplay(null);
            if (this.display == IntPtr.Zero)
            {
                throw new InvalidOperationException("XOpenDisplay failed. Ensure DISPLAY is set.");
            }

            int screen = X11.XDefaultScreen(this.display);
            IntPtr rootWindow = X11.XDefaultRootWindow(this.display);
            IntPtr parentWindow = parent.Handle != IntPtr.Zero ? parent.Handle : rootWindow;

            this.window = X11.XCreateSimpleWindow(
                this.display,
                parentWindow,
                0,
                0,
                (uint)Math.Max(1, width),
                (uint)Math.Max(1, height),
                0,
                X11.XBlackPixel(this.display, screen),
                X11.XBlackPixel(this.display, screen));

            if (this.window == IntPtr.Zero)
            {
                X11.XCloseDisplay(this.display);
                this.display = IntPtr.Zero;
                throw new InvalidOperationException("XCreateSimpleWindow failed.");
            }

            X11.XSelectInput(
                this.display,
                this.window,
                X11.EventMask.ExposureMask |
                X11.EventMask.ButtonPressMask |
                X11.EventMask.ButtonReleaseMask |
                X11.EventMask.PointerMotionMask |
                X11.EventMask.KeyPressMask |
                X11.EventMask.KeyReleaseMask |
                X11.EventMask.StructureNotifyMask);

            X11.XMapWindow(this.display, this.window);
            X11.XFlush(this.display);

            this.running = true;
            this.destroyRequested = false;

            this.eventThread = new Thread(this.EventLoop)
            {
                IsBackground = true,
                Name = "X11EventLoop",
            };
            this.eventThread.Start();

            return new PlatformHandle(this.window, "XID");
        }

        public void DestroyView()
        {
            if (Interlocked.Exchange(ref this.disposed, 1) != 0)
            {
                return;
            }

            Thread? threadToJoin;
            IntPtr displayToClose;
            IntPtr windowToDestroy;

            lock (this.syncRoot)
            {
                this.running = false;
                this.destroyRequested = true;

                threadToJoin = this.eventThread;
                displayToClose = this.display;
                windowToDestroy = this.window;

                // Invalidate managed fields early to reduce races from other callers.
                this.eventThread = null;
                this.window = IntPtr.Zero;
                this.display = IntPtr.Zero;
                this.callbacks = null;
            }

            // Destroying the window helps unblock XNextEvent on the native 
            if (displayToClose != IntPtr.Zero && windowToDestroy != IntPtr.Zero)
            {
                try
                {
                    X11.XDestroyWindow(displayToClose, windowToDestroy);
                    X11.XFlush(displayToClose);
                }
                catch
                {
                    // Avoid throwing during teardown.
                }
            }

            if (threadToJoin != null && threadToJoin.IsAlive && threadToJoin != Thread.CurrentThread)
            {
                threadToJoin.Join(TimeSpan.FromSeconds(1));
            }

            if (displayToClose != IntPtr.Zero)
            {
                try
                {
                    X11.XCloseDisplay(displayToClose);
                }
                catch
                {
                    // Avoid throwing during teardown.
                }
            }
        }

        public void Resize(int width, int height)
        {
            IntPtr currentDisplay = this.display;
            IntPtr currentWindow = this.window;

            if (currentDisplay == IntPtr.Zero || currentWindow == IntPtr.Zero)
            {
                return;
            }

            X11.XResizeWindow(
                currentDisplay,
                currentWindow,
                (uint)Math.Max(1, width),
                (uint)Math.Max(1, height));

            X11.XFlush(currentDisplay);
        }

        public void Dispose()
        {
            this.DestroyView();
            GC.SuppressFinalize(this);
        }

        private void EventLoop()
        {
            while (this.running)
            {
                IntPtr currentDisplay = this.display;
                if (currentDisplay == IntPtr.Zero)
                {
                    break;
                }

                int result = X11.XNextEvent(currentDisplay, out X11.XEvent ev);
                if (result != 0)
                {
                    continue;
                }

                if (!this.running)
                {
                    break;
                }

                this.ProcessEvent(ref ev);
            }
        }

        private void ProcessEvent(ref X11.XEvent ev)
        {
            var cb = this.callbacks;
            if (cb == null || this.destroyRequested)
            {
                return;
            }

            // Copy event data to local variables before using in lambdas to avoid CS1628
            var xmotion = ev.xmotion;
            var xbutton = ev.xbutton;
            var xkey = ev.xkey;

            switch ((X11.EventType)ev.type)
            {
                case X11.EventType.MotionNotify:
                    this.PostToUi(() => cb.MouseMove?.Invoke(xmotion.x, xmotion.y));
                    break;

                case X11.EventType.ButtonPress:
                    this.PostToUi(() =>
                    {
                        cb.FocusRequested?.Invoke();

                        switch (xbutton.button)
                        {
                            case 1:
                                cb.MouseDown?.Invoke(0, xbutton.x, xbutton.y);
                                break;
                            case 3:
                                cb.MouseDown?.Invoke(1, xbutton.x, xbutton.y);
                                break;
                            case 2:
                                cb.MouseDown?.Invoke(2, xbutton.x, xbutton.y);
                                break;
                            case 4:
                                cb.MouseWheel?.Invoke(120);
                                break;
                            case 5:
                                cb.MouseWheel?.Invoke(-120);
                                break;
                        }
                    });
                    break;

                case X11.EventType.ButtonRelease:
                    this.PostToUi(() =>
                    {
                        switch (xbutton.button)
                        {
                            case 1:
                                cb.MouseUp?.Invoke(0, xbutton.x, xbutton.y);
                                break;
                            case 3:
                                cb.MouseUp?.Invoke(1, xbutton.x, xbutton.y);
                                break;
                            case 2:
                                cb.MouseUp?.Invoke(2, xbutton.x, xbutton.y);
                                break;
                        }
                    });
                    break;

                case X11.EventType.KeyPress:
                    {
                        int vk = X11KeyMapper.ToWindowsVirtualKey(this.display, ref ev.xkey);
                        if (vk != 0)
                        {
                            this.PostToUi(() => cb.KeyDown?.Invoke(vk));
                        }

                        break;
                    }

                case X11.EventType.KeyRelease:
                    {
                        int vk = X11KeyMapper.ToWindowsVirtualKey(this.display, ref ev.xkey);
                        if (vk != 0)
                        {
                            this.PostToUi(() => cb.KeyUp?.Invoke(vk));
                        }

                        break;
                    }

                case X11.EventType.DestroyNotify:
                    this.running = false;
                    break;
            }
        }

        private void PostToUi(Action action)
        {
            if (this.destroyRequested)
            {
                return;
            }

            if (Dispatcher.UIThread.CheckAccess())
            {
                action();
            }
            else
            {
                Dispatcher.UIThread.Post(action);
            }
        }
    }

    internal static class X11KeyMapper
    {
        // KeySym values.
        private const ulong XK_BackSpace = 0xFF08;
        private const ulong XK_Tab = 0xFF09;
        private const ulong XK_Return = 0xFF0D;
        private const ulong XK_Escape = 0xFF1B;
        private const ulong XK_space = 0x0020;

        private const ulong XK_Page_Up = 0xFF55;
        private const ulong XK_Page_Down = 0xFF56;
        private const ulong XK_End = 0xFF57;
        private const ulong XK_Home = 0xFF50;
        private const ulong XK_Left = 0xFF51;
        private const ulong XK_Up = 0xFF52;
        private const ulong XK_Right = 0xFF53;
        private const ulong XK_Down = 0xFF54;
        private const ulong XK_Insert = 0xFF63;
        private const ulong XK_Delete = 0xFFFF;

        private const ulong XK_Shift_L = 0xFFE1;
        private const ulong XK_Shift_R = 0xFFE2;
        private const ulong XK_Control_L = 0xFFE3;
        private const ulong XK_Control_R = 0xFFE4;
        private const ulong XK_Alt_L = 0xFFE9;
        private const ulong XK_Alt_R = 0xFFEA;
        private const ulong XK_Super_L = 0xFFEB;
        private const ulong XK_Super_R = 0xFFEC;

        private const ulong XK_F1 = 0xFFBE;
        private const ulong XK_F2 = 0xFFBF;
        private const ulong XK_F3 = 0xFFC0;
        private const ulong XK_F4 = 0xFFC1;
        private const ulong XK_F5 = 0xFFC2;
        private const ulong XK_F6 = 0xFFC3;
        private const ulong XK_F7 = 0xFFC4;
        private const ulong XK_F8 = 0xFFC5;
        private const ulong XK_F9 = 0xFFC6;
        private const ulong XK_F10 = 0xFFC7;
        private const ulong XK_F11 = 0xFFC8;
        private const ulong XK_F12 = 0xFFC9;

        public static int ToWindowsVirtualKey(IntPtr display, ref X11.XKeyEvent keyEvent)
        {
            if (display == IntPtr.Zero)
            {
                return 0;
            }

            ulong keysym = X11.XLookupKeysym(ref keyEvent, 0);
            if (keysym == 0)
            {
                return 0;
            }

            if (keysym >= '0' && keysym <= '9')
            {
                return (int)keysym;
            }

            if (keysym >= 'A' && keysym <= 'Z')
            {
                return (int)keysym;
            }

            if (keysym >= 'a' && keysym <= 'z')
            {
                return (int)(keysym - 32);
            }

            return keysym switch
            {
                XK_BackSpace => 0x08,
                XK_Tab => 0x09,
                XK_Return => 0x0D,
                XK_Escape => 0x1B,
                XK_space => 0x20,

                XK_Page_Up => 0x21,
                XK_Page_Down => 0x22,
                XK_End => 0x23,
                XK_Home => 0x24,
                XK_Left => 0x25,
                XK_Up => 0x26,
                XK_Right => 0x27,
                XK_Down => 0x28,
                XK_Insert => 0x2D,
                XK_Delete => 0x2E,

                XK_Super_L => 0x5B,
                XK_Super_R => 0x5C,

                XK_F1 => 0x70,
                XK_F2 => 0x71,
                XK_F3 => 0x72,
                XK_F4 => 0x73,
                XK_F5 => 0x74,
                XK_F6 => 0x75,
                XK_F7 => 0x76,
                XK_F8 => 0x77,
                XK_F9 => 0x78,
                XK_F10 => 0x79,
                XK_F11 => 0x7A,
                XK_F12 => 0x7B,

                XK_Shift_L => 0xA0,
                XK_Shift_R => 0xA1,
                XK_Control_L => 0xA2,
                XK_Control_R => 0xA3,
                XK_Alt_L => 0xA4,
                XK_Alt_R => 0xA5,

                _ => 0,
            };
        }
    }

    internal static class X11
    {
        private const string LibX11 = "libX11.so.6";

        [DllImport(LibX11)]
        public static extern IntPtr XOpenDisplay(string? display);

        [DllImport(LibX11)]
        public static extern int XCloseDisplay(IntPtr display);

        [DllImport(LibX11)]
        public static extern IntPtr XDefaultRootWindow(IntPtr display);

        [DllImport(LibX11)]
        public static extern int XDefaultScreen(IntPtr display);

        [DllImport(LibX11)]
        public static extern IntPtr XBlackPixel(IntPtr display, int screen);

        [DllImport(LibX11)]
        public static extern IntPtr XCreateSimpleWindow(
            IntPtr display,
            IntPtr parent,
            int x,
            int y,
            uint width,
            uint height,
            uint borderWidth,
            IntPtr border,
            IntPtr background);

        [DllImport(LibX11)]
        public static extern int XMapWindow(IntPtr display, IntPtr window);

        [DllImport(LibX11)]
        public static extern int XDestroyWindow(IntPtr display, IntPtr window);

        [DllImport(LibX11)]
        public static extern int XFlush(IntPtr display);

        [DllImport(LibX11)]
        public static extern int XSelectInput(IntPtr display, IntPtr window, EventMask eventMask);

        [DllImport(LibX11)]
        public static extern int XNextEvent(IntPtr display, out XEvent ev);

        [DllImport(LibX11)]
        public static extern int XResizeWindow(IntPtr display, IntPtr window, uint width, uint height);

        [DllImport(LibX11)]
        public static extern ulong XLookupKeysym(ref XKeyEvent key_event, int index);

        [Flags]
        public enum EventMask : long
        {
            ExposureMask = 1 << 15,
            PointerMotionMask = 1 << 6,
            ButtonPressMask = 1 << 2,
            ButtonReleaseMask = 1 << 3,
            KeyPressMask = 1 << 0,
            KeyReleaseMask = 1 << 1,
            StructureNotifyMask = 1 << 17,
        }

        public enum EventType : int
        {
            KeyPress = 2,
            KeyRelease = 3,
            ButtonPress = 4,
            ButtonRelease = 5,
            MotionNotify = 6,
            Expose = 12,
            DestroyNotify = 17,
            ConfigureNotify = 22,
        }

        [StructLayout(LayoutKind.Explicit, Size = 192)]
        public struct XEvent
        {
            [FieldOffset(0)] public int type;
            [FieldOffset(0)] public XKeyEvent xkey;
            [FieldOffset(0)] public XButtonEvent xbutton;
            [FieldOffset(0)] public XMotionEvent xmotion;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct XKeyEvent
        {
            public int type;
            public ulong serial;
            public int send_event;
            public IntPtr display;
            public IntPtr window;
            public IntPtr root;
            public IntPtr subwindow;
            public ulong time;
            public int x;
            public int y;
            public int x_root;
            public int y_root;
            public uint state;
            public uint keycode;
            public int same_screen;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct XButtonEvent
        {
            public int type;
            public ulong serial;
            public int send_event;
            public IntPtr display;
            public IntPtr window;
            public IntPtr root;
            public IntPtr subwindow;
            public ulong time;
            public int x;
            public int y;
            public int x_root;
            public int y_root;
            public uint state;
            public uint button;
            public int same_screen;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct XMotionEvent
        {
            public int type;
            public ulong serial;
            public int send_event;
            public IntPtr display;
            public IntPtr window;
            public IntPtr root;
            public IntPtr subwindow;
            public ulong time;
            public int x;
            public int y;
            public int x_root;
            public int y_root;
            public uint state;
            public byte is_hint;
            public int same_screen;
        }
    }
}