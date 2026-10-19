using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Rasterizes finite silhouette projections into one light's cookie. Overlap always keeps the darker shadow.</summary>
    public sealed class FloorShadowMask
    {
        public int Resolution { get; }
        public Color32[] Pixels { get; }
        private readonly byte[] filtered;

        public FloorShadowMask(int resolution)
        {
            Resolution = resolution;
            Pixels = new Color32[resolution * resolution];
            filtered = new byte[resolution * resolution];
        }

        public void Clear()
        {
            var white = new Color32(255, 255, 255, 255);
            for (int i = 0; i < Pixels.Length; i++)
            {
                Pixels[i] = white;
            }
        }

        public void Project(Vector2[] outline, Vector2 origin, Matrix4x4 worldToLight, float radius, float length, float fade)
        {
            if (outline.Length < 3 || length <= 0)
            {
                return;
            }

            float coreLength = Mathf.Max(0, length - fade);
            for (int i = 0; i < outline.Length; i++)
            {
                Vector2 a = outline[i];
                Vector2 b = outline[(i + 1) % outline.Length];
                // Keep the quad diagonal stable when an imported outline has the opposite winding.
                if (a.x > b.x || (a.x == b.x && a.y > b.y))
                {
                    Vector2 swap = a;
                    a = b;
                    b = swap;
                }

                Vector2 aDirection = (a - origin).normalized;
                Vector2 bDirection = (b - origin).normalized;
                Vector3 startA = Pixel(a, worldToLight, radius, 0);
                Vector3 startB = Pixel(b, worldToLight, radius, 0);
                Vector3 coreA = Pixel(a + aDirection * coreLength, worldToLight, radius, 0);
                Vector3 coreB = Pixel(b + bDirection * coreLength, worldToLight, radius, 0);
                Quad(startA, startB, coreB, coreA);
                Vector3 endA = Pixel(a + aDirection * length, worldToLight, radius, 1);
                Vector3 endB = Pixel(b + bDirection * length, worldToLight, radius, 1);
                Quad(coreA, coreB, endB, endA);
            }
        }

        private Vector3 Pixel(Vector2 world, Matrix4x4 worldToLight, float radius, float value)
        {
            Vector3 local = worldToLight.MultiplyPoint3x4(world);
            // URP 6000.6 Light2D.shader offsets cookie UVs by half a texel of its 256-wide light lookup.
            const float lookupHalfTexel = .5f / 256;
            return new Vector3((local.x / (radius * 2) + .5f + lookupHalfTexel) * Resolution,
                (local.y / (radius * 2) + .5f + lookupHalfTexel) * Resolution, value);
        }

        public void FilterEdges()
        {
            for (int y = 0; y < Resolution; y++)
            {
                int row = y * Resolution;
                for (int x = 0; x < Resolution; x++)
                {
                    int left = row + Mathf.Max(0, x - 1);
                    int right = row + Mathf.Min(Resolution - 1, x + 1);
                    filtered[row + x] = (byte)((Pixels[left].r + Pixels[row + x].r * 2 + Pixels[right].r) / 4);
                }
            }

            for (int y = 0; y < Resolution; y++)
            {
                int above = Mathf.Max(0, y - 1) * Resolution;
                int below = Mathf.Min(Resolution - 1, y + 1) * Resolution;
                int row = y * Resolution;
                for (int x = 0; x < Resolution; x++)
                {
                    byte value = (byte)((filtered[above + x] + filtered[row + x] * 2 + filtered[below + x]) / 4);
                    Pixels[row + x] = new Color32(value, value, value, 255);
                }
            }
        }

        private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Triangle(a, b, c);
            Triangle(a, c, d);
        }

        private void Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            int first = Mathf.Max(0, Mathf.CeilToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y)) - .5f));
            int last = Mathf.Min(Resolution - 1, Mathf.FloorToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y)) - .5f));
            for (int y = first; y <= last; y++)
            {
                float left = float.PositiveInfinity;
                float right = float.NegativeInfinity;
                float leftValue = 0;
                float rightValue = 0;
                Intersect(a, b, y + .5f, ref left, ref right, ref leftValue, ref rightValue);
                Intersect(b, c, y + .5f, ref left, ref right, ref leftValue, ref rightValue);
                Intersect(c, a, y + .5f, ref left, ref right, ref leftValue, ref rightValue);
                if (right <= left)
                {
                    continue;
                }

                int start = Mathf.Max(0, Mathf.CeilToInt(left - .5f));
                int end = Mathf.Min(Resolution - 1, Mathf.FloorToInt(right - .5f));
                float inverseWidth = 1 / (right - left);
                for (int x = start; x <= end; x++)
                {
                    byte value = (byte)(255 * Mathf.Clamp01(Mathf.Lerp(leftValue, rightValue, (x + .5f - left) * inverseWidth)));
                    int index = y * Resolution + x;
                    if (value < Pixels[index].r)
                    {
                        Pixels[index] = new Color32(value, value, value, 255);
                    }
                }
            }
        }

        private static void Intersect(Vector3 a, Vector3 b, float row, ref float left, ref float right,
            ref float leftValue, ref float rightValue)
        {
            if ((a.y <= row && b.y > row) || (b.y <= row && a.y > row))
            {
                float fraction = (row - a.y) / (b.y - a.y);
                float x = Mathf.Lerp(a.x, b.x, fraction);
                float value = Mathf.Lerp(a.z, b.z, fraction);
                if (x < left)
                {
                    left = x;
                    leftValue = value;
                }

                if (x > right)
                {
                    right = x;
                    rightValue = value;
                }
            }
        }
    }
}
