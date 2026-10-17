using UnityEngine;

namespace EmergencyShift
{
    [DisallowMultipleComponent, RequireComponent(typeof(Sleeper2D))]
    public sealed class SleeperAudio2D : MonoBehaviour
    {
        [SerializeField] private SleeperAudioProfile profile;
        [SerializeField] private AudioSource breathing;
        [SerializeField] private AudioSource voice;
        [SerializeField] private AudioSource contact;
        private Sleeper2D actor;
        private AudioLowPassFilter breathingFilter;
        private AudioLowPassFilter voiceFilter;
        private AudioLowPassFilter contactFilter;
        private float nextPain;
        private float nextObstruction;
        private float obstruction = 1;
        private float voiceGain = 1;
        private float travelled;
        private Vector2 previousPosition;
        private float nextContact;
        private float nextHeard;
        public int WakeCount { get; private set; }
        public int PainCount { get; private set; }
        public int DeathCount { get; private set; }
        public AudioSource BreathingSource => breathing;
        public AudioSource VoiceSource => voice;
        public event System.Action<float, float, bool> PerceptibleCue;

        private void Awake()
        {
            actor = GetComponent<Sleeper2D>();
            breathingFilter = Prepare(breathing, true);
            voiceFilter = Prepare(voice, false);
            contactFilter = Prepare(contact, false);
            if (profile == null || breathing == null || voice == null || contact == null)
            {
                Debug.LogError("Missing Sleeper audio bindings: " + name, this);
                enabled = false;
            }
        }

        private static AudioLowPassFilter Prepare(AudioSource source, bool loop)
        {
            if (source == null)
            {
                return null;
            }
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.dopplerLevel = 0;
            source.loop = loop;
            source.Stop();
            return source.GetComponent<AudioLowPassFilter>();
        }

        private void OnEnable()
        {
            actor.StateChanged += OnState;
            actor.Damaged += OnHit;
            actor.AttackLunged += OnLunge;
            actor.AttackResolved += OnStrike;
            previousPosition = transform.position;
        }

        private void Update()
        {
            if (!actor.enabled && actor.IsAlive)
            {
                StopAll();
                return;
            }
            bool asleep = actor.State == Sleeper2D.BehaviourState.Sleeping;
            if (asleep && !breathing.isPlaying)
            {
                breathing.clip = profile.sleeping;
                if (breathing.clip != null)
                {
                    breathing.Play();
                    breathing.time = Random.Range(0, breathing.clip.length * .8f);
                }
            }
            else if (!asleep)
            {
                breathing.Stop();
            }

            UpdateMix();
            Vector2 position = transform.position;
            float distance = Vector2.Distance(position, previousPosition);
            previousPosition = position;
            if (actor.State != Sleeper2D.BehaviourState.Hunting || actor.IsStaggered || distance > 1.5f)
            {
                travelled = 0;
            }
            else
            {
                travelled += distance;
                if (travelled >= profile.distancePerContact && Time.time >= nextContact)
                {
                    travelled %= profile.distancePerContact;
                    nextContact = Time.time + .1f;
                    PlayContact(StalkerAudioSet.Cue.Step);
                }
            }
            if (!actor.IsAlive && !voice.isPlaying && actor.DeathTime >= 2)
            {
                enabled = false;
            }
        }

        private void OnState(Sleeper2D.BehaviourState state)
        {
            breathing.Stop();
            if (state == Sleeper2D.BehaviourState.Waking)
            {
                WakeCount++;
                PlayVoice(profile.waking);
                nextPain = Time.time + (profile.waking != null ? profile.waking.length : profile.painCooldown);
            }
            else if (state == Sleeper2D.BehaviourState.Dead)
            {
                DeathCount++;
                contact.Stop();
                AudioClip rupture = profile.deathSounds != null ? EnemyDeathAudioVariants.Choose(profile.deathSounds) : profile.rupture;
                float gain = profile.deathSounds != null ? profile.deathSounds.Volume : 1;
                PlayVoice(rupture, gain);
            }
            else if (state == Sleeper2D.BehaviourState.TargetDead)
            {
                StopAll();
            }
        }

