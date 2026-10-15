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
        public event System.Action Changed;
        public bool Add(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !cards.Add(id))
            {
                return false;
            }

            Changed?.Invoke();
            return true;
        }

        public string[] Capture()
        {
            var values = new string[cards.Count];
            cards.CopyTo(values);
            System.Array.Sort(values);
            return values;
        }

        public void Restore(string[] values)
        {
            cards.Clear();
            if (values != null)
            {
                foreach (var id in values)
                {
                    cards.Add(id);
                }
            }

            Changed?.Invoke();
        }

        public int Count => cards.Count;
    }
}
