using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Gameplay HUD style")]
    public sealed class GameplayHudStyle : ScriptableObject
    {
        public Color healthy = new Color(.45f, .58f, .4f);
        public Color wounded = new Color(.74f, .63f, .43f);
        public Color critical = new Color(.77f, .41f, .35f);
        public Color lostHealth = new Color(.87f, .69f, .63f);
        [Range(0, 1)] public float woundedFraction = .6f;
        [Range(0, 1)] public float criticalFraction = .35f;
        [Min(0)] public float trailDelay = .18f;
        [Min(.01f)] public float trailDuration = .5f;
        [Min(.01f)] public float hitDuration = .65f;
        [Range(0, 1)] public float spareOpacity = .42f;
        public AudioClip[] hitSounds;
        [Range(0, 1)] public float hitVolume = .12f;
    }
}
