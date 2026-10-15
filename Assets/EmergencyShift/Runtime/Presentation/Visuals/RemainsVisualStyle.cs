using UnityEngine;
using UnityEngine.Rendering;

namespace EmergencyShift
{
    /// <summary>Общее визуальное правило для трупов, кусков и редакторских образцов.</summary>
    public static class RemainsVisualStyle
    {
        public const int LiveOrder = 1;
        public const int DeadOrder = 0;
        public const float SurfaceZ = -0.03f;
        public static readonly Color Tint = new Color(0.58f, 0.55f, 0.60f, 1f);

        public static void Apply(Transform visual)
        {
            var group = visual.GetComponent<SortingGroup>();
            if (group != null) group.sortingOrder = DeadOrder;
            Vector3 position = visual.localPosition;
            position.z = SurfaceZ; visual.localPosition = position;
            foreach (SpriteRenderer renderer in visual.GetComponentsInChildren<SpriteRenderer>())
                renderer.color = Tint;
        }
    }
}
