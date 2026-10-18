using UnityEngine;

namespace EmergencyShift
{
    /// <summary>UI-only vertical fade; no generated texture or world light.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DialogueFadeGraphic : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            const int segments = 48;
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                Color row = color;
                row.a *= 1 - Mathf.SmoothStep(0, 1, t * t);
                float y = Mathf.Lerp(rect.yMin, rect.yMax, t);
                mesh.AddVert(new Vector3(rect.xMin, y), row, new Vector2(0, t));
                mesh.AddVert(new Vector3(rect.xMax, y), row, new Vector2(1, t));
                if (i == segments)
                {
                    continue;
                }

                int start = i * 2;
                mesh.AddTriangle(start, start + 1, start + 3);
                mesh.AddTriangle(start, start + 3, start + 2);
            }
        }
    }
}