        private void OnHit()
        {
            if (Time.time < nextPain || actor.State == Sleeper2D.BehaviourState.Waking)
            {
                return;
            }
            nextPain = Time.time + profile.painCooldown;
            PainCount++;
            PlayVoice(profile.pain);
        }

        private void OnLunge() => PlayContact(StalkerAudioSet.Cue.Lunge);
        private void OnStrike(bool landed)
        {
            if (landed)
            {
                PlayContact(StalkerAudioSet.Cue.Bite);
            }
        }

        private void PlayVoice(AudioClip clip, float gain = 1)
        {
            voiceGain = gain;
            voice.clip = clip;
            if (clip != null)
            {
                UpdateMix();
                voice.Play();
            }
        }

        private void PlayContact(StalkerAudioSet.Cue cue)
        {
            var clips = profile.contactSounds != null ? profile.contactSounds.Clips(cue) : null;
            if (clips == null || clips.Length == 0)
            {
                return;
            }
            contact.clip = clips[Random.Range(0, clips.Length)];
            UpdateMix();
            contact.Play();
        }

        private void UpdateMix()
        {
            if (actor.Player == null)
            {
                breathing.volume = 0;
                voice.volume = 0;
                contact.volume = 0;
                return;
            }
            if (Time.time >= nextObstruction)
            {
                nextObstruction = Time.time + profile.obstructionInterval;
                bool blocked = Physics2D.Linecast(transform.position, actor.Player.position, actor.BlockingGeometry).collider != null;
                obstruction = blocked ? profile.blockedVolume : 1;
                float cutoff = blocked ? profile.blockedCutoff : 22000;
                SetFilter(breathingFilter, cutoff);
                SetFilter(voiceFilter, cutoff);
                SetFilter(contactFilter, cutoff);
            }
            float distance = Vector2.Distance(transform.position, actor.Player.position);
            float falloff = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(profile.fullVolumeDistance, Mathf.Max(profile.fullVolumeDistance + .1f, profile.hearingDistance), distance));
            float gain = falloff * obstruction;
            float pan = Mathf.Clamp((transform.position.x - actor.Player.position.x) / profile.panDistance, -profile.maximumPan, profile.maximumPan);
            Mix(breathing, profile.breathingVolume * gain, pan);
            Mix(voice, profile.voiceVolume * voiceGain * gain, pan);
            Mix(contact, profile.contactVolume * gain, pan);
            if (Time.time >= nextHeard)
            {
                nextHeard = Time.time + .15f;
                Report(breathing, false);
                Report(voice, true);
                Report(contact, actor.State == Sleeper2D.BehaviourState.Attacking);
            }
        }

        private static void SetFilter(AudioLowPassFilter filter, float cutoff)
        {
            if (filter != null)
            {
                filter.cutoffFrequency = cutoff;
            }
        }

        private static void Mix(AudioSource source, float gain, float pan)
        {
            source.volume = Mathf.Clamp01(gain);
            source.panStereo = pan;
        }

        private void Report(AudioSource source, bool urgent)
        {
            if (source.isPlaying && !source.isVirtual && !source.mute && source.volume >= .02f && !AudioListener.pause && AudioListener.volume > .001f)
            {
                PerceptibleCue?.Invoke(.3f, source.volume, urgent);
            }
        }

        public void RestoreSilently()
        {
            StopAll();
            travelled = 0;
            previousPosition = transform.position;
            nextPain = Time.time + profile.painCooldown;
            nextObstruction = 0;
            enabled = actor.IsAlive;
        }

        private void StopAll()
        {
            breathing.Stop();
            voice.Stop();
            contact.Stop();
        }

        private void OnDisable()
        {
            if (actor != null)
            {
                actor.StateChanged -= OnState;
                actor.Damaged -= OnHit;
                actor.AttackLunged -= OnLunge;
                actor.AttackResolved -= OnStrike;
            }
            if (breathing != null && voice != null && contact != null)
            {
                StopAll();
            }
        }
    }
}
