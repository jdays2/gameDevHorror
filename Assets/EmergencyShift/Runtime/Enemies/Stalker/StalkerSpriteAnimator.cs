using UnityEngine;
using EmergencyShift.Features.Enemies.Config;

namespace EmergencyShift
{
    /// <summary>Displays complete baked body frames; eye injury and combat timing remain shared presentation.</summary>
    public sealed class StalkerSpriteAnimator : CreatureSpriteAnimator2D
    {
        [SerializeField] private ShadowStalker2D behaviour;
        [SerializeField] private StalkerSpriteSet spriteSet;
        private float displayedBodyFold;
        private bool hasDisplayedBodyFold;
        public float DisplayedBodyFold => displayedBodyFold;
        public StalkerSpriteSet SpriteSet => spriteSet;
        protected override bool AnimateCompactLocomotion => spriteSet != null;
        protected override bool ApplySupportYaw => spriteSet == null;
        protected override float LocomotionStrideScale => spriteSet != null ? spriteSet.StrideScale(displayedBodyFold) : 1;
        protected override float BodyFoldTarget => spriteSet != null && Presentation.BodyPosture == EnemyBodyPosture.CompactWalking
            ? spriteSet.CompactWalkingFold : base.BodyFoldTarget;

        protected override void Awake()
        {
            base.Awake();
            spriteSet?.Validate();
        }

        protected override bool TrySelectLocomotionFrame(float phase, out Sprite sprite, out int frame)
        {
            if (spriteSet == null)
            {
                return base.TrySelectLocomotionFrame(phase, out sprite, out frame);
            }
            float bodyTarget = BodyFoldTarget;
            if (!hasDisplayedBodyFold)
            {
                displayedBodyFold = bodyTarget;
                hasDisplayedBodyFold = true;
            }
            displayedBodyFold = Mathf.MoveTowards(displayedBodyFold, bodyTarget, Time.deltaTime / spriteSet.ClosureSeconds);
            frame = spriteSet.FrameIndex(phase, displayedBodyFold);
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
