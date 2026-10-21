using UnityEngine;

namespace EmergencyShift
{
    [DisallowMultipleComponent, RequireComponent(typeof(PersistentEntity2D))]
    public sealed class UpgradeCheckpoint2D : MonoBehaviour, ICheckpointParticipant
    {
        public string SaveKey => "UpgradeCheckpoint2D.v1";

        public string CaptureCheckpoint()
        {
            var loot = GetComponent<FlashlightUpgradePickup>();
            return CheckpointBooleanPayload.CaptureConsumed(loot.Collected);
        }

        public void ValidateCheckpoint(string json)
        {
            CheckpointBooleanPayload.ReadConsumed(json);
        }

        public void RestoreCheckpoint(string json)
        {
            bool consumed = CheckpointBooleanPayload.ReadConsumed(json);
            var loot = GetComponent<FlashlightUpgradePickup>();
            loot.RestoreState(consumed);
        }
    }
}
