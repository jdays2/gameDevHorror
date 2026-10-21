using System;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Settled part values; does not create objects or write their transforms.</summary>
    [Serializable]
    public sealed class RemainsPartState
    {
        public Vector3 position;
        public int spriteIndex;
        public RemainsSpriteState body;
        public bool hasShadow;
        public RemainsSpriteState shadow;
    }
}
