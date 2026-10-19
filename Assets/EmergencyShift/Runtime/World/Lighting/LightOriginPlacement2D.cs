using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmergencyShift
{
    public static class LightOriginPlacement2D
    {
        public static Vector2 Clamp(Vector2 anchor, Vector2 desired, Scene scene, float clearance, float retraction, LayerMask obstacles)
        {
            Vector2 direction = (desired - anchor).normalized;
            Vector2 safeAnchor = anchor;
            // A sprite silhouette can extend beyond its simplified movement collider.
            float step = Mathf.Max(clearance, .01f);
            for (float back = 0; back < retraction && InsideAny(safeAnchor, scene); back += step)
            {
                safeAnchor -= direction * step;
            }

            float distance = Vector2.Distance(safeAnchor, desired);
            if (distance < .0001f)
            {
                return safeAnchor;
            }

            float fraction = 1;
            float probeDistance = distance + clearance;
            Vector2 extended = safeAnchor + (desired - safeAnchor).normalized * probeDistance;
            var hit = Physics2D.Linecast(safeAnchor, extended, obstacles);
            if (hit.collider != null)
            {
                fraction = Mathf.Min(fraction, hit.fraction);
            }

            for (int index = 0; index < EnvironmentHeight2D.Active.Count; index++)
            {
                var height = EnvironmentHeight2D.Active[index];
                if (height.gameObject.scene != scene)
                {
                    continue;
                }

                for (int shape = 0; shape < height.FloorContours.Count; shape++)
                {
                    var contour = height.FloorContours[shape];
                    if (contour.Active)
                    {
                        fraction = Mathf.Min(fraction, FirstIntersection(safeAnchor, extended, contour.WorldPoints));
                    }
                }
            }

            if (fraction >= 1)
            {
                return desired;
            }

            return Vector2.MoveTowards(safeAnchor, desired, Mathf.Max(0, probeDistance * fraction - clearance));
        }

        public static bool Contains(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        public static float FirstIntersection(Vector2 start, Vector2 end, Vector2[] polygon)
        {
            if (Contains(start, polygon))
            {
                return 0;
            }

            Vector2 ray = end - start;
            float fraction = 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                Vector2 a = polygon[i];
                Vector2 edge = polygon[(i + 1) % polygon.Length] - a;
                float cross = Cross(ray, edge);
                if (Mathf.Abs(cross) < .000001f)
                {
                    continue;
                }

                float t = Cross(a - start, edge) / cross;
                float u = Cross(a - start, ray) / cross;
                if (t >= 0 && t <= fraction && u >= 0 && u <= 1)
                {
                    fraction = t;
                }
            }

            return fraction;
        }

        private static bool InsideAny(Vector2 point, Scene scene)
        {
            for (int index = 0; index < EnvironmentHeight2D.Active.Count; index++)
            {
                var height = EnvironmentHeight2D.Active[index];
                if (height.gameObject.scene != scene)
                {
                    continue;
                }

                for (int shape = 0; shape < height.FloorContours.Count; shape++)
                {
                    var contour = height.FloorContours[shape];
                    if (contour.Active && Contains(point, contour.WorldPoints))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    }
}
