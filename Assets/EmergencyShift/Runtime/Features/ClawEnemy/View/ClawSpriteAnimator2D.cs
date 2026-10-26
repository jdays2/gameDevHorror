using UnityEngine;
using EmergencyShift.Features.ClawEnemy.Config;

namespace EmergencyShift.Features.ClawEnemy.View
{
    public sealed class ClawSpriteAnimator2D : MonoBehaviour
    {
        [SerializeField] private ClawEnemy2D actor;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private ClawMotionSet motions;
        private TimedSpriteMotion current;
        private float age;
        private bool deadStyleApplied;

        public SpriteRenderer Body => body;

        private void Awake() => motions.Validate();

        private void LateUpdate()
        {
            if (!actor.IsAlive)
            {
                if (!deadStyleApplied)
                {
                    RemainsVisualStyle.Apply(visualRoot);
                    deadStyleApplied = true;
                }
                return;
            }
            if (actor.IsAttacking)
            {
                current = motions.Attack;
                body.sprite = current.FrameAt(actor.AttackProgress * current.Duration);
                Face(actor.AttackHeading);
                return;
            }
            float speed = actor.Velocity.magnitude;
            if (speed < .02f || actor.IsStaggered)
            {
                if (!actor.IsStaggered)
                {
                    body.sprite = motions.Walk.FrameAt(0);
                }
                age = 0;
                current = null;
                return;
            }
            TimedSpriteMotion selected = actor.HasAggro ? motions.Run : motions.Walk;
            if (current != selected)
            {
                current = selected;
                age = 0;
            }
            age += Time.deltaTime * speed / current.ReferenceSpeed;
            body.sprite = current.FrameAt(age);
            Face(actor.Velocity);
        }

        private void Face(Vector2 direction)
        {
            if (direction.sqrMagnitude < .001f)
            {
                return;
            }
            // Source art faces left. The original camera/pivot never changes.
            float heading = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180;
            visualRoot.rotation = Quaternion.RotateTowards(visualRoot.rotation,
                Quaternion.Euler(0, 0, heading), motions.TurnSpeed * Time.deltaTime);
        }
    }
}
