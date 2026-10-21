using UnityEngine;

namespace EmergencyShift
{
    [RequireComponent(typeof(PersistentEntity2D))]
    public sealed class DoorCheckpoint2D : MonoBehaviour, ICheckpointParticipant
    {
        public string SaveKey => "door.v1";

        public string CaptureCheckpoint()
        {
            var gate = GetComponent<DoorAccess2D>();
            return CheckpointBooleanPayload.CaptureDoor(GetComponent<Door2D>().IsOpen, gate != null && gate.IsUnlocked);
        }

        public void ValidateCheckpoint(string json)
        {
            CheckpointBooleanPayload.ReadDoor(json);
        }

        public void RestoreCheckpoint(string json)
        {
            CheckpointBooleanPayload.DoorState saved = CheckpointBooleanPayload.ReadDoor(json);
            var gate = GetComponent<DoorAccess2D>();
            if (gate != null)
            {
                gate.RestoreState(saved.unlocked);
            }

            GetComponent<Door2D>().RestoreState(saved.open);
        }
    }
}
