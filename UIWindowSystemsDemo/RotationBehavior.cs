using System;
using WaveEngine.Framework;
using WaveEngine.Framework.Graphics;
using WaveEngine.Mathematics;

namespace UIWindowSystemsDemo
{
    public class RotationBehavior : Behavior
    {
        [BindService(isRequired: false)]
        protected InteractionService service;

        [BindComponent]
        protected Transform3D transform;

        private float lastRotationValue;
        private Quaternion initialOrientation;

        protected override void OnActivated()
        {
            base.OnActivated();

            lastRotationValue = service?.RadioYRotation ?? 0;
            initialOrientation = transform.Orientation;
        }

        protected override void Update(TimeSpan gameTime)
        {
            if (service != null && lastRotationValue != service.RadioYRotation)
            {
                lastRotationValue = service.RadioYRotation;

                transform.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(service.RadioYRotation)) * initialOrientation;
            }
        }
    }
}
