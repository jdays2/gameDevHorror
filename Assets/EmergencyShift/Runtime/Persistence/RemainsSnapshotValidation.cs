using System;

namespace EmergencyShift
{
    /// <summary>Validates all settled remains values before any live object is created or changed.</summary>
    public static class RemainsSnapshotValidation
    {
        public static void Validate(RemainsSnapshot saved, int fragmentCount, int poolRendererCount)
        {
            if (saved == null || saved.version < 0 || saved.version > RemainsSnapshot.CurrentVersion ||
                !CheckpointValueValidation.IsFinite(saved.position) || saved.parts == null)
            {
                throw new InvalidOperationException("Invalid remains snapshot");
            }

            if (saved.hasPool)
            {
                if (poolRendererCount < 1 || saved.pool == null || saved.pool.Length != poolRendererCount)
                {
                    throw new InvalidOperationException("Blood prefab layout differs from checkpoint");
                }

                foreach (RemainsSpriteState sprite in saved.pool)
                {
                    ValidateSprite(sprite);
                }
            }

            foreach (RemainsPartState part in saved.parts)
            {
                if (part == null || part.spriteIndex < 0 || part.spriteIndex >= fragmentCount ||
                    !CheckpointValueValidation.IsFinite(part.position))
                {
                    throw new InvalidOperationException("Invalid saved fragment");
                }

                ValidateSprite(part.body);
                if (!float.IsFinite(part.eyeDamage) || part.eyeDamage < 0 || part.eyeDamage > 1 ||
                    (!part.hasEyeSurface && part.eyeDamage != 0) || (saved.version == 0 && part.hasEyeSurface))
                {
                    throw new InvalidOperationException("Invalid saved ocular surface or unsupported legacy eye payload");
                }
                if (part.hasShadow)
                {
                    ValidateSprite(part.shadow);
                }
            }
        }

        private static void ValidateSprite(RemainsSpriteState sprite)
        {
            if (sprite == null || !CheckpointValueValidation.IsFinite(sprite.position) ||
                !CheckpointValueValidation.IsFinite(sprite.scale) ||
                !CheckpointValueValidation.IsUnitRotation(sprite.rotation) ||
                !CheckpointValueValidation.IsFinite(sprite.color))
            {
                throw new InvalidOperationException("Invalid saved remains sprite");
            }
        }
    }
}
