using UnityEngine;
using EmergencyShift.Features.Enemies.Config;

namespace EmergencyShift
{
    /// <summary>Displays complete baked body frames; eye injury and combat timing remain shared presentation.</summary>
    public sealed class StalkerSpriteAnimator : CreatureSpriteAnimator2D
    {
        [SerializeField] private ShadowStalker2D behaviour;
        [SerializeField] private StalkerSpriteSet spriteSet;
        [SerializeField] private CreatureAttackSpriteSet attackSpriteSet;
        private readonly CreatureAttackSpritePlayback attackPlayback = new CreatureAttackSpritePlayback();
        private float displayedBodyFold;
        private bool hasDisplayedBodyFold;
        public float DisplayedBodyFold => displayedBodyFold;
        public StalkerSpriteSet SpriteSet => spriteSet;
        protected override bool AnimateCompactLocomotion => spriteSet != null;
        protected override bool ApplySupportYaw => spriteSet == null;
        protected override bool HoldLastBodyOnDeath => spriteSet != null;
        protected override float LocomotionStrideScale => spriteSet != null ? spriteSet.StrideScale(displayedBodyFold) : 1;
        protected override float BodyFoldTarget => spriteSet != null && Presentation.BodyPosture == EnemyBodyPosture.CompactWalking
            ? spriteSet.CompactWalkingFold : base.BodyFoldTarget;

        protected override void Awake()
        {
            base.Awake();
            if (behaviour == null)
            {
                behaviour = GetComponent<ShadowStalker2D>();
            }
            spriteSet?.Validate();
            attackSpriteSet?.Validate();
        }

        protected override bool TrySelectAttackFrame(float phase, float progress, out Sprite sprite, out int frame)
        {
            if (attackSpriteSet == null)
            {
                return base.TrySelectAttackFrame(phase, progress, out sprite, out frame);
            }
            frame = attackSpriteSet.FrameIndex(phase, progress, behaviour.Definition.StrikeAt, behaviour.Definition.AttackDuration);
            sprite = attackSpriteSet.Frame(frame);
            attackPlayback.RecordAttackFrame(attackSpriteSet, frame);
            displayedBodyFold = 0;
            hasDisplayedBodyFold = true;
            return true;
        }

        protected override void ResetDisplayedBodyPosture()
        {
            hasDisplayedBodyFold = false;
            attackPlayback.Reset();
        }

        protected override bool TrySelectLocomotionFrame(float phase, out Sprite sprite, out int frame)
        {
            if (attackPlayback.TrySelectRetractionFrame(attackSpriteSet, phase, Time.deltaTime, out frame))
            {
                sprite = attackSpriteSet.Frame(frame);
                displayedBodyFold = 0;
                hasDisplayedBodyFold = true;
                return true;
            }
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
