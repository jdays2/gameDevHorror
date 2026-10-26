using UnityEngine;

namespace EmergencyShift.Features.Enemies.View
{
    [DefaultExecutionOrder(40), RequireComponent(typeof(StalkerLimbRig2D))]
    public sealed class StalkerRigShadows2D : MonoBehaviour
    {
        private StalkerLimbRig2D rig;
        private void Awake() => rig = GetComponent<StalkerLimbRig2D>();
        private void LateUpdate() => rig.SyncShadows();
    }
}
