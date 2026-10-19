using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Follows one scene player; framing tracks that player's installed flashlight optics.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField]
        private Transform target;
        [SerializeField, Min(0.01f)]
        private float smoothing = 0.18f;
        [SerializeField]
        private CameraFramingProfile framingProfile;
        private Camera view;
        private Flashlight2D flashlight;
        private Vector3 velocity;
        private float zoomVelocity;

        public void Configure(Transform player)
        {
            target = player;
            flashlight = target != null ? target.GetComponent<Flashlight2D>() : null;
        }

        public void SetFraming(CameraFramingProfile profile)
        {
            profile.Validate();
            framingProfile = profile;
        }

        [Inject]
        private void Construct(PlayerLife2D scenePlayer)
        {
            if (target == null && scenePlayer != null)
            {
                Configure(scenePlayer.transform);
            }
        }

        private void Start()
        {
            Configure(target);

            SnapToTarget();
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            velocity = Vector3.zero;
            transform.position = new Vector3(target.position.x, target.position.y, -10);
            UpdateFraming(true);
        }

        private void LateUpdate()
        {
            if (target == null || Time.deltaTime <= 0)
            {
                return;
            }

            Vector3 destination = new Vector3(target.position.x, target.position.y, -10);
            transform.position = Vector3.SmoothDamp(transform.position, destination, ref velocity, smoothing);
            UpdateFraming(false);
        }

        private void UpdateFraming(bool snap)
        {
            if (framingProfile == null || flashlight == null)
            {
                return;
            }

            if (view == null)
            {
                view = GetComponent<Camera>();
            }

            float size = framingProfile.OrthographicSize(flashlight.Range, view.aspect, flashlight.IsOwned);
            if (snap)
            {
                zoomVelocity = 0;
                view.orthographicSize = size;
                return;
            }

            view.orthographicSize = Mathf.SmoothDamp(view.orthographicSize, size,
                ref zoomVelocity, framingProfile.zoomSmoothTime);
        }
    }
}
