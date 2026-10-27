using UnityEngine;

namespace EmergencyShift.Features.Scorpion.View
{
    [DefaultExecutionOrder(15)]
    public sealed class ScorpionSpriteAnimator2D : MonoBehaviour
    {
        [SerializeField] private Scorpion2D actor;
        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer eyes;
        private bool deadStyled;
        private void LateUpdate()
        {
            var set = actor.Presentation;
            if (!actor.IsAlive)
            {
                if (!deadStyled)
                {
                    RemainsVisualStyle.Apply(visual);
                    eyes.enabled = false;
                    deadStyled = true;
                }
                return;
            }
            Face(actor.Facing);
            eyes.enabled = actor.HasTargetLineOfSight;
            if (actor.IsAttacking)
            {
                body.sprite = set.Attack.FrameAt(actor.AttackProgress * set.Attack.Duration);
                eyes.sprite = set.AttackEyes.FrameAt(actor.AttackProgress * set.AttackEyes.Duration);
            }
            else if (actor.HasAggro && (actor.Velocity.sqrMagnitude > .0004f || actor.IsGathering))
            {
                float age = actor.GaitPhase * set.Run.Duration;
                body.sprite = set.Run.FrameAt(age);
                eyes.sprite = set.RunEyes.FrameAt(age);
            }
            else if (!actor.HasAggro)
            {
                body.sprite = set.Idle;
                eyes.sprite = set.IdleEyes;
            }
            else
            {
                body.sprite = set.Run.FrameAt(actor.GaitPhase * set.Run.Duration);
                eyes.sprite = set.RunEyes.FrameAt(actor.GaitPhase * set.RunEyes.Duration);
            }
        }

        private void Face(Vector2 direction)
        {
            if (direction.sqrMagnitude < .001f)
            {
                return;
            }
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90;
            visual.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
