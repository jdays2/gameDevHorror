using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Anchors a real Stalker target; its hit, sound and death pipeline keeps running.</summary>
    [RequireComponent(typeof(ShadowStalker2D))]
    public sealed class SandboxStationaryStalker2D : MonoBehaviour
    {
        private void Start()
        {
            var brain = GetComponent<ShadowStalker2D>();
            brain.AttacksEnabled = false;
            var body = GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            var crowd = GetComponent<CrowdAgent2D>();
            if (crowd != null)
            {
                crowd.enabled = false;
            }
        }
    }
}
