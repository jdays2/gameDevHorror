using EmergencyShift.Features.Flashlight.Logic;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Presentation follow/zoom; optics are cached from the owning player's events.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField]
        private Transform target;
        [SerializeField, Min(.01f)]
        private float smoothing = .18f;
        [SerializeField]
        private CameraFramingProfile framingProfile;
        private Camera view;
        private IFlashlightOpticsSource optics;
        private Vector3 velocity;
        private float zoomVelocity;
        private float targetSize;
        private float previousAspect;
        private bool framingDirty = true;
        private bool subscribed;
        private Vector2 impactOffset;
        private Vector3 appliedImpact;

        [Inject]
        private void Construct(PlayerLife2D scenePlayer)
        {
            if (target == null && scenePlayer != null)
            {
                Configure(scenePlayer.transform);
            }
        }

        private void Awake()
        {
            view = GetComponent<Camera>();
            Configure(target);
        }

        private void OnEnable() => Bind();
        private void Start() => SnapToTarget();
        private void OnDisable()
        {
            Unbind();
            ClearImpact();
        }
        private void OnDestroy() => Unbind();

        private void OnValidate()
        {
            framingDirty = true;
            if (Application.isPlaying)
            {
                Configure(target);
            }
        }

        private void LateUpdate()
        {
            // Smooth only the follow position. Feeding the previous kick into SmoothDamp causes drift.
            transform.position -= appliedImpact;
            appliedImpact = Vector3.zero;
            if (target == null || Time.deltaTime <= 0)
            {
                return;
            }

            Vector3 destination = new Vector3(target.position.x, target.position.y, -10);
            transform.position = Vector3.SmoothDamp(transform.position, destination, ref velocity, smoothing);
            appliedImpact = transform.TransformDirection(new Vector3(impactOffset.x, impactOffset.y, 0));
            transform.position += appliedImpact;
            UpdateFraming(false);
        }

        public void SetImpactOffset(Vector2 screenOffset)
        {
            impactOffset = screenOffset;
        }

        public void Configure(Transform player)
        {
            Unbind();
            ClearImpact();
            target = player;
            optics = target != null ? target.GetComponent<IFlashlightOpticsSource>() : null;
            framingDirty = true;
            Bind();
        }

        public void SetFraming(CameraFramingProfile profile)
        {
            profile.Validate();
            framingProfile = profile;
            framingDirty = true;
        }

        public void SnapToTarget()
        {
            ClearImpact();
            if (target == null)
            {
                return;
            }

            if (optics == null)
            {
                Configure(target);
            }

            velocity = Vector3.zero;
            transform.position = new Vector3(target.position.x, target.position.y, -10);
            UpdateFraming(true);
        }

        private void ClearImpact()
        {
            transform.position -= appliedImpact;
            appliedImpact = Vector3.zero;
            impactOffset = Vector2.zero;
        }

        private void Bind()
        {
            framingDirty = true;
            if (subscribed || optics == null || !isActiveAndEnabled)
            {
                return;
            }

            optics.OwnershipChanged += OnOpticsChanged;
            optics.OpticsChanged += OnOpticsChanged;
            subscribed = true;
        }

        private void Unbind()
        {
            if (!subscribed)
            {
                return;
            }

            optics.OwnershipChanged -= OnOpticsChanged;
            optics.OpticsChanged -= OnOpticsChanged;
            subscribed = false;
        }

        private void OnOpticsChanged() => framingDirty = true;

        private void UpdateFraming(bool snap)
        {
            if (framingProfile == null || optics == null)
            {
                return;
            }

            if (view == null)
            {
                view = GetComponent<Camera>();
            }

            float aspect = view.aspect;
            if (framingDirty || !Mathf.Approximately(aspect, previousAspect))
            {
                targetSize = framingProfile.OrthographicSize(optics.Range, aspect, optics.IsOwned);
                previousAspect = aspect;
                framingDirty = false;
            }

            if (snap)
            {
                zoomVelocity = 0;
                view.orthographicSize = targetSize;
                return;
            }

            view.orthographicSize = Mathf.SmoothDamp(view.orthographicSize, targetSize,
                ref zoomVelocity, framingProfile.ZoomSmoothTime);
        }
    }
}
