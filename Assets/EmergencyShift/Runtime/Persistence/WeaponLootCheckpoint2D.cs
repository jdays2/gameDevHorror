using System;
using UnityEngine;

namespace EmergencyShift
{
    [DisallowMultipleComponent, RequireComponent(typeof(PersistentEntity2D))]
    public sealed class WeaponLootCheckpoint2D : MonoBehaviour, ICheckpointParticipant
    {
        [Serializable]
        private sealed class State
        {
            public int format;
            public bool consumed;
            public int remaining;
        }

        public string SaveKey => "WeaponLootCheckpoint2D.v1";

        public string CaptureCheckpoint()
        {
            var loot = GetComponent<WeaponLootPickup>();
            return JsonUtility.ToJson(new State { format = 1, consumed = loot.Collected, remaining = loot.RemainingCartridges });
        }

        public void ValidateCheckpoint(string json)
        {
            Read(json);
        }

        public void RestoreCheckpoint(string json)
        {
            State saved = Read(json);
            var loot = GetComponent<WeaponLootPickup>();
            loot.RestoreState(saved.consumed, saved.remaining);
        }

        private static State Read(string json)
        {
            CheckpointBooleanPayload.ReadConsumed(json);
            var saved = JsonUtility.FromJson<State>(json);
            var presence = new State { remaining = -1 };
            JsonUtility.FromJsonOverwrite(json, presence);
            if (saved.remaining < 0 || saved.remaining != presence.remaining)
            {
                throw new InvalidOperationException("Missing or negative weapon loot remaining ammo");
            }

            return saved;
        }
    }
}
