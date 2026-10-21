using System;
using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Gameplay HUD style")]
    public sealed class GameplayHudStyle : ScriptableObject
    {
        [SerializeField]
        private Color healthy = new Color(.45f, .58f, .4f);
        [SerializeField]
        private Color wounded = new Color(.74f, .63f, .43f);
        [SerializeField]
        private Color critical = new Color(.77f, .41f, .35f);
        [SerializeField]
        private Color lostHealth = new Color(.87f, .69f, .63f);
        [SerializeField, Range(0, 1)]
        private float woundedFraction = .6f;
        [SerializeField, Range(0, 1)]
        private float criticalFraction = .35f;
        [SerializeField, Min(0)]
        private float trailDelay = .18f;
        [SerializeField, Min(.01f)]
        private float trailDuration = .5f;
        [SerializeField, Min(.01f)]
        private float hitDuration = .65f;
        [SerializeField, Range(0, 1)]
        private float spareOpacity = .42f;
        [SerializeField]
        private AudioClip[] hitSounds = Array.Empty<AudioClip>();
        [SerializeField, Range(0, 1)]
        private float hitVolume = .12f;

        public Color Healthy => healthy;
        public Color Wounded => wounded;
        public Color Critical => critical;
        public Color LostHealth => lostHealth;
        public float WoundedFraction => woundedFraction;
        public float CriticalFraction => criticalFraction;
        public float TrailDelay => trailDelay;
        public float TrailDuration => trailDuration;
        public float HitDuration => hitDuration;
        public float SpareOpacity => spareOpacity;
        public float HitVolume => hitVolume;
        public int HitSoundCount => hitSounds != null ? hitSounds.Length : 0;

        public AudioClip HitSoundAt(int index)
        {
            if (index < 0 || index >= HitSoundCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return hitSounds[index];
        }

        public void Validate()
        {
            if (!ValidColor(healthy) || !ValidColor(wounded) || !ValidColor(critical) || !ValidColor(lostHealth) ||
                !Unit(woundedFraction) || !Unit(criticalFraction) || criticalFraction > woundedFraction ||
                !float.IsFinite(trailDelay) || trailDelay < 0 ||
                !float.IsFinite(trailDuration) || trailDuration < .01f ||
                !float.IsFinite(hitDuration) || hitDuration < .01f ||
                !Unit(spareOpacity) || !Unit(hitVolume))
            {
                throw new InvalidOperationException("Invalid gameplay HUD style: " + name);
            }
        }

        private static bool Unit(float value) => float.IsFinite(value) && value >= 0 && value <= 1;
        private static bool ValidColor(Color value) => float.IsFinite(value.r) && float.IsFinite(value.g)
            && float.IsFinite(value.b) && Unit(value.a);
    }
}
