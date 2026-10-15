using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Только мирный маршрут: ограниченная область, случайные достижимые точки и паузы.</summary>
    [DisallowMultipleComponent]
    public sealed class EnemyPatrol2D : MonoBehaviour
    {
        [Tooltip("Выключено: ждать на текущем месте. Включено: иногда передвигаться до первого обнаружения игрока.")]
        [SerializeField]
        private bool patrolEnabled;
        [SerializeField]
        private bool usePatrolArea = true;
        [Header("World-aligned area relative to spawn")]
        [SerializeField]
        private Vector2 areaOffset;
        [SerializeField]
        private Vector2 areaSize = new Vector2(3, 3);
        [SerializeField]
        private EnemyPatrolProfile profile;
        public EnemyPatrolProfile Profile => profile;
        private float patrolSpeed => profile != null ? profile.speed : 0;
        private float wanderStepDistance => profile != null ? profile.wanderStepDistance : 0;
        private Vector2 pauseSeconds => profile != null ? profile.pauseSeconds : Vector2.one;

        private Vector2 origin;
        private bool initialized;
        private bool hasTarget;
        private Vector2 target;
        private float pauseLeft;
        private float stalledFor;
        private float previousDistance;
        public bool PatrolEnabled => isActiveAndEnabled && patrolEnabled;
        public bool UsePatrolArea => usePatrolArea;
        public Vector2 AreaOrigin => Application.isPlaying && initialized ? origin : (Vector2)transform.position;
        public Rect Area => new Rect(AreaOrigin + areaOffset - areaSize * .5f, areaSize);
        public bool HasTarget => hasTarget;
        public Vector2 Target => target;
        public bool IsPausing => pauseLeft > 0;

        public bool ContainsBody(Vector2 position, float radius)
        {
            if (!usePatrolArea)
            {
                return true;
            }

            Rect area = Area;
            Vector2 padding = Vector2.one * (radius + .04f);
            return Contains(position, area.min + padding, area.max - padding);
        }

        [System.Serializable]
        public sealed class Snapshot
        {
            public Vector2 origin;
            public bool hasTarget;
            public Vector2 target;
            public float pause;
            public float stalled;
            public float previousDistance;
        }

        public Snapshot CaptureState()
        {
            return new Snapshot
            {
                origin = origin,
                hasTarget = hasTarget,
                target = target,
                pause = pauseLeft,
                stalled = stalledFor,
                previousDistance = previousDistance
            };
        }

        public void RestoreState(Snapshot saved)
        {
            origin = saved.origin;
            initialized = true;
            hasTarget = saved.hasTarget;
            target = saved.target;
            pauseLeft = saved.pause;
            stalledFor = saved.stalled;
            previousDistance = saved.previousDistance;
        }

        public void Initialize(Vector2 spawn)
        {
            origin = spawn;
            initialized = true;
            ResetRoute();
        }

        public void ResetRoute()
        {
            hasTarget = false;
            pauseLeft = 0;
            stalledFor = 0;
        }

        // Вызывается владельцем состояния только в Patrolling, никогда во время укуса/страха/смерти.
        public Vector2 Step(Vector2 position, float radius, LayerMask solids, float dt)
        {
            if (!PatrolEnabled)
            {
                return Vector2.zero;
            }

            Rect area = Area;
            Vector2 min = area.min + Vector2.one * (radius + .04f);
            Vector2 max = area.max - Vector2.one * (radius + .04f);
            // Некорректная/слишком тесная область не вызывает телепортации или движения за границей.
            if (usePatrolArea && (min.x >= max.x || min.y >= max.y))
            {
                return Vector2.zero;
            }

            if (pauseLeft > 0)
            {
                pauseLeft = Mathf.Max(0, pauseLeft - dt);
                return Vector2.zero;
            }

            if (hasTarget && ((usePatrolArea && !Contains(target, min, max)) || Vector2.Distance(position, target) < .12f))
            {
                Pause();
                return Vector2.zero;
            }

            if (!hasTarget)
            {
                // Ограниченное число проб: не строим путь сквозь стены и не ищем всю сцену каждый кадр.
                for (int i = 0; i < 12; i++)
                {
                    Vector2 candidate = usePatrolArea ? new Vector2(Random.Range(min.x, max.x), Random.Range(min.y, max.y)) : position + Random.insideUnitCircle * wanderStepDistance;
                    Vector2 delta = candidate - position;
                    if (delta.magnitude < .3f || Physics2D.CircleCast(position, radius, delta.normalized, delta.magnitude, solids).collider != null)
                    {
                        continue;
                    }

                    target = candidate;
                    hasTarget = true;
                    previousDistance = delta.magnitude;
                    stalledFor = 0;
                    break;
                }

                if (!hasTarget)
                {
                    pauseLeft = .4f;
                    return Vector2.zero;
                }
            }

            Vector2 toTarget = target - position;
            float distance = toTarget.magnitude;
            stalledFor = previousDistance - distance > dt * patrolSpeed * .15f ? 0 : stalledFor + dt;
            previousDistance = distance;
            float step = Mathf.Min(patrolSpeed * dt, distance);
            if (stalledFor > .9f || Physics2D.CircleCast(position, radius, toTarget.normalized, step + .04f, solids).collider != null)
            {
                Pause();
                return Vector2.zero;
            }

            return toTarget.normalized * (step / Mathf.Max(.0001f, dt));
        }

        private static bool Contains(Vector2 point, Vector2 min, Vector2 max)
        {
            return point.x >= min.x && point.x <= max.x && point.y >= min.y && point.y <= max.y;
        }

        private void Pause()
        {
            hasTarget = false;
            stalledFor = 0;
            pauseLeft = Random.Range(pauseSeconds.x, pauseSeconds.y);
        }

        private void OnValidate()
        {
            areaSize = Vector2.Max(areaSize, Vector2.one * .2f);
            ResetRoute();
        }

        private void OnDrawGizmosSelected()
        {
            Rect area = Area;
            Gizmos.color = new Color(.95f, .72f, .2f, .8f);
            if (usePatrolArea)
            {
                Gizmos.DrawWireCube(new Vector3(area.center.x, area.center.y, transform.position.z), new Vector3(area.width, area.height, 0));
            }

            if (Application.isPlaying && hasTarget)
            {
                Gizmos.DrawLine(transform.position, target);
                Gizmos.DrawWireSphere(target, .09f);
            }
        }
    }
}
