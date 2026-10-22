using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    [DisallowMultipleComponent]
    public sealed class PersistentEntity2D : MonoBehaviour
    {
        [SerializeField, Tooltip("Unique per placed scene instance. Assigned by Prepare level persistence.")]
        private string persistentId;
        public string Id => persistentId;

        public void AssignId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Persistent instance ID is required.", nameof(value));
            }

            persistentId = value;
        }

        private Dictionary<string, ICheckpointParticipant> Participants()
        {
            var result = new Dictionary<string, ICheckpointParticipant>(StringComparer.Ordinal);
            foreach (var behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour is ICheckpointParticipant participant)
                {
                    if (string.IsNullOrWhiteSpace(participant.SaveKey) || result.ContainsKey(participant.SaveKey))
                    {
                        throw new InvalidOperationException("Missing or duplicate checkpoint participant key: " + name);
                    }

                    result.Add(participant.SaveKey, participant);
                }
            }

            return result;
        }

        public CheckpointEntityState Capture()
        {
            var participants = Participants();
            var saved = new CheckpointEntityState
            {
                id = persistentId,
                active = gameObject.activeSelf,
                position = transform.position,
                rotation = transform.rotation,
                parts = new CheckpointPart[participants.Count]
            };
            int index = 0;
            foreach (var participant in participants)
            {
                saved.parts[index++] = new CheckpointPart
                {
                    key = participant.Key,
                    json = participant.Value.CaptureCheckpoint()
                };
            }

            return saved;
        }

        public void Validate(CheckpointEntityState saved)
        {
            if (saved == null || string.IsNullOrWhiteSpace(Id) || !string.Equals(saved.id, Id, StringComparison.Ordinal))
            {
                throw new System.IO.InvalidDataException("Missing/mismatched checkpoint entity ID.");
            }

            CheckpointValueValidation.ValidatePose(saved.position, saved.rotation);
            var participants = Participants();
            if (saved.parts == null || saved.parts.Length != participants.Count)
            {
                throw new InvalidOperationException("Checkpoint component layout changed: " + Id);
            }

            var keys = new HashSet<string>();
            foreach (var part in saved.parts)
            {
                if (part == null || string.IsNullOrWhiteSpace(part.key) || !keys.Add(part.key) || !participants.TryGetValue(part.key, out var participant))
                {
                    throw new InvalidOperationException("Missing, unknown or duplicate save component.");
                }

                participant.ValidateCheckpoint(part.json);
            }
        }

        public void PrepareRestore(CheckpointEntityState saved)
        {
            Validate(saved);
            // Inactive encounter actors need Awake before their runtime modules can be restored.
            transform.SetPositionAndRotation(saved.position, saved.rotation);
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        public void Restore(CheckpointEntityState saved)
        {
            Validate(saved);
            var participants = Participants();
            foreach (var part in saved.parts)
            {
                participants[part.key].RestoreCheckpoint(part.json);
            }

            gameObject.SetActive(saved.active);
        }

        public static Dictionary<string, PersistentEntity2D> Collect(UnityEngine.SceneManagement.Scene scene)
        {
            var result = new Dictionary<string, PersistentEntity2D>(StringComparer.Ordinal);
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var entity in root.GetComponentsInChildren<PersistentEntity2D>(true))
                {
                    if (string.IsNullOrWhiteSpace(entity.Id))
                    {
                        throw new InvalidOperationException("Missing persistent ID: " + entity.name);
                    }

                    if (result.ContainsKey(entity.Id))
                    {
                        throw new InvalidOperationException("Duplicate persistent ID: " + entity.Id);
                    }

                    result.Add(entity.Id, entity);
                }
            }

            return result;
        }
    }
}
