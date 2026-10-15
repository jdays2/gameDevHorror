using System;
using UnityEngine;

namespace EmergencyShift
{
    [Serializable]
    public sealed class CheckpointPart
    {
        public string key;
        public string json;
    }

    [Serializable]
    public sealed class CheckpointEntityState
    {
        public string id;
        public bool active;
        public Vector3 position;
        public Quaternion rotation;
        public CheckpointPart[] parts;
    }

    [Serializable]
    public sealed class CheckpointData
    {
        public const int CurrentVersion = 1;
        public int version = CurrentVersion;
        public int levelRevision;
        public string levelId;
        public string checkpointId;
        public float elapsedSeconds;
        public string[] visitedCheckpoints;
        public CheckpointEntityState[] entities;
    }

    public interface ICheckpointParticipant
    {
        string SaveKey
        {
            get;
        }

        string CaptureCheckpoint();
        void ValidateCheckpoint(string json);
        void RestoreCheckpoint(string json);
    }
}
