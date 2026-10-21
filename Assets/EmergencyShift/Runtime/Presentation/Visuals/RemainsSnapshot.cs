using System;
using UnityEngine;

namespace EmergencyShift
{
    [Serializable]
    public sealed class RemainsSnapshot
    {
        public string variantId;
        public Vector3 position;
        public bool hasPool;
        public string bloodVariantId;
        public RemainsSpriteState[] pool;
        public RemainsPartState[] parts;
    }
}
