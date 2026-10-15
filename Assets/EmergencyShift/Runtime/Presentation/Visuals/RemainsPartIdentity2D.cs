using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Settled presentation data. No AI, physics or Update after landing.</summary>
    public sealed class RemainsPartIdentity2D : MonoBehaviour
    {
        private int spriteIndex;
        private SpriteRenderer body;
        private SpriteRenderer shadow;
        public void Configure(int index, SpriteRenderer visual, SpriteRenderer contact)
        {
            spriteIndex = index;
            body = visual;
            shadow = contact;
        }

        public RemainsPartState Capture()
        {
            var saved = new RemainsPartState
            {
                position = transform.position,
                spriteIndex = spriteIndex,
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
