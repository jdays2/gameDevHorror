using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Config
{
    [CreateAssetMenu(menuName = "Emergency Shift/Enemies/Stalker limb rig")]
    public sealed class StalkerRigProfile : ScriptableObject
    {
        [SerializeField] private Texture2D atlas;
        [SerializeField] private Sprite torso;
        [SerializeField] private Material surfaceMaterial;
        [SerializeField] private Material shadowMaterial;
        [SerializeField] private Vector2 pivot = new Vector2(187, 200);
        [SerializeField] private float pixelsPerUnit = 224;
        [SerializeField] private Vector2 cellSize = new Vector2(358, 406);
        [SerializeField] private Rect[] layerBounds;
        [SerializeField] private StalkerLimb[] limbs;
        [SerializeField, Min(.05f)] private float cycleDistance = .68f;
        [SerializeField, Range(.5f, .8f)] private float supportFraction = .60f;
        [SerializeField, Min(.01f)] private float liftHeight = .075f;
        [SerializeField, Min(.01f)] private float bodyHeight = .13f;
        [SerializeField, Min(0)] private float weightTransfer = .018f;

        public Texture2D Atlas => atlas;
        public Sprite Torso => torso;
        public Material SurfaceMaterial => surfaceMaterial;
        public Material ShadowMaterial => shadowMaterial;
        public Vector2 Pivot => pivot;
        public float PixelsPerUnit => pixelsPerUnit;
        public Vector2 CellSize => cellSize;
        public IReadOnlyList<Rect> LayerBounds => layerBounds;
        public IReadOnlyList<StalkerLimb> Limbs => limbs;
        public float CycleDistance => cycleDistance;
        public float SupportFraction => supportFraction;
        public float LiftHeight => liftHeight;
        public float BodyHeight => bodyHeight;
        public float WeightTransfer => weightTransfer;

        public void Validate()
        {
            if (atlas == null || torso == null || surfaceMaterial == null || shadowMaterial == null || limbs == null || limbs.Length != 6 ||
                layerBounds == null || layerBounds.Length != 13 || !float.IsFinite(cycleDistance) ||
                cycleDistance < .05f || !float.IsFinite(supportFraction) || supportFraction < .5f || supportFraction > .8f ||
                !float.IsFinite(pixelsPerUnit) || pixelsPerUnit <= 0 || !float.IsFinite(liftHeight) || liftHeight < 0 ||
                !float.IsFinite(bodyHeight) || bodyHeight <= 0 || !float.IsFinite(weightTransfer) || weightTransfer < 0)
            {
                throw new InvalidOperationException("Incomplete Stalker limb rig: " + name);
            }
            foreach (StalkerLimb limb in limbs)
            {
                if (!float.IsFinite(limb.UpperLength) || !float.IsFinite(limb.LowerLength) ||
                    limb.UpperLength <= 0 || limb.LowerLength <= 0 || !float.IsFinite(limb.PhaseOffset))
                {
                    throw new InvalidOperationException("Invalid Stalker bone lengths: " + name);
                }
            }
        }
    }

    [Serializable]
    public struct StalkerLimb
    {
        public string Id;
        public Vector2 Hip;
        public Vector2 Knee;
        public Vector2 Tip;
        public float UpperLength;
        public float LowerLength;
        public float PhaseOffset;
    }
}
