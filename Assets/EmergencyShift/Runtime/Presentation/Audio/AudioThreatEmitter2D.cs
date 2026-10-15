using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Small scene-filtered registry. New enemy adapters expose state without changing the music director.</summary>
    public abstract class AudioThreatEmitter2D : MonoBehaviour
    {
        private static readonly List<AudioThreatEmitter2D> active = new List<AudioThreatEmitter2D>();
        public static IReadOnlyList<AudioThreatEmitter2D> Active => active;
        public abstract bool IsAlive
        {
            get;
        }
        public abstract bool IsEngaged
        {
            get;
        }
        public abstract bool IsAttacking
        {
            get;
        }
        public abstract Transform Target
        {
            get;
        }
        public abstract LayerMask Obstacles
        {
            get;
        }
        public float HeardUntil { get; private set; } = float.NegativeInfinity;
        public float UrgentUntil { get; private set; } = float.NegativeInfinity;

        protected virtual void OnEnable()
        {
            if (!active.Contains(this))
            {
                active.Add(this);
            }
        }

        protected virtual void OnDisable()
        {
            active.Remove(this);
            HeardUntil = UrgentUntil = float.NegativeInfinity;
        }

        protected void ReportHeard(float seconds, float gain, bool urgent)
        {
            if (gain < .02f || !IsAlive)
            {
                return;
            }

            HeardUntil = Mathf.Max(HeardUntil, Time.time + Mathf.Clamp(seconds, .25f, 2f));
            if (urgent)
            {
                UrgentUntil = Time.time + .4f;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            active.Clear();
        }
    }
}
