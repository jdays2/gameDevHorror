using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Only access tokens for this run; not a general inventory or a save system.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAccessCards2D : MonoBehaviour
    {
        private readonly HashSet<string> cards = new HashSet<string>();
        public bool Has(string id) => !string.IsNullOrWhiteSpace(id) && cards.Contains(id);
        public bool Add(string id) => !string.IsNullOrWhiteSpace(id) && cards.Add(id);
        public int Count => cards.Count;
    }
}
