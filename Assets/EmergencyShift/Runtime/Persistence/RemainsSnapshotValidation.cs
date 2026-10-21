using System;

namespace EmergencyShift
{
    /// <summary>Validates all settled remains values before any live object is created or changed.</summary>
    public static class RemainsSnapshotValidation
    {
        public static void Validate(RemainsSnapshot saved, int fragmentCount, int poolRendererCount)
        {
            if (saved == null || !CheckpointValueValidation.IsFinite(saved.position) || saved.parts == null)
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
