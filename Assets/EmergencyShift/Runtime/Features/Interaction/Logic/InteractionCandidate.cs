using UnityEngine;

namespace EmergencyShift.Features.Interaction.Logic
{
    /// <summary>Index is transient within one query, never a persistent entity identity.</summary>
    public readonly struct InteractionCandidate
    {
        public InteractionCandidate(int index, bool available, Vector2 offset, Vector2 facing, bool hasFacing)
        {
            Index = index;
            Available = available;
            Offset = offset;
            Facing = facing;
            HasFacing = hasFacing;
        }

        public int Index { get; }
        public bool Available { get; }
        public Vector2 Offset { get; }
        public Vector2 Facing { get; }
        public bool HasFacing { get; }
    }
}
