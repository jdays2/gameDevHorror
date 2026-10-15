using System;
using UnityEngine;

namespace EmergencyShift
{
    [RequireComponent(typeof(PersistentEntity2D))]
    public sealed class DoorCheckpoint2D : MonoBehaviour, ICheckpointParticipant
    {
        [Serializable]
        private sealed class State
        {
            public int format;
            public bool open;
            public bool unlocked;
        }

        public string SaveKey => "door.v1";

        public string CaptureCheckpoint()
        {
            var gate = GetComponent<DoorAccess2D>();
            return JsonUtility.ToJson(new State { format = 1, open = GetComponent<Door2D>().IsOpen, unlocked = gate != null && gate.IsUnlocked });
        }

        public void ValidateCheckpoint(string json)
        {
            var saved = JsonUtility.FromJson<State>(json);
            if (saved == null || saved.format != 1)
            {
                throw new InvalidOperationException("Invalid door checkpoint");
            }
        }

        public void RestoreCheckpoint(string json)
        {
            var saved = JsonUtility.FromJson<State>(json);
            var gate = GetComponent<DoorAccess2D>();
            if (gate != null)
            {
                gate.RestoreState(saved.unlocked);
            }

            GetComponent<Door2D>().RestoreState(saved.open);
        }
    }
}
