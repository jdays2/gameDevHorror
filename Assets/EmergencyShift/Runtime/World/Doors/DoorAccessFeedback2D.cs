using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Reader feedback is independent of the existing rail and latch voices.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(DoorAccess2D))]
    public sealed class DoorAccessFeedback2D : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        private DoorAccess2D access;
        private Transform listener;

        public void Configure(AudioSource voice)
        {
            source = voice;
        }

        [Inject]
        private void Construct(PlayerLife2D scenePlayer)
        {
            listener = scenePlayer != null ? scenePlayer.transform : null;
        }

        private void Awake()
        {
            access = GetComponent<DoorAccess2D>();

        }

        private void OnEnable()
        {
            access.AccessChecked += Play;
        }

        private void OnDisable()
        {
            access.AccessChecked -= Play;
            source?.Stop();
        }

        private void Play(bool granted)
        {
            var profile = access.FeedbackProfile;
            if (profile == null || source == null || !source.isActiveAndEnabled)
            {
                return;
            }

            source.clip = granted ? profile.Granted : profile.Denied;
            source.loop = false;
            source.pitch = 1;
            Mix();
            source.Play();
        }

        private void Update()
        {
            if (source != null && source.isPlaying)
            {
                Mix();
            }
        }

        private void Mix()
        {
            var profile = access.FeedbackProfile;
            if (profile == null || listener == null)
            {
                return;
            }

            Vector2 delta = transform.position - listener.position;
            float gain = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(profile.FullVolumeRadius, profile.HearingDistance, delta.magnitude));
            source.volume = profile.Volume * gain;
            source.panStereo = Mathf.Clamp(delta.x / profile.PanDistance, -profile.MaximumPan, profile.MaximumPan);
        }
    }
}
