using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Settled presentation data. No AI, physics or Update after landing.</summary>
    public sealed class RemainsPartIdentity2D : MonoBehaviour
    {
        private int spriteIndex;
        private SpriteRenderer body;
        private SpriteRenderer shadow;
        private bool hasEyeSurface;
        private float eyeDamage;
        public void Configure(int index, SpriteRenderer visual, SpriteRenderer contact, bool eyeSurface = false, float damage = 0)
        {
            spriteIndex = index;
            body = visual;
            shadow = contact;
            hasEyeSurface = eyeSurface;
            eyeDamage = damage;
        }

        public RemainsPartState Capture()
        {
            var saved = new RemainsPartState
            {
                position = transform.position,
                spriteIndex = spriteIndex,
                hasEyeSurface = hasEyeSurface,
                eyeDamage = eyeDamage,
                body = new RemainsSpriteState()
            };
            saved.body.Capture(body);
            if (shadow != null)
            {
                saved.hasShadow = true;
                saved.shadow = new RemainsSpriteState();
                saved.shadow.Capture(shadow);
            }

            return saved;
        }
    }
}
