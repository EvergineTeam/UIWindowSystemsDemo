using System;

namespace UIWindowSystemsDemo.Avalonia.Platform
{
    /// <summary>
    /// Groups all raw-input callbacks that a platform backend invokes
    /// so <see cref="EvergineControl"/> can dispatch Avalonia events.
    /// </summary>
    internal sealed class NativeInputCallbacks
    {
        public Action<int, int>? MouseMove { get; init; }
        public Action<int, int, int>? MouseDown { get; init; }
        public Action<int, int, int>? MouseUp { get; init; }
        public Action<int>? MouseWheel { get; init; }
        public Action<int>? KeyDown { get; init; }
        public Action<int>? KeyUp { get; init; }
        public Action<int, int>? TouchMove { get; init; }
        public Action<int, int>? TouchDown { get; init; }
        public Action<int, int>? TouchUp { get; init; }
        public Action? FocusRequested { get; init; }
    }
}