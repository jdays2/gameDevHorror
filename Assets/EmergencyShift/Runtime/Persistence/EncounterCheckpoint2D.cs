using UnityEngine;

namespace EmergencyShift
{
    [DisallowMultipleComponent, RequireComponent(typeof(PersistentEntity2D))]
    public sealed class EncounterCheckpoint2D : MonoBehaviour, ICheckpointParticipant
    {
        public string SaveKey => "EncounterCheckpoint2D.v1";

        public string CaptureCheckpoint()
        {
            var loot = GetComponent<EncounterActivation2D>();
            return CheckpointBooleanPayload.CaptureConsumed(loot.Activated);
        }

        public void ValidateCheckpoint(string json)
        {
            CheckpointBooleanPayload.ReadConsumed(json);
        }

        public void RestoreCheckpoint(string json)
        {
            bool consumed = CheckpointBooleanPayload.ReadConsumed(json);
            var loot = GetComponent<EncounterActivation2D>();
            loot.RestoreState(consumed);
        }
    }
}
