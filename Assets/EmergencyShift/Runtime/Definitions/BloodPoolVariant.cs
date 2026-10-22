using System;
using UnityEngine;

namespace EmergencyShift
{
    [Serializable]
    public sealed class BloodPoolVariant
    {
        [SerializeField] private string id;
        [SerializeField] private Sprite sprite;
        [SerializeField, Min(.1f)] private float canvasSize = 4.5f;
        public string Id => id;
        public Sprite Sprite => sprite;
        public float CanvasSize => canvasSize;

        public BloodPoolVariant()
        {
        }

        public BloodPoolVariant(string id, Sprite sprite)
        {
            this.id = id;
            this.sprite = sprite;
        }

        public bool IsValid() => !string.IsNullOrWhiteSpace(id) && sprite != null && float.IsFinite(canvasSize) && canvasSize > 0;
    }
}
