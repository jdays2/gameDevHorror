using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Marks a full-height wall or door, independently of low furniture collision layers.</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class FullHeightLightBlocker2D : MonoBehaviour
    {
        private static readonly List<FullHeightLightBlocker2D> active = new List<FullHeightLightBlocker2D>();
        public static int RegistryVersion { get; private set; }

        private void OnEnable()
        {
            if (gameObject.scene.IsValid() && !active.Contains(this))
            {
                active.Add(this);
                RegistryVersion++;
            }
        }

        private void OnDisable()
        {
            if (active.Remove(this))
            {
                RegistryVersion++;
            }
        }

        public static void CollectFor(Component owner, List<BoxCollider2D> destination)
        {
            foreach (var blocker in active)
            {
                if (blocker != null && blocker.gameObject.scene == owner.gameObject.scene)
                {
                    var shape = blocker.GetComponent<BoxCollider2D>();
                    if (shape != null && !destination.Contains(shape))
                    {
                        destination.Add(shape);
                    }
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            active.Clear();
            RegistryVersion++;
        }
    }
}
