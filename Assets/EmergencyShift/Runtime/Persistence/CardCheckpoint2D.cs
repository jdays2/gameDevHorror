using UnityEngine;

namespace EmergencyShift
{
    [DisallowMultipleComponent, RequireComponent(typeof(PersistentEntity2D))]
    public sealed class CardCheckpoint2D : MonoBehaviour, ICheckpointParticipant
    {
        public string SaveKey => "CardCheckpoint2D.v1";

        public string CaptureCheckpoint()
        {
            var loot = GetComponent<AccessCardPickup2D>();
            return CheckpointBooleanPayload.CaptureConsumed(loot.Collected);
        }

        public void ValidateCheckpoint(string json)
        {
            CheckpointBooleanPayload.ReadConsumed(json);
        }

        public void RestoreCheckpoint(string json)
        {
            bool consumed = CheckpointBooleanPayload.ReadConsumed(json);
            var loot = GetComponent<AccessCardPickup2D>();
            loot.RestoreState(consumed);
        }
    }
}
