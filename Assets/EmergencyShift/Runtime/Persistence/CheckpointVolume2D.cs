using UnityEngine;

namespace EmergencyShift
{
    [RequireComponent(typeof(BoxCollider2D)), DisallowMultipleComponent]
    public sealed class CheckpointVolume2D : MonoBehaviour
    {
        [SerializeField]
        private string checkpointId;
        [SerializeField]
        private LevelCheckpointController2D controller;
        public string Id => checkpointId;

        public void Configure(string id, LevelCheckpointController2D owner)
        {
            checkpointId = id;
            controller = owner;
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (controller != null && other.GetComponentInParent<PlayerLife2D>() != null)
            {
                controller.RequestSave(checkpointId);
            }
        }

        private void OnDrawGizmosSelected()
        {
            var box = GetComponent<BoxCollider2D>();
            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.offset, box.size);
        }
    }
}
