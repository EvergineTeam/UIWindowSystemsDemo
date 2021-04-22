using System;
using WaveEngine.Framework.Services;

namespace UIWindowSystemsDemo
{
    public class InteractionService : Service
    {
        public event EventHandler CameraReset;

        public float Displacement { get; set; }

        public void ResetCamera()
        {
            this.CameraReset?.Invoke(this, EventArgs.Empty);
        }
    }
}
