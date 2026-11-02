using System;
using EmergencyShift.Features.WorldAudio.View;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace EmergencyShift.Features.Scorpion.View
{
    [RequireComponent(typeof(Scorpion2D))]
    public sealed class ScorpionAudio2D : MonoBehaviour
    {
        [SerializeField] private AudioSource voice;
        [SerializeField] private AudioSource claws;
        [SerializeField] private AudioSource contact;
        [SerializeField] private AudioSource presence;
        [SerializeField] private AudioSource detection;
        private EnemyDeathAudioVariants deathVariants;
        private Scorpion2D actor;
        private Vector2 previous;
        private float travelled;
        private float nextContact;
        private float nextHit;
        private float nextMix;
        private bool attackSound;
        private bool deathPlayed;
        private float nextSqueak;
        private int previousSqueak = -1;
        private bool wasAggro;
        private float obstruction = 1;
        public int StepsPlayed { get; private set; }
        public int AlarmsPlayed { get; private set; }
        public int AttacksPlayed { get; private set; }
        public int HitsPlayed { get; private set; }
        public int DeathsPlayed { get; private set; }
        public int DetectionAccentsPlayed { get; private set; }
        public int SqueaksPlayed { get; private set; }
        public event Action<float, float, bool> PerceptibleCue;

        [Inject]
        private void Construct(EnemyDeathAudioVariants variants)
        {
            deathVariants = variants;
        }

        [Inject]
        private void RouteWorldAudio(WorldAudioBus bus = null) => bus?.Route(voice, claws, contact, presence, detection);

        private void Awake()
        {
            actor = GetComponent<Scorpion2D>();
            previous = transform.position;
            Prepare(voice);
            Prepare(claws);
            Prepare(contact);
            Prepare(presence);
            Prepare(detection);
        }

        private static void Prepare(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0;
            source.dopplerLevel = 0;
            source.Stop();
        }

        private void OnEnable()
        {
            actor.Hit += OnHit;
            actor.Died += OnDeath;
            previous = transform.position;
            attackSound = false;
            ScheduleNextSqueak();
        }

        private void OnDisable()
        {
            actor.Hit -= OnHit;
            actor.Died -= OnDeath;
            Stop();
        }

        private void Update()
        {
            if (!actor.enabled && actor.IsAlive)
            {
                Stop();
                return;
            }
            // PauseMenu pauses the listener; retain the playhead for resume.
            if (Time.timeScale <= 0 || AudioListener.pause)
            {
                return;
            }
            if (Time.time >= nextMix)
            {
                UpdateMix();
                if (actor.IsAlive)
                {
                    Report(voice, actor.HasAggro);
                    Report(claws, true);
                    Report(contact, actor.IsAttacking);
                    Report(presence, false);
                    Report(detection, true);
                }
            }
            // The brain is disabled on death; its existing voice source owns the remaining tail.
            if (!actor.IsAlive)
            {
                if (!voice.isPlaying && !detection.isPlaying)
                {
                    enabled = false;
                }
                return;
            }
            var set = actor.Presentation;
            if (actor.HasAggro != wasAggro)
            {
                ScheduleNextSqueak();
            }
            if (Time.time >= nextSqueak && !presence.isPlaying && !actor.IsAttacking && !detection.isPlaying && set.SqueakCount > 0)
            {
                int selected = Random.Range(0, set.SqueakCount);
                if (set.SqueakCount > 1 && selected == previousSqueak)
                {
                    selected = (selected + 1) % set.SqueakCount;
                }
                previousSqueak = selected;
                presence.clip = set.Squeak(selected);
                presence.loop = false;
                presence.pitch = 1;
                presence.Play();
                SqueaksPlayed++;
                ScheduleNextSqueak();
                Report(presence, false);
            }
            Vector2 position = transform.position;
            float distance = Vector2.Distance(position, previous);
            previous = position;
            if (distance < set.DistancePerCycle && actor.HasAggro)
            {
                travelled += distance;
                if (travelled >= set.ContactDistance && Time.time >= nextContact && !contact.isPlaying)
                {
                    travelled %= set.ContactDistance;
                    nextContact = Time.time + .08f;
                    contact.pitch = Random.Range(.96f, 1.04f);
                    contact.clip = set.Step(Random.Range(0, set.StepCount));
                    contact.Play();
                    StepsPlayed++;
                }
            }
            else
            {
                travelled = 0;
            }
            if (actor.IsAttacking && !attackSound && actor.AttackProgress >= .32f)
            {
                claws.clip = set.Grasp;
                claws.pitch = Random.Range(.96f, 1.03f);
                claws.Play();
                AttacksPlayed++;
                attackSound = true;
            }
            if (!actor.IsAttacking)
            {
                attackSound = false;
            }
        }

        public void PlayAlarm()
        {
            if (!isActiveAndEnabled || !actor.IsAlive || !actor.enabled)
            {
                return;
            }
            UpdateMix();
            presence.Stop();
            ScheduleNextSqueak();
            detection.clip = actor.Presentation.Alarm;
            detection.pitch = 1;
            detection.Play();
            AlarmsPlayed++;
            DetectionAccentsPlayed++;
            Report(detection, true);
        }

        private void ScheduleNextSqueak()
        {
            wasAggro = actor.HasAggro;
            Vector2 interval = wasAggro ? actor.Presentation.RunSqueakInterval : actor.Presentation.IdleSqueakInterval;
            nextSqueak = Time.time + Random.Range(interval.x, interval.y);
        }

        private void OnHit()
        {
            // Fatal damage raises Died synchronously before Scorpion2D raises Hit.
            if (!actor.IsAlive || Time.time < nextHit)
            {
                return;
            }
            nextHit = Time.time + .12f;
            voice.pitch = 1;
            voice.clip = actor.Presentation.Hit;
            UpdateMix();
            voice.Play();
            HitsPlayed++;
            Report(voice, true);
        }

        private void OnDeath()
        {
            if (deathPlayed)
            {
                return;
            }
            deathPlayed = true;
            StopLiving();
            voice.Stop();
            voice.pitch = 1;
            voice.clip = deathVariants.Choose(actor.Presentation.DeathSounds);
            UpdateMix();
            if (voice.clip != null)
            {
                voice.Play();
                DeathsPlayed++;
            }
        }

        private void UpdateMix()
        {
            var set = actor.Presentation;
            nextMix = Time.time + .12f;
            float gain = 0;
            if (actor.Target != null)
            {
                float distance = Vector2.Distance(actor.Target.position, transform.position);
                float attenuation = Mathf.Clamp01(1 - distance / set.SoundRadius);
                bool blocked = Physics2D.Linecast(transform.position, actor.Target.position, actor.BlockingGeometry).collider != null;
                obstruction = blocked ? .18f : 1;
                gain = attenuation * attenuation * obstruction;
            }
            float voiceVolume = set.VoiceVolume;
            if (deathPlayed)
            {
                voiceVolume *= set.DeathSounds.Volume;
            }
            else if (voice.clip == set.Hit)
            {
                voiceVolume = set.HitVolume;
            }
            voice.volume = voiceVolume * gain;
            claws.volume = set.VoiceVolume * gain;
            contact.volume = set.StepVolume * gain;
            presence.volume = set.SqueakVolume * gain;
            detection.volume = set.DetectionVolume * gain;
        }

        private void Report(AudioSource source, bool urgent)
        {
            if (source.isPlaying && !source.isVirtual && !source.mute && source.volume >= .02f &&
                !AudioListener.pause && AudioListener.volume > .001f)
            {
                PerceptibleCue?.Invoke(.3f, source.volume, urgent);
            }
        }

        private void StopLiving()
        {
            claws.Stop();
            contact.Stop();
            presence.Stop();
            attackSound = false;
        }

        private void Stop()
        {
            voice.Stop();
            detection.Stop();
            StopLiving();
        }
    }
}
