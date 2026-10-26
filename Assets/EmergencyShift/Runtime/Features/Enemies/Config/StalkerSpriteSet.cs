using System;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Config
{
    /// <summary>Shared, immutable whole-body poses: closure rows, movement phases within each row.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Enemies/Stalker sprite set")]
    public sealed class StalkerSpriteSet : ScriptableObject
    {
        [SerializeField, Min(2)] private int phases = 48;
        [SerializeField, Min(2)] private int closureRows = 9;
        [SerializeField, Min(.01f), Tooltip("Visual body tuck duration; does not delay eye protection or AI.")]
        private float closureSeconds = .24f;
        [SerializeField] private Sprite[] frames;

        public int Phases => phases;
        public int ClosureRows => closureRows;
        public float ClosureSeconds => closureSeconds;

        public int FrameIndex(float phase, float closure)
        {
            int row = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(closure) * (closureRows - 1)), 0, closureRows - 1);
            int column = Mathf.Clamp(Mathf.FloorToInt(Mathf.Repeat(phase, 1) * phases), 0, phases - 1);
            return row * phases + column;
        }

        public Sprite Frame(int index) => frames[index];

        public void Validate()
        {
            if (phases < 2 || closureRows < 2 || !float.IsFinite(closureSeconds) || closureSeconds < .01f ||
                frames == null || frames.Length != phases * closureRows)
            {
                throw new InvalidOperationException("Incomplete Stalker sprite matrix: " + name);
            }
            foreach (Sprite frame in frames)
            {
                if (frame == null)
                {
                    throw new InvalidOperationException("Missing Stalker body frame: " + name);
                }
            }
        }
    }
}
