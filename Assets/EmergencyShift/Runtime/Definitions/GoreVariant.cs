using System;
using UnityEngine;

namespace EmergencyShift
{
    [Serializable]
    public sealed class GoreVariant
    {
        [SerializeField] private string id;
        [SerializeField, Min(1)] private int fragmentCount = 8;
        [SerializeField] private int spriteOffset;
        [SerializeField] private float rotationOffset;
        [SerializeField] private Vector2 scale = new Vector2(.65f, .95f);
        [SerializeField, Min(.1f)] private float impulseMultiplier = 1;
        [SerializeField, Tooltip("Optional explicit composition; empty retains radial fragments.")]
        private GorePiece[] pieces;
        [SerializeField] private bool replaceOriginalBody;
        [SerializeField, Tooltip("Available for old snapshots, excluded from a new death.")]
        private bool restorationOnly;

        public string Id => id;
        public int FragmentCount => fragmentCount;
        public int SpriteOffset => spriteOffset;
        public float RotationOffset => rotationOffset;
        public Vector2 Scale => scale;
        public float ImpulseMultiplier => impulseMultiplier;
        public bool ReplaceOriginalBody => replaceOriginalBody;
        public bool RestorationOnly => restorationOnly;
        public int PieceCount => pieces != null ? pieces.Length : 0;
        public GorePiece PieceAt(int index) => pieces[index];

        public GoreVariant()
        {
        }

        public GoreVariant(string id, bool restorationOnly = false)
        {
            this.id = id;
            this.restorationOnly = restorationOnly;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(id) || fragmentCount < 1 || spriteOffset < 0 ||
                !float.IsFinite(rotationOffset) || !float.IsFinite(scale.x) || !float.IsFinite(scale.y) ||
                scale.x <= 0 || scale.y < scale.x || !float.IsFinite(impulseMultiplier) || impulseMultiplier <= 0)
            {
                throw new InvalidOperationException("Invalid gore variant.");
            }

            for (int index = 0; index < PieceCount; index++)
            {
                if (pieces[index] == null)
                {
                    throw new InvalidOperationException("Missing gore piece.");
                }

                pieces[index].Validate();
            }
        }
    }
}
