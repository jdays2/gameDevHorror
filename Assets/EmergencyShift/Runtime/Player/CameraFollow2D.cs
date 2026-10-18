using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Мягко следует за игроком после обновления движения.</summary>
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField]
        private Transform target;
        [SerializeField, Min(0.01f)]
        private float smoothing = 0.18f;
        private Vector3 velocity;
        public void Configure(Transform player) => target = player;
        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            velocity = Vector3.zero;
            transform.position = new Vector3(target.position.x, target.position.y, -10);
        }

        private void LateUpdate()
        {
            if (target == null || Time.deltaTime <= 0)
            {
                return;
            }

            Vector3 destination = new Vector3(target.position.x, target.position.y, -10);
            transform.position = Vector3.SmoothDamp(transform.position, destination, ref velocity, smoothing);
        }
    }
}
