using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Общие параметры одного визуального варианта. Не хранит патроны/состояние игрока.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Shot visual profile")]
    public sealed class ShotVisualProfile : ScriptableObject
    {
        public string displayName = "Тёплая вспышка";
        public Color flashColor = new Color(1, 0.72f, 0.38f);
        public Color bulletColor = new Color(1, 0.88f, 0.62f);
        [Min(0.02f)] public float flashSeconds = 0.08f;
        [Min(0.1f)] public float flashRadius = 1.9f;
        [Min(0)] public float flashIntensity = 2f;
        [Min(0.02f)] public float impactSeconds = 0.12f;
        [Min(1)] public float bulletSpeed = 36f;
        [Min(0.02f)] public float trailLength = 0.42f;
        [Min(0.01f)] public float trailWidth = 0.025f;
        [Min(0)] public float bulletLightRadius;
        [Min(0)] public float bulletLightIntensity;
    }
}
