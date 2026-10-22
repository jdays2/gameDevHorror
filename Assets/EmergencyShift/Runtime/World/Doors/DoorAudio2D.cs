using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Recorded rail movement follows actual door events; two saved voices, no timed fake close.</summary>
    [RequireComponent(typeof(Door2D)), DisallowMultipleComponent]
    public sealed class DoorAudio2D : MonoBehaviour
    {
        [SerializeField, Tooltip("Use all local values, including values equal to former defaults.")]
        private bool useAuthoredAudioTuning;
        private float Volume => AudioProfileValidation.Resolve(volume, .85f, sounds != null ? sounds.Volume : .85f, useAuthoredAudioTuning);
        private float MovementVolume => AudioProfileValidation.Resolve(movementVolume, .4f, sounds != null ? sounds.MovementVolume : .4f, useAuthoredAudioTuning);
        private float HearingDistance => AudioProfileValidation.Resolve(hearingDistance, 12, sounds != null ? sounds.HearingDistance : 12, useAuthoredAudioTuning);
        [SerializeField]
        private DoorAudioSet sounds;
        [SerializeField]
        private AudioSource motion;
        [SerializeField]
        private AudioSource transient;
        [SerializeField]
        private Transform listener;
        [SerializeField, Range(0, 1)]
        private float volume = .85f;
        [SerializeField, Range(0, 1)]
        private float movementVolume = .4f;
        [SerializeField, Min(1)]
        private float hearingDistance = 12;
        private Door2D door;
        private System.Random random;
        private int lastStop = -1;
        public int StartsPlayed
        {
            get; private set;
        }
        public int StopsPlayed
        {
            get; private set;
        }
        public bool Sliding => motion != null && motion.isPlaying;

        public void Configure(DoorAudioSet set, AudioSource body, AudioSource click, Transform target)
        {
            sounds = set;
            motion = body;
            transient = click;
            listener = target;
        }

        public void ConfigureListener(Transform target)
        {
            listener = target;
        }

        [Inject]
        private void Construct(PlayerLife2D scenePlayer)
        {
            if (listener == null && scenePlayer != null)
            {
                listener = scenePlayer.transform;
            }
        }

        private void ValidateResolvedTuning()
        {
            if (!AudioProfileValidation.Unit(Volume) ||
                !AudioProfileValidation.Unit(MovementVolume) ||
                !AudioProfileValidation.Positive(HearingDistance))
            {
                throw new System.InvalidOperationException("Invalid resolved audio tuning: " + name);
            }
        }

        private void Awake()
        {
            sounds?.Validate();
            ValidateResolvedTuning();
            door = GetComponent<Door2D>();
            random = new System.Random(System.Guid.NewGuid().GetHashCode());

        }

        private void OnEnable()
        {
            door.MotionStarted += Begin;
            door.MotionStopped += End;
        }

        private void OnDisable()
        {
            door.MotionStarted -= Begin;
            door.MotionStopped -= End;
            motion?.Stop();
            transient?.Stop();
        }

        private void Begin(bool opening)
        {
            if (sounds == null || motion == null || transient == null || !motion.isActiveAndEnabled || !transient.isActiveAndEnabled)
            {
                return;
            }

            var clips = opening ? sounds.OpenStart : sounds.CloseStart;
            if (clips.Count > 0)
            {
                transient.clip = clips[random.Next(clips.Count)];
                transient.pitch = 1;
                transient.Play();
                StartsPlayed++;
            }

            motion.clip = sounds.SlideLoop;
            motion.loop = true;
            motion.pitch = 1;
            Mix();
            if (motion.clip != null)
            {
                motion.Play();
            }
        }

        private void End(bool opened)
        {
            motion?.Stop();
            // Child sources may already be disabled when Door2D emits its interrupted-motion event.
            if (!isActiveAndEnabled || sounds == null || transient == null || !transient.isActiveAndEnabled || sounds.Stop.Count == 0)
            {
                return;
            }

            int index = random.Next(sounds.Stop.Count);
            if (sounds.Stop.Count > 1 && index == lastStop)
            {
                index = (index + 1) % sounds.Stop.Count;
            }

            lastStop = index;
            transient.clip = sounds.Stop[index];
            transient.pitch = 1;
            Mix();
            transient.Play();
            StopsPlayed++;
        }

        private void Update()
        {
            if ((motion != null && motion.isPlaying) || (transient != null && transient.isPlaying))
            {
                Mix();
            }
        }

        private void Mix()
        {
            if (listener == null)
            {
                return;
            }

            Vector2 delta = (Vector2)transform.position - (Vector2)listener.position;
            float gain = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(sounds.FullVolumeDistance, HearingDistance, delta.magnitude));
            float pan = Mathf.Clamp(delta.x / sounds.PanDistance, -sounds.MaximumPan, sounds.MaximumPan);
            if (motion != null)
            {
                motion.volume = Volume * MovementVolume * gain;
                motion.panStereo = pan;
            }

            if (transient != null)
            {
                transient.volume = Volume * gain;
                transient.panStereo = pan;
            }
        }
    }
}
