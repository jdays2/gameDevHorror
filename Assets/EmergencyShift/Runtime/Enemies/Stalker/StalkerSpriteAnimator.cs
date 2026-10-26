using UnityEngine;
using EmergencyShift.Features.Enemies.View;

namespace EmergencyShift
{
    /// <summary>Preserves existing prefab references and authoring API; rendering is shared with the Sleeper.</summary>
    public sealed class StalkerSpriteAnimator : CreatureSpriteAnimator2D
    {
        [SerializeField] private ShadowStalker2D behaviour;
        private StalkerLimbRig2D limbRig;
        protected override bool AnimateCompactLocomotion => limbRig != null;
        protected override bool ApplySupportYaw => limbRig == null;

        protected override void Awake()
        {
            base.Awake();
            limbRig = GetComponent<StalkerLimbRig2D>();
        }

        protected override void LateUpdate()
        {
            limbRig?.RemoveEyeOffset();
            base.LateUpdate();
        }

        public void Configure(ShadowStalker2D actor, Transform root, SpriteRenderer renderer,
            SpriteRenderer irisRenderer, SpriteRenderer pupilRenderer, Sprite[] walk, Sprite[] recoil)
        {
            behaviour = actor;
            base.Configure(actor.GetComponent<StalkerPresentationSource2D>(), root, renderer, irisRenderer, pupilRenderer, walk, recoil);
        }
    }
}
