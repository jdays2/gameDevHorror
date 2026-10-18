using UnityEngine;

namespace EmergencyShift
{
    /// <summary>UI-only vertical fade; no generated texture or world light.</summary>
    public sealed class DialogueFadeGraphic : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            Color transparent = new Color(color.r, color.g, color.b, 0);
            mesh.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMin), color, Vector2.right);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMax), transparent, Vector2.one);
            mesh.AddVert(new Vector3(rect.xMin, rect.yMax), transparent, Vector2.up);
            mesh.AddTriangle(0, 1, 2);
            mesh.AddTriangle(0, 2, 3);
        }
    }
}
