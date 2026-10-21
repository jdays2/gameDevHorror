using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public readonly struct EnemyPatrolBounds
    {
        public EnemyPatrolBounds(bool useArea, Vector2 offset, Vector2 size)
        {
            UseArea = useArea;
            Offset = offset;
            Size = size;
        }

        public bool UseArea { get; }
        public Vector2 Offset { get; }
        public Vector2 Size { get; }
        public bool IsValid => Finite(Offset) && Finite(Size) && Size.x > 0 && Size.y > 0;

        public Rect Area(Vector2 origin) => new Rect(origin + Offset - Size * .5f, Size);

        public bool ContainsBody(Vector2 origin, Vector2 position, float radius, float skin)
        {
            if (!UseArea)
            {
                return true;
            }

            Rect area = Area(origin);
            Vector2 padding = Vector2.one * (radius + skin);
            return Contains(position, area.min + padding, area.max - padding);
        }

        internal static bool Contains(Vector2 point, Vector2 min, Vector2 max) =>
            point.x >= min.x && point.x <= max.x && point.y >= min.y && point.y <= max.y;

        internal static bool Finite(Vector2 point) => float.IsFinite(point.x) && float.IsFinite(point.y);
    }
}
