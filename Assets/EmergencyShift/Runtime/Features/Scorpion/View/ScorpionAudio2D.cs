using UnityEngine;

namespace EmergencyShift.Features.Scorpion.View
{
    [RequireComponent(typeof(Scorpion2D))]
    public sealed class ScorpionAudio2D : MonoBehaviour
    {
        [SerializeField] private AudioSource voice;
        [SerializeField] private AudioSource contact;
        [SerializeField] private AudioSource presence;
        private Scorpion2D actor;
        private Vector2 previous;
        private float travelled;
        private float nextContact;
        private float nextHit;
        private float nextMix;
        private bool attackSound;
        private float obstruction = 1;
        public int StepsPlayed { get; private set; }
        public int AlarmsPlayed { get; private set; }
        public int AttacksPlayed { get; private set; }
        public int HitsPlayed { get; private set; }

        private void Awake()
        {
            actor = GetComponent<Scorpion2D>();
            previous = transform.position;
            Prepare(voice);
            Prepare(contact);
            Prepare(presence);
        }

        private static void Prepare(AudioSource source)
        {
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.dopplerLevel = 0;
            source.Stop();
        }

        private void OnEnable()
        {
            actor.Hit += OnHit;
            actor.Died += Stop;
            previous = transform.position;
        }

        private void OnDisable()
        {
            actor.Hit -= OnHit;
            actor.Died -= Stop;
            Stop();
        }

        private void Update()
        {
            if (!actor.enabled || !actor.IsAlive || Time.timeScale <= 0)
            {
                Stop();
                return;
            }
            var set = actor.Presentation;
            if (Time.time >= nextMix)
            {
                nextMix = Time.time + .12f;
                Vector2 delta = actor.Target != null ? (Vector2)actor.Target.position - (Vector2)transform.position : Vector2.one * set.SoundRadius;
                float attenuation = Mathf.Clamp01(1 - delta.magnitude / set.SoundRadius);
                obstruction = Physics2D.Linecast(transform.position,
                    actor.Target != null ? actor.Target.position : transform.position, actor.BlockingGeometry).collider == null ? 1 : .18f;
                float gain = attenuation * attenuation * obstruction;
                voice.volume = set.VoiceVolume * gain;
                contact.volume = set.StepVolume * gain;
                presence.volume = set.VoiceVolume * .16f * gain;
            }
            bool idle = !actor.HasAggro;
            if (idle && !presence.isPlaying)
            {
                presence.clip = set.Presence;
                presence.loop = true;
                presence.Play();
                presence.time = Random.Range(0, presence.clip.length * .8f);
            }
            else if (!idle)
            {
                presence.Stop();
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
                voice.clip = set.Grasp;
                voice.pitch = Random.Range(.96f, 1.03f);
                voice.Play();
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
            presence.Stop();
            voice.pitch = 1;
            voice.clip = actor.Presentation.Alarm;
            voice.Play();
            AlarmsPlayed++;
        }

        private void OnHit()
        {
            if (Time.time < nextHit)
            {
                return;
            }
            nextHit = Time.time + .12f;
            voice.pitch = Random.Range(.96f, 1.04f);
            voice.clip = actor.Presentation.Hit;
            voice.Play();
            HitsPlayed++;
        }

        private void Stop()
        {
            voice.Stop();
            contact.Stop();
            presence.Stop();
        }
    }
}
