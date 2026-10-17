using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Preserves existing prefab references and authoring API; rendering is shared with the Sleeper.</summary>
    public sealed class StalkerSpriteAnimator : CreatureSpriteAnimator2D
    {
        [SerializeField] private ShadowStalker2D behaviour;

        public void Configure(ShadowStalker2D actor, Transform root, SpriteRenderer renderer,
            SpriteRenderer irisRenderer, SpriteRenderer pupilRenderer, Sprite[] walk, Sprite[] recoil)
        {
            behaviour = actor;
            base.Configure(actor.GetComponent<StalkerPresentationSource2D>(), root, renderer, irisRenderer, pupilRenderer, walk, recoil);
        }
    }
}
