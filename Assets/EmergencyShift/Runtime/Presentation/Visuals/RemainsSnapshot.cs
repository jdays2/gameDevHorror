using System;
using UnityEngine;

namespace EmergencyShift
{
    [Serializable]
    public sealed class RemainsSpriteState
    {
        public Vector3 position;
        public Vector3 scale;
        public Quaternion rotation;
        public Color color;
        public bool visible;
        public void Capture(SpriteRenderer renderer)
        {
            position = renderer.transform.localPosition;
            scale = renderer.transform.localScale;
            rotation = renderer.transform.localRotation;
            color = renderer.color;
            visible = renderer.enabled;
        }

        public void Apply(SpriteRenderer renderer)
        {
            renderer.transform.localPosition = position;
            renderer.transform.localScale = scale;
            renderer.transform.localRotation = rotation;
            renderer.color = color;
            renderer.enabled = visible;
        }
    }

    [Serializable]
    public sealed class RemainsPartState
    {
        public Vector3 position;
        public int spriteIndex;
        public RemainsSpriteState body;
        public bool hasShadow;
        public RemainsSpriteState shadow;
    }

    [Serializable]
    public sealed class RemainsSnapshot
    {
        public string variantId;
        public Vector3 position;
        public bool hasPool;
        public RemainsSpriteState[] pool;
        public RemainsPartState[] parts;
    }
}
