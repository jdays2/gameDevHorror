using UnityEngine;
using UnityEngine.UI;

namespace EmergencyShift.Features.DeathScreen.View
{
    /// <summary>Black uGUI aperture; reveals no world pixels after closure.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DeathScreenIris : MaskableGraphic
    {
        private const int Columns = 64;
        private const int Rows = 36;
        private float progress = 1;
        private float feather = .18f;

        public void Render(float closureProgress, float edgeFeather)
        {
            float nextProgress = Mathf.Clamp01(closureProgress);
            if (progress == nextProgress && feather == edgeFeather)
            {
                return;
            }
            progress = nextProgress;
            feather = edgeFeather;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            Rect rect = rectTransform.rect;
            if (progress >= 1)
            {
                AddVertex(vertices, new Vector2(rect.xMin, rect.yMin), 1);
                AddVertex(vertices, new Vector2(rect.xMin, rect.yMax), 1);
                AddVertex(vertices, new Vector2(rect.xMax, rect.yMax), 1);
                AddVertex(vertices, new Vector2(rect.xMax, rect.yMin), 1);
                vertices.AddTriangle(0, 1, 2);
                vertices.AddTriangle(2, 3, 0);
                return;
            }
            float radius = Mathf.Lerp(1 - feather * .5f, -feather, progress);
            for (int row = 0; row <= Rows; row++)
            {
                float y = row / (float)Rows;
                for (int column = 0; column <= Columns; column++)
                {
                    float x = column / (float)Columns;
                    float distance = new Vector2(x * 2 - 1, y * 2 - 1).magnitude;
                    float edge = Mathf.Clamp01((distance - radius) / feather);
                    float alpha = edge * edge * (3 - 2 * edge);
                    AddVertex(vertices, new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, x),
                        Mathf.Lerp(rect.yMin, rect.yMax, y)), alpha);
                }
            }
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    int lowerLeft = row * (Columns + 1) + column;
                    int upperLeft = lowerLeft + Columns + 1;
                    vertices.AddTriangle(lowerLeft, upperLeft, upperLeft + 1);
                    vertices.AddTriangle(upperLeft + 1, lowerLeft + 1, lowerLeft);
                }
            }
        }

        private static void AddVertex(VertexHelper vertices, Vector2 position, float alpha)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = new Color(0, 0, 0, alpha);
            vertices.AddVert(vertex);
        }
    }
}
