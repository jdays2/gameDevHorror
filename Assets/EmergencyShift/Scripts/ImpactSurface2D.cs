using UnityEngine;

namespace EmergencyShift
{
    public enum ImpactMaterial { Concrete, Wood, Flesh }

    /// <summary>Optional collider/parent override; untagged scenery is concrete, Stalkers are flesh.</summary>
    [DisallowMultipleComponent]
    public sealed class ImpactSurface2D : MonoBehaviour
    {
        [SerializeField] private ImpactMaterial material = ImpactMaterial.Concrete;
        public ImpactMaterial Material => material;

        public static ImpactMaterial Resolve(Collider2D target)
        {
            if (target == null) return ImpactMaterial.Concrete;
            var surface = target.GetComponentInParent<ImpactSurface2D>();
            if (surface != null) return surface.Material;
            return target.GetComponentInParent<ShadowStalker2D>() != null ? ImpactMaterial.Flesh : ImpactMaterial.Concrete;
        }
    }
}
