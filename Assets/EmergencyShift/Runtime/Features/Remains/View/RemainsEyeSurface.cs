using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Frozen ocular surface of a detached organ. No ticking or living-eye tracking.</summary>
    public static class RemainsEyeSurface
    {
        private static readonly int burnId = Shader.PropertyToID("_BurnAmount");
        private static readonly int frameId = Shader.PropertyToID("_EyeFrame");
        private static readonly int warningId = Shader.PropertyToID("_WarningAmount");
        private static readonly int pupilId = Shader.PropertyToID("_PupilLayer");

        public static void Apply(SpriteRenderer renderer, Material material, float damage)
        {
            if (material == null)
            {
                throw new System.InvalidOperationException("Remains eye requires its configured surface material.");
            }

            renderer.sharedMaterial = material;
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetFloat(burnId, damage);
            properties.SetFloat(frameId, 0);
            properties.SetFloat(warningId, 0);
            properties.SetFloat(pupilId, 0);
            renderer.SetPropertyBlock(properties);
        }
    }
}
