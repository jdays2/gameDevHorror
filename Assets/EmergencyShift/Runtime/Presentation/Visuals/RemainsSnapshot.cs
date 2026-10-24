using System;
using UnityEngine;

namespace EmergencyShift
{
    [Serializable]
    public sealed class RemainsSnapshot
    {
        public const int CurrentVersion = 1;
        // Zero is the existing unversioned format: all parts use their recorded ordinary material.
        public int version;
        public string variantId;
        public Vector3 position;
        public bool hasPool;
        public string bloodVariantId;
        public RemainsSpriteState[] pool;
        public RemainsPartState[] parts;
    }
}
