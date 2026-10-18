using UnityEngine;

namespace EmergencyShift
{
    public enum EnvironmentHeightClass
    {
        Low,
        Furniture,
        Tall
    }

    /// <summary>Authoring height of a static surface. The Editor bakes URP receiver/caster layers.</summary>
    [DisallowMultipleComponent]
    public sealed class EnvironmentHeight2D : MonoBehaviour
    {
        [Tooltip("Low: bin/chair. Furniture: worktop/drum. Tall: cupboard/wall. Shadows reach the same class and lower surfaces.")]
        public EnvironmentHeightClass heightClass = EnvironmentHeightClass.Furniture;
    }
}
