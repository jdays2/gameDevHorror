using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Простой светобоязненный враг: подкрадывание, сжатие, отход и пауза.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class ShadowStalker2D : MonoBehaviour
    {
        // Returning зарезервирован для совместимости старых данных; возврата домой больше нет.
        public enum BehaviourState { Sneaking, Recoiling, Retreating, Waiting, Attacking, Dead, Dormant, Returning, Patrolling, TargetDead }

        [SerializeField] private Transform player;
        [SerializeField] private Flashlight2D flashlight;
        [SerializeField] private LayerMask blockingGeometry;
        [SerializeField, Min(0.1f), Tooltip("Скорость постоянного преследования после первого обнаружения.")] private float creepSpeed = 1.5f;
        [SerializeField, Min(0.1f)] private float retreatSpeed = 3.8f;
        [SerializeField, Min(0.1f)] private float standOffDistance = 1.05f;
        [SerializeField, Min(0.1f)] private float recoilSeconds = 0.35f;
        [SerializeField, Min(0.1f)] private float retreatSeconds = 1.05f;
        [SerializeField, Min(0.1f)] private float cooldownSeconds = 3f;
        [Header("Light response")]
        [SerializeField, Min(0.1f)] private float lightFearDistance = 3.2f;
        [SerializeField, Min(0.05f)] private float reactionDelay = 0.45f;
        [Header("Attack")]
        [SerializeField] private bool attacksEnabled = true;
        [SerializeField, Min(0.1f)] private float attackDistance = 1.28f;
        [SerializeField, Min(0.1f)] private float attackDuration = 0.8f;
        [SerializeField, Min(0)] private float attackCooldown = 1.2f;
        [SerializeField, Min(1)] private int maximumHealth = 2;
        [Header("First detection — proximity and line of sight")]
        [SerializeField, Min(.5f), InspectorName("Detection Radius")]
        private float habitatRadius = 3.2f;
        [Header("Bullet hit reaction")]
        [SerializeField, Min(0)] private float hitStaggerSeconds = .09f;
        [SerializeField, Min(0)] private float hitPushSpeed = 1.6f;

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
        private Vector2 home;
        private float staggerUntil;
        private Vector2 hitPush;
        private StalkerHitResponse2D hitResponse;
        private StalkerPatrol2D patrol;
        private StalkerChasePath2D chasePath;
        public bool HasAggro { get; private set; }
        public float PursuitSpeed => creepSpeed;
        public float RetreatSpeed => retreatSpeed;
        public int ChaseWaypointCount => chasePath != null ? chasePath.WaypointCount : 0;
        public Vector2 Home => home;
        public float HabitatRadius => habitatRadius;
        public bool IsStaggered => State!=BehaviourState.Dead && Time.time<staggerUntil;
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
            gore = GetComponent<GoreBurst2D>();hitResponse=GetComponent<StalkerHitResponse2D>();
            home=body.position;State = BehaviourState.Dormant; Health = maximumHealth;
            patrol=GetComponent<StalkerPatrol2D>();
            chasePath=new StalkerChasePath2D();
            if(patrol!=null)patrol.Initialize(home);
        }

        private void FixedUpdate()
        {
            stateTime += Time.fixedDeltaTime;
            if (State == BehaviourState.Dead) { body.linearVelocity = Vector2.zero; return; }
            if (player == null || flashlight == null) { body.linearVelocity = Vector2.zero; return; }
            bool aliveTarget=playerLife==null||!playerLife.IsDead;
            if(!aliveTarget)
            {
                if(patrol!=null)patrol.ResetRoute();
                Exposed=false;exposureTime=0;Enter(BehaviourState.TargetDead);body.linearVelocity=Vector2.zero;return;
            }
            bool lineOfSight=aliveTarget&&Physics2D.Linecast(body.position,player.position,blockingGeometry).collider==null;
            if(!HasAggro&&Vector2.Distance(body.position,player.position)<=habitatRadius&&lineOfSight)ActivateAggro();
            if(IsStaggered){body.linearVelocity=hitPush*Mathf.Clamp01((staggerUntil-Time.time)/Mathf.Max(.01f,hitStaggerSeconds));return;}
            Exposed = flashlight.Illuminates(body.position, blockingGeometry);
            bool closeLight = Exposed && Vector2.Distance(body.position, flashlight.Origin) <= lightFearDistance;
            bool idle=State==BehaviourState.Dormant||State==BehaviourState.Patrolling;
            if ((idle || State == BehaviourState.Sneaking || State == BehaviourState.Attacking) && closeLight)
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
                case BehaviourState.Dormant:
                case BehaviourState.Patrolling:
                    Closure=Mathf.MoveTowards(Closure,0,Time.fixedDeltaTime*3);
                    if(patrol!=null&&patrol.PatrolEnabled)
                    {
                        if(State!=BehaviourState.Patrolling)Enter(BehaviourState.Patrolling);
                        float radius=bodyCollider.radius*Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.y));
                        velocity=patrol.Step(body.position,radius,blockingGeometry.value|(1<<9),Time.fixedDeltaTime);
                    }
                    else if(State!=BehaviourState.Dormant){if(patrol!=null)patrol.ResetRoute();Enter(BehaviourState.Dormant);}
                    break;
                case BehaviourState.Sneaking:
                    Closure = Mathf.MoveTowards(Closure, 0, Time.fixedDeltaTime * 3);
                    if (closeLight)
                    {
                        // Пока свет лишь замечен, продолжаем прежнее движение без мгновенного разворота.
                        velocity = FindClearDirection(approachBeforeLight) * creepSpeed;
                        break;
                    }
                    Vector2 toPlayer = (Vector2)player.position - body.position;
                    if (HasAggro)
                    {
                        if (attacksEnabled && playerLife != null && lineOfSight && toPlayer.magnitude <= attackDistance && Time.time >= attackReadyAt && Closure < 0.1f)
                        {
                            Enter(BehaviourState.Attacking);strikeApplied = false;escapeDirection = toPlayer.normalized;
                            break;
                        }
                        // Точка за спиной определяется направлением фонаря, а не формой капсулы.
                        Vector2 behindPlayer = (Vector2)player.position - flashlight.Direction * standOffDistance;
                        float radius=bodyCollider.radius*Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.y));
                        int solids=blockingGeometry.value|(1<<9);
                        Vector2 goal=lineOfSight?behindPlayer:(Vector2)player.position;
                        if(lineOfSight&&(Physics2D.OverlapCircle(goal,radius,solids)!=null||Physics2D.Linecast(player.position,goal,solids).collider!=null))
                            goal=player.position;
                        Vector2 toBehind = goal - body.position;
                        if (toBehind.magnitude > 0.15f && toPlayer.magnitude > standOffDistance * 0.8f)
                        {
                            velocity = chasePath.Direction(body.position,goal,radius,solids) * creepSpeed;
                        }
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
                    if (stateTime >= cooldownSeconds && !Exposed)
                        Enter(HasAggro?BehaviourState.Sneaking:BehaviourState.Dormant);
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
            int solids=blockingGeometry.value|(1<<9); // Низкая мебель мешает движению, но не свету/взгляду.
            if (Physics2D.CircleCast(body.position, probe, preferred, 0.3f, solids).collider == null)
                return preferred;
            foreach (float angle in AvoidanceAngles)
            {
                Vector2 alternative = Quaternion.Euler(0, 0, angle) * preferred;
                if (Physics2D.CircleCast(body.position, probe, alternative, 0.3f, solids).collider == null)
                    return alternative;
            }
            return Vector2.zero;
        }

        private void Enter(BehaviourState state) { State = state; stateTime = 0; }
        private void ActivateAggro()
        {
            if(HasAggro)return;
            HasAggro=true;if(patrol!=null)patrol.ResetRoute();
            if(State==BehaviourState.Dormant||State==BehaviourState.Patrolling)Enter(BehaviourState.Sneaking);
        }
        /// <summary>Пуля задаёт направление реакции; здоровье и Dead остаются единственным источником истины.</summary>
        public void ApplyBulletHit(int amount,Vector2 point,Vector2 direction)
        {
            if(State==BehaviourState.Dead||amount<=0)return;
            TakeDamage(amount);
            if(hitResponse!=null)hitResponse.Play(point,direction,State==BehaviourState.Dead);
            if(State==BehaviourState.Dead)return;
            if(player!=null&&(playerLife==null||!playerLife.IsDead))ActivateAggro();
            staggerUntil=Time.time+hitStaggerSeconds;hitPush=direction.normalized*hitPushSpeed;
            if(State==BehaviourState.Attacking){Enter(BehaviourState.Sneaking);attackReadyAt=Time.time+.35f;}
        }
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
            if(patrol!=null)patrol.ResetRoute();
            body.linearVelocity = Vector2.zero;body.simulated = false;bodyCollider.enabled = false;
            if (gore != null) gore.Play(blockingGeometry);
        }
        private void OnDisable() { if (body != null) body.linearVelocity = Vector2.zero; }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color=new Color(.3f,.75f,.85f,.7f);Gizmos.DrawWireSphere(transform.position,habitatRadius);
        }
    }
}
