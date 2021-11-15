using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;

namespace UIWindowSystemsDemo
{
    public class ResetCameraComponent : Component
    {
        [BindComponent(isExactType: false)]
        protected Camera camera;

        // The service will be null when the scene is executed from the Editor.
        [BindService(false)]
        protected InteractionService service;

        private Vector3 initialPosition;
        private Quaternion initialOrientation;

        protected override void OnActivated()
        {
            base.OnActivated();

            this.initialPosition = camera.Transform.Position;
            this.initialOrientation = camera.Transform.Orientation;

            if (service != null)
            {
                service.CameraReset += Service_CameraReset;
            }
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            if (service != null)
            {
                service.CameraReset -= Service_CameraReset;
            }
        }

        private void Service_CameraReset(object sender, System.EventArgs e)
        {
            this.camera.Transform.Position = initialPosition;
            this.camera.Transform.Orientation = initialOrientation;
        }
    }
}
