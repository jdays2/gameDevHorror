using EmergencyShift.Features.Flashlight.Logic;
using UnityEngine;

namespace EmergencyShift.Features.Flashlight.View
{
    public sealed class FlashlightAimEnvironment2D : IFlashlightAimEnvironment
    {
        private readonly Transform owner;
        private readonly Transform pivot;

        public FlashlightAimEnvironment2D(Transform owner, Transform pivot)
        {
            this.owner = owner;
            this.pivot = pivot;
        }

        public bool IsAvailable => owner != null && pivot != null;
        public Vector2 OwnerPosition => owner.position;
        public float Angle => pivot.eulerAngles.z;

        public void SetAngle(float angle) => pivot.rotation = Quaternion.Euler(0, 0, angle);
    }
}
