using System;
using UnityEngine;

namespace EmergencyShift.Features.DamageFeedback.Config
{
    [CreateAssetMenu(menuName = "Emergency Shift/UI/Directional damage style")]
    public sealed class DirectionalDamageStyle : ScriptableObject
    {
        [SerializeField]
        private Material material;
        [SerializeField]
        private Color bloodColor = new Color(.8f, .025f, .04f, 1);
        [SerializeField, Min(0)]
        private float holdSeconds = .28f;
        [SerializeField, Min(.01f)]
        private float fadeSeconds = 1.8f;
        [SerializeField, Range(.05f, 1)]
        private float opacity = .9f;
        [SerializeField, Range(.03f, .25f), Tooltip("Inward reach relative to the shorter screen dimension.")]
        private float depth = .19f;
        [SerializeField, Range(.1f, .8f), Tooltip("Width along the edge relative to the shorter screen dimension.")]
        private float spread = .62f;
        [SerializeField, Range(0, .25f), Tooltip("Maximum camera translation in world units. Zero disables the kick.")]
        private float cameraDistance = .08f;
        [SerializeField, Range(.01f, 1), Tooltip("One short directional kick and return, in real seconds.")]
        private float cameraSeconds = .22f;

        public Material Material => material;
        public Color BloodColor => bloodColor;
        public float Depth => depth;
        public float Spread => spread;

        public DamageFeedbackSettings CreateSettings()
        {
            if (float.IsNaN(depth) || float.IsInfinity(depth) || depth < .03f || depth > .25f ||
                float.IsNaN(spread) || float.IsInfinity(spread) || spread < .1f || spread > .8f)
            {
                throw new InvalidOperationException("Directional damage depth/spread are outside their authored ranges.");
            }

            return new DamageFeedbackSettings(holdSeconds, fadeSeconds, opacity, cameraDistance, cameraSeconds);
        }
    }
}
