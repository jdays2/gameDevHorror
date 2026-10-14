using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Простой светобоязненный враг: подкрадывание, сжатие, отход и пауза.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class ShadowStalker2D : MonoBehaviour
    {
        public enum BehaviourState { Sneaking, Recoiling, Retreating, Waiting, Attacking, Dead }

        [SerializeField] private Transform player;
        [SerializeField] private Flashlight2D flashlight;
        [SerializeField] private LayerMask blockingGeometry;
        [SerializeField, Min(0.1f)] private float creepSpeed = 1.1f;
        [SerializeField, Min(0.1f)] private float retreatSpeed = 2.4f;
        [SerializeField, Min(0.1f)] private float noticeDistance = 7f;
        [SerializeField, Min(0.1f)] private float standOffDistance = 1.05f;
        [SerializeField, Min(0.1f)] private float recoilSeconds = 0.35f;
        [SerializeField, Min(0.1f)] private float retreatSeconds = 1.05f;
        [SerializeField, Min(0.1f)] private float cooldownSeconds = 3f;
        [Header("Light response")]
        [SerializeField, Min(0.1f)] private float lightFearDistance = 2.4f;
        [SerializeField, Min(0.05f)] private float reactionDelay = 0.45f;
        [Header("Attack")]
        [SerializeField] private bool attacksEnabled = true;
        [SerializeField, Min(0.1f)] private float attackDistance = 1.28f;
        [SerializeField, Min(0.1f)] private float attackDuration = 0.8f;
        [SerializeField, Min(0)] private float attackCooldown = 1.2f;
        [SerializeField, Min(1)] private int maximumHealth = 2;

        private Rigidbody2D body;
        private CircleCollider2D bodyCollider;
        private static readonly float[] AvoidanceAngles = { 45f, -45f, 90f, -90f };
        private float stateTime;
        private Vector2 escapeDirection;
        private float exposureTime;
        private Vector2 approachBeforeLight;
        private PlayerLife2D playerLife;
        private GoreBurst2D gore;
        private bool strikeApplied;
        private float attackReadyAt;
        public BehaviourState State { get; private set; }
        public float Closure { get; private set; }
        public bool Exposed { get; private set; }
        public float MovementSpeed => body != null ? body.linearVelocity.magnitude : 0;
        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;
        public Transform Player => player;
        public Flashlight2D LightSource => flashlight;
        public LayerMask BlockingGeometry => blockingGeometry;
        public float LightFearDistance => lightFearDistance;
        public float ReactionDelay => reactionDelay;
        public float LightReactionProgress => Mathf.Clamp01(exposureTime / reactionDelay);
        public float AttackProgress => State == BehaviourState.Attacking ? Mathf.Clamp01(stateTime / attackDuration) : 0;
        public float DeathTime => State == BehaviourState.Dead ? stateTime : 0;
        public int Health { get; private set; }
        public bool AttacksEnabled { get => attacksEnabled; set => attacksEnabled = value; }

        public void Configure(Transform target, Flashlight2D lamp, LayerMask obstacles)
        { player = target; flashlight = lamp; blockingGeometry = obstacles; playerLife = target != null ? target.GetComponent<PlayerLife2D>() : null; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>(); bodyCollider = GetComponent<CircleCollider2D>();
            playerLife = player != null ? player.GetComponent<PlayerLife2D>() : null;
            gore = GetComponent<GoreBurst2D>(); State = BehaviourState.Sneaking; Health = maximumHealth;
        }

        private void FixedUpdate()
        {
            stateTime += Time.fixedDeltaTime;
            if (State == BehaviourState.Dead) { body.linearVelocity = Vector2.zero; return; }
            if (player == null || flashlight == null) { body.linearVelocity = Vector2.zero; return; }
            Exposed = flashlight.Illuminates(body.position, blockingGeometry);
            bool closeLight = Exposed && Vector2.Distance(body.position, flashlight.Origin) <= lightFearDistance;
            if ((State == BehaviourState.Sneaking || State == BehaviourState.Attacking) && closeLight)
            {
                if (exposureTime == 0)
                    approachBeforeLight = body.linearVelocity.sqrMagnitude > 0.01f
                        ? body.linearVelocity.normalized : ((Vector2)player.position - body.position).normalized;
                exposureTime += Time.fixedDeltaTime;
                if (exposureTime >= reactionDelay)
                {
                    escapeDirection = (body.position - flashlight.Origin).normalized;
                    if (escapeDirection.sqrMagnitude < 0.001f) escapeDirection = -flashlight.Direction;
                    Enter(BehaviourState.Recoiling);
                }
            }
            else exposureTime = 0;

            Vector2 velocity = Vector2.zero;
            switch (State)
            {
                case BehaviourState.Sneaking:
                    Closure = Mathf.MoveTowards(Closure, 0, Time.fixedDeltaTime * 3);
                    if (closeLight)
                    {
                        // Пока свет лишь замечен, продолжаем прежнее движение без мгновенного разворота.
                        velocity = FindClearDirection(approachBeforeLight) * creepSpeed;
                        break;
                    }
                    Vector2 toPlayer = (Vector2)player.position - body.position;
                    bool canTrack = toPlayer.magnitude < noticeDistance &&
                        (playerLife == null || !playerLife.IsDead) &&
                        Physics2D.Linecast(body.position, player.position, blockingGeometry).collider == null;
                    if (canTrack)
                    {
                        if (attacksEnabled && playerLife != null && toPlayer.magnitude <= attackDistance && Time.time >= attackReadyAt && Closure < 0.1f)
                        {
                            Enter(BehaviourState.Attacking);strikeApplied = false;escapeDirection = toPlayer.normalized;
                            break;
                        }
                        // Точка за спиной определяется направлением фонаря, а не формой капсулы.
                        Vector2 behindPlayer = (Vector2)player.position - flashlight.Direction * standOffDistance;
                        Vector2 toBehind = behindPlayer - body.position;
                        if (toBehind.magnitude > 0.15f && toPlayer.magnitude > standOffDistance * 0.8f)
                            velocity = FindClearDirection(toBehind.normalized) * creepSpeed;
                    }
                    break;
                case BehaviourState.Recoiling:
                    Closure = Mathf.Clamp01(stateTime / recoilSeconds);
                    if (stateTime >= recoilSeconds) Enter(BehaviourState.Retreating);
                    break;
                case BehaviourState.Retreating:
                    Closure = 1;
                    velocity = FindClearDirection(escapeDirection) * retreatSpeed;
                    if (stateTime >= retreatSeconds) { Enter(BehaviourState.Waiting); velocity = Vector2.zero; }
                    break;
                case BehaviourState.Waiting:
                    Closure = 1;
                    if (stateTime >= cooldownSeconds && !Exposed) Enter(BehaviourState.Sneaking);
                    break;
                case BehaviourState.Attacking:
                    Closure = 0;
                    // Подготовка, короткий бросок, укус и восстановление — один завершённый цикл.
                    if (AttackProgress >= 0.25f && AttackProgress < 0.48f)
                        velocity = FindClearDirection(escapeDirection) * 2.7f;
                    if (!strikeApplied && AttackProgress >= 0.42f)
                    {
                        strikeApplied = true;
                        if (playerLife != null && !playerLife.IsDead && Vector2.Distance(body.position,player.position) < 1.4f &&
                            Physics2D.Linecast(body.position,player.position,blockingGeometry).collider == null)
                            playerLife.TakeDamage(1);
                    }
                    if (AttackProgress >= 1)
                    { attackReadyAt = Time.time + attackCooldown;Enter(BehaviourState.Sneaking);velocity = Vector2.zero; }
                    break;
            }
            body.linearVelocity = velocity;
        }

        private Vector2 FindClearDirection(Vector2 preferred)
        {
            float probe = bodyCollider.radius * 0.9f;
            if (Physics2D.CircleCast(body.position, probe, preferred, 0.3f, blockingGeometry).collider == null)
                return preferred;
            foreach (float angle in AvoidanceAngles)
            {
                Vector2 alternative = Quaternion.Euler(0, 0, angle) * preferred;
                if (Physics2D.CircleCast(body.position, probe, alternative, 0.3f, blockingGeometry).collider == null)
                    return alternative;
            }
            return Vector2.zero;
        }

        private void Enter(BehaviourState state) { State = state; stateTime = 0; }
        public void TakeDamage(int amount)
        {
            if (State == BehaviourState.Dead || amount <= 0) return;
            Health = Mathf.Max(0,Health-amount);
            if (Health == 0) Kill();
        }

        [ContextMenu("Preview/Kill creature (Play only)")]
        public void Kill()
        {
            if (!Application.isPlaying || State == BehaviourState.Dead) return;
            Health = 0;Closure = 1;Enter(BehaviourState.Dead);
            body.linearVelocity = Vector2.zero;body.simulated = false;bodyCollider.enabled = false;
            if (gore != null) gore.Play(blockingGeometry);
        }
        private void OnDisable() { if (body != null) body.linearVelocity = Vector2.zero; }
    }
}
