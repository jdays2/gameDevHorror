using System;
using UnityEngine;

namespace EmergencyShift
{
    [DisallowMultipleComponent, RequireComponent(typeof(PersistentEntity2D))]
    public sealed class CardCheckpoint2D : MonoBehaviour, ICheckpointParticipant
    {
        [Serializable]
        private sealed class State
        {
            public int format;
            public bool consumed;
            public int remaining;
        }

        public string SaveKey => "CardCheckpoint2D.v1";

        public string CaptureCheckpoint()
        {
            var loot = GetComponent<AccessCardPickup2D>();
            return JsonUtility.ToJson(new State { format = 1, consumed = loot.Collected });
        }

        public void ValidateCheckpoint(string json)
        {
            var saved = JsonUtility.FromJson<State>(json);
            if (saved == null || saved.format != 1)
            {
                throw new InvalidOperationException("Invalid saved pickup/encounter");
            }
        }

        public void RestoreCheckpoint(string json)
        {
            var saved = JsonUtility.FromJson<State>(json);
            var loot = GetComponent<AccessCardPickup2D>();
            loot.RestoreState(saved.consumed);
        }
    }
}
