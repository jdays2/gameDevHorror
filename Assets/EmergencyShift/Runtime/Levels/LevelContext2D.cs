using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Explicit scene-scoped player binding; no global player singleton or per-frame scene searches.</summary>
    [DefaultExecutionOrder(-200), DisallowMultipleComponent]
    public sealed class LevelContext2D : MonoBehaviour
    {
        [SerializeField]
        private PlayerLife2D player;
        [SerializeField]
        private Flashlight2D flashlight;
        private static readonly List<LevelContext2D> active = new List<LevelContext2D>();
        public PlayerLife2D Player => player;
        public Flashlight2D Flashlight => flashlight;

        public void Configure(PlayerLife2D target, Flashlight2D light)
        {
            player = target;
            flashlight = light;
        }

        private void OnEnable()
        {
            if (!active.Contains(this))
            {
                active.Add(this);
            }
        }

        private void OnDisable()
        {
            active.Remove(this);
        }

        public static LevelContext2D For(Component owner)
        {
            if (owner == null)
            {
                return null;
            }

            foreach (var context in active)
            {
                if (context != null && context.gameObject.scene == owner.gameObject.scene)
                {
                    return context;
                }
            }

            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            active.Clear();
        }
    }
}
