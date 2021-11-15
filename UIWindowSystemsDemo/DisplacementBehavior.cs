using System;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;

namespace UIWindowSystemsDemo
{
    public class DisplacementBehavior : Behavior
    {
        [BindService(isRequired: false)]
        protected InteractionService service;

        [BindComponent]
        protected Transform3D transform;

        private float offsetValue;
        private Vector3 initialPosition;

        protected override void OnActivated()
        {
            base.OnActivated();

            offsetValue = service?.Displacement ?? 0;
            initialPosition = transform.LocalPosition;
        }

        protected override void Update(TimeSpan gameTime)
        {
            if (service != null && offsetValue != service.Displacement)
            {
                offsetValue = service.Displacement;

                
                transform.LocalPosition = initialPosition + new Vector3(0, 0, (offsetValue * 1.5f) / 10f);
            }
        }
    }
}
