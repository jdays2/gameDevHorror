using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Перелистывает спрайты, поворачивает изображение и ведёт отдельный зрачок.</summary>
    public sealed class StalkerSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private ShadowStalker2D behaviour;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer iris;
        [SerializeField] private SpriteRenderer pupil;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] recoilFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private Sprite[] deathFrames;
        [SerializeField, Min(1)] private float walkFramesPerSecond = 7f;
        [SerializeField, Min(0.001f)] private float pupilTravel = 0.028f;
        [SerializeField, Min(1)] private float turnSpeed = 160f;
        private float walkClock;
        private float compactClock;
        private bool deathStyleApplied;
        public int CurrentFrame { get; private set; }
        public bool EyeOpen => pupil != null && pupil.enabled;
        public Vector2 PupilOffset => pupil != null ? (Vector2)pupil.transform.localPosition : Vector2.zero;

        public void Configure(ShadowStalker2D actor, Transform root, SpriteRenderer renderer,
            SpriteRenderer irisRenderer, SpriteRenderer pupilRenderer, Sprite[] walk, Sprite[] recoil)
        {
            behaviour = actor; visualRoot = root; bodyRenderer = renderer; iris = irisRenderer;
            pupil = pupilRenderer; walkFrames = walk; recoilFrames = recoil;
        }
        public void ConfigureCombat(Sprite[] attack,Sprite[] death){attackFrames=attack;deathFrames=death;}

        private void LateUpdate()
        {
            if (behaviour == null || visualRoot == null || bodyRenderer == null || walkFrames == null || walkFrames.Length == 0) return;
            bool creeping = behaviour.State == ShadowStalker2D.BehaviourState.Sneaking||behaviour.State==ShadowStalker2D.BehaviourState.Returning;
            bool attacking = behaviour.State == ShadowStalker2D.BehaviourState.Attacking;
            bool dead = behaviour.State == ShadowStalker2D.BehaviourState.Dead;
            if (dead && !deathStyleApplied)
            {
                RemainsVisualStyle.Apply(visualRoot);
                deathStyleApplied = true;
            }
            if (creeping && behaviour.Velocity.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(behaviour.Velocity.y, behaviour.Velocity.x) * Mathf.Rad2Deg - 90;
                visualRoot.rotation = Quaternion.RotateTowards(visualRoot.rotation, Quaternion.Euler(0, 0, angle), turnSpeed * Time.deltaTime);
                walkClock += Time.deltaTime * Mathf.Clamp(behaviour.MovementSpeed / 1.1f, 0.35f, 1.5f);
            }
            if (attacking && behaviour.Player != null)
            {
                Vector2 target = (Vector2)behaviour.Player.position - (Vector2)transform.position;
                float angle = Mathf.Atan2(target.y,target.x)*Mathf.Rad2Deg-90;
                visualRoot.rotation=Quaternion.RotateTowards(visualRoot.rotation,Quaternion.Euler(0,0,angle),turnSpeed*Time.deltaTime);
            }
            if (dead && deathFrames != null && deathFrames.Length > 0)
            {
                CurrentFrame = Mathf.Min(deathFrames.Length-1,(int)(behaviour.DeathTime*6f));
                bodyRenderer.sprite = deathFrames[CurrentFrame];
            }
            else if (attacking && attackFrames != null && attackFrames.Length > 0)
            {
                CurrentFrame = Mathf.Min(attackFrames.Length-1,(int)(behaviour.AttackProgress*attackFrames.Length));
                bodyRenderer.sprite = attackFrames[CurrentFrame];
            }
            else if (behaviour.Closure < 0.05f || recoilFrames == null || recoilFrames.Length == 0)
            {
                CurrentFrame = (int)(walkClock * walkFramesPerSecond) % walkFrames.Length;
                bodyRenderer.sprite = walkFrames[CurrentFrame];
            }
            else
            {
                CurrentFrame = Mathf.Clamp(Mathf.RoundToInt(behaviour.Closure * (recoilFrames.Length - 1)), 0, recoilFrames.Length - 1);
                if (behaviour.State == ShadowStalker2D.BehaviourState.Retreating)
                {
                    compactClock += Time.deltaTime;
                    CurrentFrame = recoilFrames.Length - 1 - ((int)(compactClock * 5) % 2);
                }
                bodyRenderer.sprite = recoilFrames[CurrentFrame];
            }

            bool open = !dead && behaviour.Closure < 0.166f;
            if (iris != null) iris.enabled = open;
            if (pupil == null) return;
            pupil.enabled = open;
            if (!open || behaviour.Player == null) return;
            Vector2 localTarget = visualRoot.InverseTransformPoint(behaviour.Player.position);
            Vector2 offset = localTarget.normalized * pupilTravel * Mathf.Clamp01(localTarget.magnitude / 0.4f);
            Vector2 next = Vector2.MoveTowards(pupil.transform.localPosition, offset, Time.deltaTime * 0.25f);
            pupil.transform.localPosition = new Vector3(next.x, next.y, -0.002f);
        }
    }
}
