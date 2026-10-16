using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Shared bounded floor-shadow tuning for low scenery; no gameplay visibility rules.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Prop shadow profile")]
    public sealed class PropShadowProfile : ScriptableObject
    {
        [Range(0, .5f)]
        public float contactOpacity = .13f;
        [Range(0, .5f)]
        public float projectionOpacity = .22f;
        [Min(0)]
        public float maximumExtension = .24f;
        [Min(0)]
        public float extensionPerDistance = .06f;
        [Min(0)]
        public float contactExpansion = .012f;
        [Min(0)]
        public float flashlightStrength = 1.5f;

        public void Validate()
        {
            if (!float.IsFinite(contactOpacity) || contactOpacity < 0 || contactOpacity > .5f
                || !float.IsFinite(projectionOpacity) || projectionOpacity < 0 || projectionOpacity > .5f
                || !float.IsFinite(maximumExtension) || maximumExtension < 0
                || !float.IsFinite(extensionPerDistance) || extensionPerDistance < 0
                || !float.IsFinite(contactExpansion) || contactExpansion < 0
                || !float.IsFinite(flashlightStrength) || flashlightStrength < 0)
            {
                throw new System.InvalidOperationException("Invalid prop shadow profile: " + name);
            }
        }
    }
}
