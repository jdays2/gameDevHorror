using EmergencyShift.Core.Randomness;
using EmergencyShift.Features.PlayerFootsteps;
using EmergencyShift.Features.PlayerFootsteps.Config;
using EmergencyShift.Features.PlayerFootsteps.Logic;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Contacts follow real travel, so walking into a wall or teleporting produces no fake steps.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D), typeof(PlayerLife2D))]
    public sealed class PlayerFootsteps2D : MonoBehaviour, IPlayerFootstepView
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
        private PlayerFootstepControllerFactory controllers;
        private PlayerFootstepController controller;
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
        private void Construct(GroundAudioSurfaceQuery2D groundSurfaces, [Inject(Id = RandomSourceIds.Cosmetic)] RandomSourceFactory randomSources,
            PlayerFootstepControllerFactory footstepControllers)
        {
            surfaces = groundSurfaces;
            random = randomSources.Create();
            controllers = footstepControllers;
        }

        private void Awake()
        {
            sounds?.Validate();
            body = GetComponent<Rigidbody2D>();
            life = GetComponent<PlayerLife2D>();
            Prepare(leftSource);
            Prepare(rightSource);
            controller = controllers.Create(new PlayerFootstepSettings(distancePerStep, minimumInterval), this);
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
                controller.Reset(body.position);
            }
        }

        private void Update()
        {
            controller.Sample(body.position, body.linearVelocity.sqrMagnitude,
                !life.IsDead && body.simulated && Time.timeScale > 0);
        }

        void IPlayerFootstepView.PlayStep(Vector2 position)
        {
            LastSurface = surfaces.At(position);
            var clips = sounds != null ? sounds.Steps(LastSurface) : default(AudioClipBank);
            if (clips.Count == 0)
            {
                return;
            }

            var source = left ? leftSource : rightSource;
            left = !left;
            if (source == null)
            {
                return;
            }

            int id = random.NextInt(0, clips.Count);
            int previousId = last[(int)LastSurface];
            if (clips.Count > 1 && id == previousId)
            {
                id = (id + 1 + random.NextInt(0, clips.Count - 1)) % clips.Count;
            }

            last[(int)LastSurface] = id;
            if (clips[id] == null)
            {
                return;
            }

            source.clip = clips[id];
            source.volume = sounds.StepVolume;
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

            if (body != null)
            {
                controller?.Reset(body.position);
            }
        }
    }
}
