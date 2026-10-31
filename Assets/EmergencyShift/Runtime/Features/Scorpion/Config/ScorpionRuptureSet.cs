using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift.Features.Scorpion.Config
{
    [CreateAssetMenu(menuName = "Emergency Shift/Enemies/Scorpion rupture poses")]
    public sealed class ScorpionRuptureSet : ScriptableObject
    {
        [Serializable]
        public sealed class Pose
        {
            [SerializeField] private Sprite source;
            [SerializeField] private Sprite[] parts;
            [SerializeField] private Vector2[] offsets;
            public Sprite Source => source;
            public int Count => parts.Length;
            public Sprite Part(int index) => parts[index];
            public Vector2 Offset(int index) => offsets[index];
            public void Validate()
            {
                if (source == null || parts == null || parts.Length != 12 || offsets == null || offsets.Length != parts.Length)
                {
                    throw new InvalidOperationException("Incomplete scorpion rupture pose.");
                }
                for (int index = 0; index < parts.Length; index++)
                {
                    if (parts[index] == null || !float.IsFinite(offsets[index].x) || !float.IsFinite(offsets[index].y))
                    {
                        throw new InvalidOperationException("Invalid scorpion rupture part: " + source.name);
                    }
                }
            }
        }
        [SerializeField] private Pose[] poses;
        public int PoseCount => poses.Length;
        public Pose PoseAt(int index) => poses[index];
        public int IndexOf(Sprite source)
        {
            for (int index = 0; index < poses.Length; index++)
            {
                if (poses[index].Source == source)
                {
                    return index;
                }
            }
            throw new InvalidOperationException("Scorpion death pose was not exported: " + source?.name);
        }
        public void Validate()
        {
            if (poses == null || poses.Length == 0)
            {
                throw new InvalidOperationException("Missing scorpion rupture poses: " + name);
            }
            var sources = new HashSet<Sprite>();
            foreach (Pose pose in poses)
            {
                if (pose == null)
                {
                    throw new InvalidOperationException("Missing scorpion rupture pose: " + name);
                }
                pose.Validate();
                if (!sources.Add(pose.Source))
                {
                    throw new InvalidOperationException("Duplicate scorpion rupture pose: " + pose.Source.name);
                }
            }
        }
        public Pose Find(Sprite source)
        {
            return poses[IndexOf(source)];
        }
    }
}
