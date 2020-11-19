using System;
using WaveEngine.Framework.Services;

namespace UIWindowSystemsDemo
{
    public class InteractionService : Service
    {
        public event EventHandler CameraReset;

        public float RadioYRotation { get; set; }

        public void ResetCamera()
        {
            this.CameraReset?.Invoke(this, EventArgs.Empty);
        }
    }
}
