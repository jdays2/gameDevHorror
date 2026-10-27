using EmergencyShift.Features.Enemies.Config;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Displays baked body poses while the Sleeper owns its wake and combat timing.</summary>
    [DisallowMultipleComponent]
    public sealed class SleeperSpriteAnimator : CreatureSpriteAnimator2D
    {
        [SerializeField] private StalkerSpriteSet spriteSet;
        [SerializeField] private CreatureAttackSpriteSet attackSpriteSet;
        private readonly CreatureAttackSpritePlayback attackPlayback = new CreatureAttackSpritePlayback();
        private Sleeper2D behaviour;
        private bool hasDisplayedBodyFold;

        public StalkerSpriteSet SpriteSet => spriteSet;
        public float DisplayedBodyFold { get; private set; }

        protected override bool ApplySupportYaw => spriteSet == null;
        protected override bool HoldLastBodyOnDeath => spriteSet != null;

        protected override void Awake()
        {
            base.Awake();
            behaviour = GetComponent<Sleeper2D>();
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
            DisplayedBodyFold = 0;
            hasDisplayedBodyFold = true;
            return true;
        }

        protected override bool TrySelectLocomotionFrame(float phase, out Sprite sprite, out int frame)
        {
            if (attackPlayback.TrySelectRetractionFrame(attackSpriteSet, phase, Time.deltaTime, out frame))
            {
                sprite = attackSpriteSet.Frame(frame);
                DisplayedBodyFold = 0;
                hasDisplayedBodyFold = true;
                return true;
            }
            if (spriteSet == null)
            {
                return base.TrySelectLocomotionFrame(phase, out sprite, out frame);
            }

            float target = BodyFoldTarget;
            // Follow the brain's .9 second waking timeline directly. If another state
            // cuts that timeline short, finish the remaining body movement through rows.
            if (!hasDisplayedBodyFold || Presentation.BodyPosture == EnemyBodyPosture.Unfolding)
            {
                DisplayedBodyFold = target;
            }
            else
            {
                DisplayedBodyFold = Mathf.MoveTowards(DisplayedBodyFold, target, Time.deltaTime / spriteSet.ClosureSeconds);
            }
            hasDisplayedBodyFold = true;
            frame = spriteSet.FrameIndex(phase, DisplayedBodyFold);
            sprite = spriteSet.Frame(frame);
            return true;
        }

        protected override Vector2 EyePoseOffset(int frame)
        {
            return spriteSet != null ? Vector2.zero : base.EyePoseOffset(frame);
        }

        protected override void ResetDisplayedBodyPosture()
        {
            attackPlayback.Reset();
            hasDisplayedBodyFold = false;
        }
    }
}
