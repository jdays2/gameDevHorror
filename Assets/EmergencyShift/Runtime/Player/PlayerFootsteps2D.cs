using EmergencyShift.Core.Randomness;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Contacts follow real travel, so walking into a wall or teleporting produces no fake steps.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D), typeof(PlayerLife2D))]
    public sealed class PlayerFootsteps2D : MonoBehaviour
    {
        [SerializeField]
        private PlayerWorldAudioSet sounds;
        [SerializeField]
        private AudioSource leftSource;
        [SerializeField]
        private AudioSource rightSource;
        [SerializeField, Min(.1f)]
        private float distancePerStep = 1.05f;
        [SerializeField, Min(.05f)]
        private float minimumInterval = .22f;
        [SerializeField, Range(0, .1f)]
        private float pitchVariation = .025f;
        private Rigidbody2D body;
        private PlayerLife2D life;
        private Vector2 previous;
        private float travelled;
        private float nextStep;
        private bool left;
        private readonly int[] last =
        {
            -1,
            -1,
            -1
        };
        private IRandomSource random;
        private GroundAudioSurfaceQuery2D surfaces;
        public int StepsPlayed
        {
            get; private set;
        }
        public GroundSoundSurface LastSurface
        {
            get; private set;
        }

        [Inject]
        private void Construct(GroundAudioSurfaceQuery2D groundSurfaces, RandomSourceFactory randomSources)
        {
            surfaces = groundSurfaces;
            random = randomSources.Create();
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            life = GetComponent<PlayerLife2D>();
            Prepare(leftSource);
            Prepare(rightSource);
        }

        private static void Prepare(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0;
            source.dopplerLevel = 0;
            source.priority = 160;
            source.Stop();
        }

        private void OnEnable()
        {
            if (body != null)
            {
                previous = body.position;
            }

            travelled = 0;
            nextStep = 0;
        }

        private void Update()
        {
            Vector2 position = body.position;
            float distance = Vector2.Distance(position, previous);
            previous = position;
            if (life.IsDead || !body.simulated || Time.timeScale <= 0 || distance > 2f)
            {
                travelled = 0;
                return;
            }

            // Render frames may occur between physics ticks; do not erase a partial stride then.
            if (distance < .00001f)
            {
                if (body.linearVelocity.sqrMagnitude < .001f)
                {
                    travelled = 0;
                }

                return;
            }

            travelled += distance;
            if (travelled < distancePerStep || Time.time < nextStep)
            {
                return;
            }

            travelled %= distancePerStep;
            nextStep = Time.time + minimumInterval;
            LastSurface = surfaces.At(position);
            var clips = sounds != null ? sounds.Steps(LastSurface) : null;
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            var source = left ? leftSource : rightSource;
            left = !left;
            if (source == null)
            {
                return;
            }

            int id = random.NextInt(0, clips.Length);
            int previousId = last[(int)LastSurface];
            if (clips.Length > 1 && id == previousId)
            {
                id = (id + 1 + random.NextInt(0, clips.Length - 1)) % clips.Length;
            }

            last[(int)LastSurface] = id;
            if (clips[id] == null)
            {
                return;
            }

            source.clip = clips[id];
            source.volume = sounds.stepVolume;
            source.pitch = 1 + ((float)random.NextUnit() * 2 - 1) * pitchVariation;
            source.Play();
            StepsPlayed++;
        }

        private void OnDisable()
        {
            if (leftSource != null)
            {
                leftSource.Stop();
            }

            if (rightSource != null)
            {
                rightSource.Stop();
            }

            travelled = 0;
        }
    }
}
