using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Temporary shooting-range health; leaving preserves the health held before entry.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class SandboxHealthZone2D : MonoBehaviour
    {
        [SerializeField, Min(1)]
        private int testHealth = 100;
        private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();
        private PlayerLife2D player;
        private int previousCapacity;
        private int previousHealth;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var life = other.GetComponentInParent<PlayerLife2D>();
            if (life == null || life.IsDead || (player != null && player != life))
            {
                return;
            }

            occupants.Add(other);
            if (player != null)
            {
                return;
            }

            player = life;
            previousCapacity = life.MaximumHealth;
            previousHealth = life.Health;
            int capacity = Mathf.Max(testHealth, previousCapacity);
            player.SetHealthCapacity(capacity, capacity);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (occupants.Remove(other) && occupants.Count == 0)
            {
                Restore();
            }
        }

        private void Restore()
        {
            if (player != null)
            {
                player.SetHealthCapacity(previousCapacity, Mathf.Min(previousHealth, player.Health));
            }

            player = null;
            occupants.Clear();
        }

        private void OnDisable()
        {
            Restore();
        }
    }
}
