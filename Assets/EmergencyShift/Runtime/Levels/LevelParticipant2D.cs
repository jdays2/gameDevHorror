using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Scene-authored lifetime boundary; the level does not know enemy species.</summary>
    [DisallowMultipleComponent]
    public sealed class LevelParticipant2D : MonoBehaviour
    {
        [SerializeField]
        private Behaviour[] activity;
        [SerializeField]
        private Rigidbody2D body;
        public void Configure(Behaviour[] components, Rigidbody2D physicsBody)
        {
            activity = components;
            body = physicsBody;
        }

        public void StopActivity()
        {
            if (activity != null)
            {
                foreach (var component in activity)
                {
                    if (component != null)
                    {
                        component.enabled = false;
                    }
                }
            }

            if (body == null)
            {
                return;
            }

            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }
    }
}
