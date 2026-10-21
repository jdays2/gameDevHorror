using System;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Value-only legacy checkpoint DTO. Namespace and field names preserve the saved schema.</summary>
    [Serializable]
    public sealed class RemainsSpriteState
    {
        public Vector3 position;
        public Vector3 scale;
        public Quaternion rotation;
        public Color color;
        public bool visible;
    }
}
