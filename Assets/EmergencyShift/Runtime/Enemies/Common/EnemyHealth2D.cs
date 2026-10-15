using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Instance-owned health. Death is emitted once; no AI, sprite, sound or species dependency.</summary>
    [DisallowMultipleComponent]
    public sealed class EnemyHealth2D : MonoBehaviour
    {
        public int Current
        {
            get; private set;
        }
        public int Maximum
        {
            get; private set;
        }
        public bool IsAlive => Current > 0;

        public event System.Action Damaged, Died;
        public void Initialize(int maximum)
        {
            Maximum = Mathf.Max(1, maximum);
            Current = Maximum;
        }

        public bool TakeDamage(int amount)
        {
            if (!IsAlive || amount <= 0)
            {
                return false;
            }

            Current = Mathf.Max(0, Current - amount);
            if (Current == 0)
            {
                Died?.Invoke();
            }
            else
            {
                Damaged?.Invoke();
            }

            return true;
        }

        public void Restore(int current)
        {
            Current = Mathf.Clamp(current, 0, Maximum);
        }

        public void Kill()
        {
            if (IsAlive)
            {
                TakeDamage(Current);
            }
        }
    }
}
