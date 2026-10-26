using UnityEngine;
using EmergencyShift.Features.Enemies.Config;

namespace EmergencyShift
{
    /// <summary>Displays complete baked body frames; eye injury and combat timing remain shared presentation.</summary>
    public sealed class StalkerSpriteAnimator : CreatureSpriteAnimator2D
    {
        [SerializeField] private ShadowStalker2D behaviour;
        [SerializeField] private StalkerSpriteSet spriteSet;
        private float displayedClosure;
        private bool hasDisplayedClosure;
        public float DisplayedClosure => displayedClosure;
        public StalkerSpriteSet SpriteSet => spriteSet;
        protected override bool AnimateCompactLocomotion => spriteSet != null;
        protected override bool ApplySupportYaw => spriteSet == null;

        protected override void Awake()
        {
            base.Awake();
            spriteSet?.Validate();
        }

        protected override bool TrySelectLocomotionFrame(float phase, float closure, out Sprite sprite, out int frame)
        {
            if (spriteSet == null)
            {
                return base.TrySelectLocomotionFrame(phase, closure, out sprite, out frame);
            }
            if (!hasDisplayedClosure)
            {
                displayedClosure = closure;
                hasDisplayedClosure = true;
            }
            displayedClosure = Mathf.MoveTowards(displayedClosure, closure, Time.deltaTime / spriteSet.ClosureSeconds);
            frame = spriteSet.FrameIndex(phase, displayedClosure);
            sprite = spriteSet.Frame(frame);
            return true;
        }

        protected override Vector2 EyePoseOffset(int frame) => spriteSet != null ? Vector2.zero : base.EyePoseOffset(frame);

        public void Configure(ShadowStalker2D actor, Transform root, SpriteRenderer renderer,
            SpriteRenderer irisRenderer, SpriteRenderer pupilRenderer, Sprite[] walk, Sprite[] recoil)
        {
            behaviour = actor;
            base.Configure(actor.GetComponent<StalkerPresentationSource2D>(), root, renderer, irisRenderer, pupilRenderer, walk, recoil);
        }
    }
}
