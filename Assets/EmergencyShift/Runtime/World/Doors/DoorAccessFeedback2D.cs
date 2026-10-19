using UnityEngine;

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

        private void Awake()
        {
            access = GetComponent<DoorAccess2D>();
            var context = LevelContext2D.For(this);
            if (context != null && context.Player != null)
            {
                listener = context.Player.transform;
            }
            else if (Camera.main != null)
            {
                listener = Camera.main.transform;
            }
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

            source.clip = granted ? profile.granted : profile.denied;
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
            float gain = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(2, profile.hearingDistance, delta.magnitude));
            source.volume = profile.volume * gain;
            source.panStereo = Mathf.Clamp(delta.x / 6, -.75f, .75f);
        }
    }
}
