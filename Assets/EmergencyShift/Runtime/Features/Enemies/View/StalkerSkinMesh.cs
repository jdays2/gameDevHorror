using System.Collections.Generic;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.View
{
    /// <summary>Connected skin parameterized around each knee without crossing its triangles.</summary>
    internal sealed class StalkerSkinMesh
    {
        private const int AngularSamples = 64;
        private const int RadialSamples = 10;
        private const float RigidSector = 10 * Mathf.Deg2Rad;
        private const float FullTurn = 2 * Mathf.PI;
        private readonly StalkerRigProfile profile;
        private readonly Vector3[] vertices;
        private readonly Color[] colors;
        private readonly float[] angles;
        private readonly float[] radii;
        private readonly int[] starts = new int[7];
        private readonly float[] sourceSectors = new float[6];

        public Mesh Mesh { get; }

        public StalkerSkinMesh(StalkerRigProfile profile)
        {
            this.profile = profile;
            var points = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            var polarAngles = new List<float>();
            var polarRadii = new List<float>();
            for (int leg = 0; leg < 6; leg++)
            {
                starts[leg] = points.Count;
                StalkerLimb limb = profile.Limbs[leg];
                int layer = leg * 2 + 1;
                Rect pixels = profile.LayerBounds[layer];
                var bounds = Rect.MinMaxRect((pixels.xMin - profile.Pivot.x) / profile.PixelsPerUnit,
                    (profile.Pivot.y - pixels.yMax) / profile.PixelsPerUnit,
                    (pixels.xMax - profile.Pivot.x) / profile.PixelsPerUnit,
                    (profile.Pivot.y - pixels.yMin) / profile.PixelsPerUnit);
                float upperAngle = Angle(limb.Hip - limb.Knee);
                float sector = Mathf.Repeat(Angle(limb.Tip - limb.Knee) - upperAngle, FullTurn);
                sourceSectors[leg] = sector;
                var rays = new List<float> { sector };
                for (int ray = 0; ray < AngularSamples; ray++)
                {
                    rays.Add(FullTurn * ray / AngularSamples);
                }
                rays.Sort();
                AddPoint(limb.Knee, 0, 0);
                for (int ring = 1; ring <= RadialSamples; ring++)
                {
                    foreach (float angle in rays)
                    {
                        Vector2 direction = Direction(upperAngle + angle);
                        float horizontal = direction.x > 0 ? bounds.xMax - limb.Knee.x : bounds.xMin - limb.Knee.x;
                        float vertical = direction.y > 0 ? bounds.yMax - limb.Knee.y : bounds.yMin - limb.Knee.y;
                        float edge = Mathf.Min(Mathf.Abs(horizontal / direction.x), Mathf.Abs(vertical / direction.y));
                        float radius = edge * ring / RadialSamples;
                        AddPoint(limb.Knee + direction * radius, angle, radius);
                    }
                }
                for (int ray = 0; ray < rays.Count; ray++)
                {
                    int next = (ray + 1) % rays.Count;
                    int first = starts[leg] + 1;
                    AddTriangle(starts[leg], first + next, first + ray);
                    for (int ring = 1; ring < RadialSamples; ring++)
                    {
                        int inner = first + (ring - 1) * rays.Count;
                        int outer = inner + rays.Count;
                        AddTriangle(inner + ray, inner + next, outer + next);
                        AddTriangle(inner + ray, outer + next, outer + ray);
                    }
                }

                void AddPoint(Vector2 point, float angle, float radius)
                {
                    points.Add(point);
                    polarAngles.Add(angle);
                    polarRadii.Add(radius);
                    float pixelX = point.x * profile.PixelsPerUnit + profile.Pivot.x;
                    float pixelY = profile.Pivot.y - point.y * profile.PixelsPerUnit;
                    uv.Add(new Vector2((layer % 4 * profile.CellSize.x + pixelX) / profile.Atlas.width,
                        1 - (layer / 4 * profile.CellSize.y + pixelY) / profile.Atlas.height));
                }

                void AddTriangle(int a, int b, int c)
                {
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(c);
                }
            }
            starts[6] = points.Count;
            vertices = points.ToArray();
            colors = new Color[vertices.Length];
            angles = polarAngles.ToArray();
            radii = polarRadii.ToArray();
            Mesh = new Mesh { name = "Stalker six continuous leg surfaces" };
            Mesh.MarkDynamic();
            Mesh.vertices = vertices;
            Mesh.SetUVs(0, uv);
            Mesh.SetTriangles(triangles, 0);
        }

        public void Update(StalkerGaitModel gait, Transform visual, Color tint)
        {
            for (int leg = 0; leg < 6; leg++)
            {
                StalkerLimb limb = profile.Limbs[leg];
                Vector2 hip = visual.InverseTransformPoint(gait.Hips[leg]);
                Vector2 knee = visual.InverseTransformPoint(gait.Knees[leg]);
                Vector2 foot = visual.InverseTransformPoint(gait.Feet[leg]);
                float upperScale = Vector2.Distance(hip, knee) / Vector2.Distance(limb.Hip, limb.Knee);
                float lowerScale = Vector2.Distance(knee, foot) / Vector2.Distance(limb.Knee, limb.Tip);
                float upperAngle = Angle(hip - knee);
                float sector = Mathf.Repeat(Angle(foot - knee) - upperAngle, FullTurn);
                float sourceSector = sourceSectors[leg];
                float reserve = Mathf.Min(RigidSector, Mathf.Min(sector, FullTurn - sector) * .25f);
                for (int index = starts[leg]; index < starts[leg + 1]; index++)
                {
                    float angle = angles[index];
                    float mapped;
                    float weight;
                    if (angle <= sourceSector)
                    {
                        mapped = MapSector(angle, sourceSector, sector, reserve);
                        weight = angle / sourceSector;
                    }
                    else
                    {
                        mapped = sector + MapSector(angle - sourceSector, FullTurn - sourceSector, FullTurn - sector, reserve);
                        weight = 1 - (angle - sourceSector) / (FullTurn - sourceSector);
                    }
                    float radius = radii[index] * Mathf.Lerp(upperScale, lowerScale, Mathf.SmoothStep(0, 1, weight));
                    vertices[index] = knee + Direction(upperAngle + mapped) * radius;
                    colors[index] = tint;
                }
            }
            Mesh.vertices = vertices;
            Mesh.colors = colors;
            Mesh.RecalculateBounds();
        }

        private static float MapSector(float angle, float source, float target, float reserve)
        {
            if (angle <= reserve)
            {
                return angle;
            }
            if (angle >= source - reserve)
            {
                return target - source + angle;
            }
            return reserve + (angle - reserve) * (target - 2 * reserve) / (source - 2 * reserve);
        }

        private static float Angle(Vector2 value) => Mathf.Atan2(value.y, value.x);
        private static Vector2 Direction(float angle) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }
}
