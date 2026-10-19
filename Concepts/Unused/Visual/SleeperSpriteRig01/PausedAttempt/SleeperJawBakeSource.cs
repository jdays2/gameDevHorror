using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Editor
{
    /// <summary>
    /// Offline native-art jaw source. Closed membrane contours retain their original
    /// source positions and UVs. The caller prepares alpha and heals the obsolete front
    /// seam in a copy; this class does not read, paint or replace texture pixels.
    /// </summary>
    public sealed class SleeperJawBakeSource : IDisposable
    {
        private const float SourceScale = .00155f;
        private const float MaximumAngle = 110f;
        private const float SurfaceZ = -.035f;
        private static readonly Vector2 BodyPixel = new Vector2(512f, 730f);
        private static readonly Vector2 EyePixel = new Vector2(512f, 350f);
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Panel> panels = new List<Panel>(3);
        private readonly List<Tether> tethers = new List<Tether>(4);
        private readonly List<MeshRenderer> toothRenderers = new List<MeshRenderer>(6);
        private readonly Sprite[] eyeFrames;
        private GameObject group;
        private SpriteRenderer eye;
        private Material toothMaterial;
        private Material gumMaterial;
        private bool disposed;

        /// <param name="closed">Prepared full bloodied closed portrait, with healed front seam.</param>
        /// <param name="clean">Native clean portrait retained for constructor compatibility.</param>
        /// <param name="open">Prepared full open portrait, source of the six actual fangs.</param>
        /// <param name="eyes">Native cropped eye sprites in Open, Half, Slit, Shut order.</param>
        public SleeperJawBakeSource(Transform root, Texture2D closed, Texture2D clean,
            Texture2D open, Sprite[] eyes)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            ValidateTexture(closed, nameof(closed));
            ValidateTexture(clean, nameof(clean));
            ValidateTexture(open, nameof(open));
            if (eyes == null || eyes.Length != 4)
            {
                throw new ArgumentException("Use four native eye frames: Open, Half, Slit, Shut.", nameof(eyes));
            }

            foreach (Sprite frame in eyes)
            {
                if (frame == null || frame.pixelsPerUnit <= 0f)
                {
                    throw new ArgumentException("Every native eye frame must be valid.", nameof(eyes));
                }
            }

            eyeFrames = (Sprite[])eyes.Clone();
            try
            {
                group = new GameObject("Native Sleeper jaw bake source");
                group.hideFlags = HideFlags.HideAndDontSave;
                group.layer = root.gameObject.layer;
                if (root.gameObject.scene.IsValid() && group.scene != root.gameObject.scene)
                {
                    SceneManager.MoveGameObjectToScene(group, root.gameObject.scene);
                }

                group.transform.SetParent(root, false);
                Material outside = CreateMaterial(closed, "Native closed lobes and collar");
                Material inside = CreateMaterial(open, "Native inner mouth tissue");
                Material cavity = CreateMaterial(open, "Native dark cavity");
                cavity.color = Color.white;
                toothMaterial = CreateMaterial(open, "Six native fangs");
                gumMaterial = CreateMaterial(open, "Native tooth-free oral folds");
                CreateCollar(closed, outside);
                CreatePanels(closed, outside, open, inside, cavity);
                CreateEye();
                Apply(0f, 0f);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Apply(float jawOpen01, float eyeOpen01)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(SleeperJawBakeSource));
            }

            if (!float.IsFinite(jawOpen01) || !float.IsFinite(eyeOpen01))
            {
                throw new ArgumentException("Pose progress must be finite.");
            }

            float progress = Mathf.Clamp01(jawOpen01);
            float angle = progress * MaximumAngle * Mathf.Deg2Rad;
            foreach (Panel panel in panels)
            {
                panel.Apply(angle);
            }

            foreach (Tether tether in tethers)
            {
                tether.Apply(angle);
            }

            float gumReveal = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.05f, .3f, progress));
            gumMaterial.color = new Color(1f, 1f, 1f, gumReveal);

            // Transparent 2D surfaces cannot depth-write the fangs. Reveal their fixed
            // geometry only as the lips part; opacity changes, never tooth dimensions.
            float reveal = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.2f, .5f, progress));
            toothMaterial.color = new Color(1f, 1f, 1f, reveal);
            foreach (MeshRenderer renderer in toothRenderers)
            {
                renderer.enabled = reveal > .001f;
            }

            eye.sprite = eyeFrames[EyeFrame(Mathf.Clamp01(eyeOpen01))];
            // Native sprite pivot is source (512,350), not its crop's bounding center.
            eye.transform.localPosition = SourcePoint(EyePixel, -.05f);
            eye.transform.localScale = Vector3.one * (SourceScale * eye.sprite.pixelsPerUnit);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            if (group != null)
            {
                UnityEngine.Object.DestroyImmediate(group);
            }

            foreach (Mesh mesh in meshes)
            {
                if (mesh != null)
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
            }

            foreach (Material material in materials)
            {
                if (material != null)
                {
                    UnityEngine.Object.DestroyImmediate(material);
                }
            }

            meshes.Clear();
            materials.Clear();
            panels.Clear();
            tethers.Clear();
            toothRenderers.Clear();
        }

        private static void ValidateTexture(Texture2D texture, string argument)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(argument);
            }

            if (texture.width != 1024 || texture.height != 1536)
            {
                throw new ArgumentException("Native portraits must remain 1024 by 1536.", argument);
            }
        }

        private Material CreateMaterial(Texture2D texture, string name)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("UI/Default");
            }

            if (shader == null)
            {
                throw new InvalidOperationException("A double-sided unlit alpha shader is required.");
            }

            var material = new Material(shader);
            material.name = name;
            material.hideFlags = HideFlags.HideAndDontSave;
            material.mainTexture = texture;
            material.color = Color.white;
            material.renderQueue = 3000;
            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", (float)CullMode.Off);
            }

            materials.Add(material);
            return material;
        }

        private Mesh CreateMesh(string name, Vector3[] vertices, Vector2[] sourceUv,
            int[] indices, Texture2D texture, Material material, int order, out MeshRenderer renderer)
        {
            var child = new GameObject(name);
            child.hideFlags = HideFlags.HideAndDontSave;
            child.layer = group.layer;
            child.transform.SetParent(group.transform, false);
            var mesh = new Mesh();
            mesh.name = name;
            mesh.hideFlags = HideFlags.HideAndDontSave;
            var uv = new Vector2[sourceUv.Length];
            var colors = new Color32[sourceUv.Length];
            for (int i = 0; i < uv.Length; i++)
            {
                uv[i] = new Vector2((sourceUv[i].x + .5f) / texture.width,
                    1f - (sourceUv[i].y + .5f) / texture.height);
                colors[i] = new Color32(255, 255, 255, 255);
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.colors32 = colors;
            mesh.triangles = indices;
            mesh.RecalculateBounds();
            meshes.Add(mesh);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return mesh;
        }

        private void CreateCollar(Texture2D closed, Material material)
        {
            Vector2[] polygon = Points(318,285, 349,313, 397,319, 447,290, 482,257,
                512,270, 542,257, 577,290, 627,319, 674,315, 706,285, 724,300,
                710,336, 674,371, 634,402, 598,422, 584,430, 440,430, 426,422,
                390,402, 350,371, 316,336, 300,300);
            CreateMesh("Identity-UV native lower head and collar", WorldPoints(polygon, -.02f),
                polygon, Triangulate(polygon), closed, material, 5, out _);
        }

        private void CreatePanels(Texture2D closed, Material outside, Texture2D open,
            Material inside, Material cavity)
        {
            Vector2[][] outlines =
            {
                Points(512,45, 548,78, 584,120, 626,178, 568,191, 512,220,
                    456,191, 398,178, 438,119, 478,78),
                Points(398,178, 456,191, 512,220, 512,270, 482,257, 447,290, 397,319,
                    349,313, 318,285, 326,242, 359,200),
                Points(626,178, 568,191, 512,220, 512,270, 542,257, 577,290, 627,319,
                    675,313, 706,285, 698,242, 665,200)
            };
            Vector2[] hinges = { new Vector2(512,80), new Vector2(340,250), new Vector2(684,250) };
            Vector2[] normals = { Vector2.up, Vector2.left, Vector2.right };
            // The interior stays inside the hinge envelope. A full duplicate of the
            // closed roof would leave a stationary dark nose when the front lobe lifts.
            Vector2[] cavityControls = Points(512,90, 600,160, 677,250, 620,290,
                512,285, 404,290, 347,250, 424,160);
            Vector2[] cavityOutline = RoundedBoundary(cavityControls, 8);
            CreateMesh("Fixed cavity inside native hinges", WorldPoints(cavityOutline, -.021f),
                InnerUv(cavityOutline), Triangulate(cavityOutline), open, cavity, 4, out _);
            CreateGumRim(cavityOutline, open);
            for (int i = 0; i < outlines.Length; i++)
            {
                Vector2[] polygon = outlines[i];
                int[] indices = Triangulate(polygon);
                Vector2[] innerUv = InnerUv(polygon);
                var panel = new Panel(SourcePoint(hinges[i], SurfaceZ), normals[i]);
                Vector3[] outer = panel.LocalCoordinates(WorldPoints(polygon, SurfaceZ), .001f);
                Mesh outerMesh = CreateMesh("Native organic lobe " + i, new Vector3[polygon.Length],
                    polygon, indices, closed, outside, 35, out MeshRenderer outerRenderer);
                panel.Add(outerMesh, outer);
                Vector3[] inner = panel.LocalCoordinates(WorldPoints(polygon, SurfaceZ), -.001f);
                Mesh innerMesh = CreateMesh("Inner native lobe " + i, new Vector3[polygon.Length],
                    innerUv, indices, open, inside, 35, out MeshRenderer innerRenderer);
                panel.Add(innerMesh, inner);
                panel.SetFaces(outerRenderer, innerRenderer);
                panels.Add(panel);
            }

            CreateTether(panels[0], new Vector2(424,160), new Vector2(456,191), open, "Front left attached fold");
            CreateTether(panels[0], new Vector2(600,160), new Vector2(568,191), open, "Front right attached fold");
            CreateTether(panels[1], new Vector2(360,260), new Vector2(447,290), open, "Left attached fold");
            CreateTether(panels[2], new Vector2(664,260), new Vector2(577,290), open, "Right attached fold");

            AddTooth(panels[0], "front left", Points(460,58, 477,58, 486,73, 492,102, 475,91, 455,70), false,
                new Vector2(493,165), Vector2.up, open);
            AddTooth(panels[0], "front right", Points(547,55, 563,57, 571,72, 563,87, 538,102, 538,78), false,
                new Vector2(531,165), Vector2.up, open);
            AddTooth(panels[1], "left outer", Points(375,185, 371,228, 365,260, 352,277, 333,274, 329,258, 342,213), true,
                new Vector2(430,225), new Vector2(1f,.1f), open);
            AddTooth(panels[1], "left inner", Points(435,217, 435,250, 425,288, 410,297, 385,292, 388,271, 409,235), true,
                new Vector2(444,270), new Vector2(1f,-.5f), open);
            AddTooth(panels[2], "right inner", Points(589,215, 614,232, 636,267, 638,291, 616,296, 600,282, 590,250), true,
                new Vector2(580,270), new Vector2(-1f,-.5f), open);
            AddTooth(panels[2], "right outer", Points(649,185, 682,213, 696,259, 691,275, 672,278, 659,260, 653,228), true,
                new Vector2(594,225), new Vector2(-1f,.1f), open);
        }

        private void AddTooth(Panel panel, string name, Vector2[] source, bool tipAtLowY,
            Vector2 anchor, Vector2 inwardPixels, Texture2D texture)
        {
            Vector2 minimum = source[0];
            Vector2 maximum = source[0];
            foreach (Vector2 point in source)
            {
                minimum = Vector2.Min(minimum, point);
                maximum = Vector2.Max(maximum, point);
            }

            Vector2 inward = inwardPixels.normalized;
            Vector2 across = new Vector2(-inward.y, inward.x);
            var world = new Vector3[source.Length];
            var offsets = new float[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                float along = source[i].y - minimum.y;
                if (tipAtLowY)
                {
                    along = maximum.y - source[i].y;
                }

                float lateral = source[i].x - (minimum.x + maximum.x) * .5f;
                Vector2 target = anchor + inward * (along * .68f) + across * (lateral * .68f);
                world[i] = SourcePoint(target, SurfaceZ);
                offsets[i] = -.004f - Mathf.InverseLerp(0f, maximum.y - minimum.y, along) * .022f;
            }

            Vector3[] local = panel.LocalCoordinates(world, 0f);
            for (int i = 0; i < local.Length; i++)
            {
                local[i].z = offsets[i];
            }

            Mesh mesh = CreateMesh("Native fang " + name, new Vector3[source.Length], source,
                Triangulate(source), texture, toothMaterial, 38, out MeshRenderer renderer);
            toothRenderers.Add(renderer);
            panel.Add(mesh, local);
        }

        private void CreateGumRim(Vector2[] boundary, Texture2D texture)
        {
            var vertices = new Vector3[boundary.Length * 2];
            var uv = new Vector2[vertices.Length];
            var indices = new int[boundary.Length * 6];
            for (int i = 0; i < boundary.Length; i++)
            {
                Vector2 radial = (boundary[i] - new Vector2(512,210)).normalized;
                vertices[i * 2] = SourcePoint(boundary[i] - radial * 5f, -.026f);
                vertices[i * 2 + 1] = SourcePoint(boundary[i] + radial * 5f, -.026f);
                float phase = i * Mathf.PI * 2f / boundary.Length;
                // This entire UV strip is x438..473,y130..200: outside all six
                // source fang polygons. It cannot introduce painted duplicate teeth.
                float fold = (Mathf.Sin(phase) + 1f) * .5f;
                Vector2 native = Vector2.Lerp(new Vector2(442,136), new Vector2(469,193), fold);
                uv[i * 2] = native + new Vector2(-2f, 1f);
                uv[i * 2 + 1] = native + new Vector2(2f, -1f);
                int next = (i + 1) % boundary.Length;
                int at = i * 6;
                indices[at] = i * 2;
                indices[at + 1] = i * 2 + 1;
                indices[at + 2] = next * 2;
                indices[at + 3] = next * 2;
                indices[at + 4] = i * 2 + 1;
                indices[at + 5] = next * 2 + 1;
            }

            CreateMesh("Thin native gingival border", vertices, uv, indices, texture,
                gumMaterial, 32, out _);
        }

        private void CreateTether(Panel panel, Vector2 fixedBase, Vector2 movingLip,
            Texture2D texture, string name)
        {
            const int segments = 12;
            var uv = new Vector2[(segments + 1) * 2];
            var indices = new int[segments * 6];
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                uv[i * 2] = Vector2.Lerp(new Vector2(438,132), new Vector2(462,197), t);
                uv[i * 2 + 1] = Vector2.Lerp(new Vector2(444,133), new Vector2(470,194), t);
                if (i == segments)
                {
                    continue;
                }

                int at = i * 6;
                indices[at] = i * 2;
                indices[at + 1] = i * 2 + 1;
                indices[at + 2] = i * 2 + 2;
                indices[at + 3] = i * 2 + 2;
                indices[at + 4] = i * 2 + 1;
                indices[at + 5] = i * 2 + 3;
            }

            Mesh mesh = CreateMesh(name, new Vector3[uv.Length], uv, indices, texture,
                gumMaterial, 33, out _);
            tethers.Add(new Tether(mesh, panel, SourcePoint(fixedBase, -.028f), movingLip));
        }

        private static Vector2[] RoundedBoundary(Vector2[] controls, int subdivisions)
        {
            var result = new Vector2[controls.Length * subdivisions];
            for (int i = 0; i < controls.Length; i++)
            {
                Vector2 a = controls[(i + controls.Length - 1) % controls.Length];
                Vector2 b = controls[i];
                Vector2 c = controls[(i + 1) % controls.Length];
                Vector2 d = controls[(i + 2) % controls.Length];
                for (int j = 0; j < subdivisions; j++)
                {
                    float t = j / (float)subdivisions;
                    result[i * subdivisions + j] = .5f * ((2f * b) + (-a + c) * t
                        + (2f * a - 5f * b + 4f * c - d) * t * t
                        + (-a + 3f * b - 3f * c + d) * t * t * t);
                }
            }

            return result;
        }

        private void CreateEye()
        {
            var child = new GameObject("One native eye, fixed original socket");
            child.hideFlags = HideFlags.HideAndDontSave;
            child.layer = group.layer;
            child.transform.SetParent(group.transform, false);
            eye = child.AddComponent<SpriteRenderer>();
            eye.sortingOrder = 45;
            eye.sharedMaterial = CreateMaterial(eyeFrames[0].texture, "Native eye frames");
        }

        private static int EyeFrame(float progress)
        {
            if (progress >= .75f)
            {
                return 0;
            }

            if (progress >= .4f)
            {
                return 1;
            }

            if (progress >= .15f)
            {
                return 2;
            }

            return 3;
        }

        private static Vector2[] InnerUv(Vector2[] polygon)
        {
            var result = new Vector2[polygon.Length];
            for (int i = 0; i < result.Length; i++)
            {
                float x = Mathf.InverseLerp(318f, 706f, polygon[i].x);
                float y = Mathf.InverseLerp(45f, 319f, polygon[i].y);
                result[i] = new Vector2(Mathf.Lerp(454f, 570f, x), Mathf.Lerp(125f, 275f, y));
            }

            return result;
        }

        private static Vector2[] Points(params float[] values)
        {
            var points = new Vector2[values.Length / 2];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Vector2(values[i * 2], values[i * 2 + 1]);
            }

            return points;
        }

        private static Vector3 SourcePoint(Vector2 pixel, float z)
        {
            return new Vector3((pixel.x - BodyPixel.x) * SourceScale,
                (BodyPixel.y - pixel.y) * SourceScale, z);
        }

        private static Vector3[] WorldPoints(Vector2[] pixels, float z)
        {
            var points = new Vector3[pixels.Length];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = SourcePoint(pixels[i], z);
            }

            return points;
        }

        private static int[] Triangulate(Vector2[] points)
        {
            float area = 0f;
            for (int i = 0; i < points.Length; i++)
            {
                area += Cross(points[i], points[(i + 1) % points.Length]);
            }

            float orientation = Mathf.Sign(area);
            var remaining = new List<int>(points.Length);
            var result = new List<int>((points.Length - 2) * 3);
            for (int i = 0; i < points.Length; i++)
            {
                remaining.Add(i);
            }

            while (remaining.Count > 2)
            {
                bool clipped = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    int a = remaining[(i + remaining.Count - 1) % remaining.Count];
                    int b = remaining[i];
                    int c = remaining[(i + 1) % remaining.Count];
                    if (Cross(points[b] - points[a], points[c] - points[b]) * orientation <= .0001f)
                    {
                        continue;
                    }

                    bool containsPoint = false;
                    foreach (int candidate in remaining)
                    {
                        if (candidate == a || candidate == b || candidate == c)
                        {
                            continue;
                        }

                        if (Inside(points[candidate], points[a], points[b], points[c], orientation))
                        {
                            containsPoint = true;
                            break;
                        }
                    }

                    if (containsPoint)
                    {
                        continue;
                    }

                    result.Add(a);
                    result.Add(b);
                    result.Add(c);
                    remaining.RemoveAt(i);
                    clipped = true;
                    break;
                }

                if (!clipped)
                {
                    throw new InvalidOperationException("The native anatomical polygon cannot be triangulated.");
                }
            }

            return result.ToArray();
        }

        private static bool Inside(Vector2 p, Vector2 a, Vector2 b, Vector2 c, float orientation)
        {
            return Cross(b - a, p - a) * orientation >= -.0001f
                && Cross(c - b, p - b) * orientation >= -.0001f
                && Cross(a - c, p - c) * orientation >= -.0001f;
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        private sealed class Panel
        {
            private readonly Vector3 hinge;
            private readonly Vector2 normal;
            private readonly Vector2 tangent;
            private readonly List<Piece> pieces = new List<Piece>(4);
            private MeshRenderer outerFace;
            private MeshRenderer innerFace;

            internal Panel(Vector3 hingePosition, Vector2 outwardNormal)
            {
                hinge = hingePosition;
                normal = outwardNormal;
                tangent = new Vector2(-normal.y, normal.x);
            }

            internal Vector3[] LocalCoordinates(Vector3[] positions, float thickness)
            {
                var result = new Vector3[positions.Length];
                for (int i = 0; i < result.Length; i++)
                {
                    Vector2 delta = positions[i] - hinge;
                    result[i] = new Vector3(Vector2.Dot(delta, tangent),
                        -Vector2.Dot(delta, normal), thickness);
                }

                return result;
            }

            internal void Add(Mesh mesh, Vector3[] coordinates)
            {
                pieces.Add(new Piece(mesh, coordinates));
            }

            internal void SetFaces(MeshRenderer outside, MeshRenderer inside)
            {
                outerFace = outside;
                innerFace = inside;
            }

            internal Vector3 AtSource(Vector2 source, float angle)
            {
                Vector2 delta = SourcePoint(source, SurfaceZ) - hinge;
                var local = new Vector3(Vector2.Dot(delta, tangent), -Vector2.Dot(delta, normal), -.008f);
                return Pose(local, Mathf.Cos(angle), Mathf.Sin(angle));
            }

            internal void Apply(float angle)
            {
                float cosine = Mathf.Cos(angle);
                float sine = Mathf.Sin(angle);
                outerFace.enabled = cosine >= 0f;
                innerFace.enabled = cosine < 0f;
                foreach (Piece piece in pieces)
                {
                    for (int i = 0; i < piece.Coordinates.Length; i++)
                    {
                        Vector3 local = piece.Coordinates[i];
                        piece.Vertices[i] = Pose(local, cosine, sine);
                    }

                    piece.Mesh.vertices = piece.Vertices;
                    piece.Mesh.RecalculateBounds();
                }
            }

            private Vector3 Pose(Vector3 local, float cosine, float sine)
            {
                Vector2 xy = tangent * local.x - normal * (local.y * cosine);
                xy += normal * (local.z * sine);
                float z = -local.y * sine - local.z * cosine;
                return hinge + new Vector3(xy.x, xy.y, z);
            }

            private sealed class Piece
            {
                internal readonly Mesh Mesh;
                internal readonly Vector3[] Coordinates;
                internal readonly Vector3[] Vertices;

                internal Piece(Mesh mesh, Vector3[] coordinates)
                {
                    Mesh = mesh;
                    Coordinates = coordinates;
                    Vertices = new Vector3[coordinates.Length];
                }
            }
        }

        private sealed class Tether
        {
            private readonly Mesh mesh;
            private readonly Panel panel;
            private readonly Vector3 fixedBase;
            private readonly Vector2 movingLip;
            private readonly Vector3[] vertices = new Vector3[26];

            internal Tether(Mesh target, Panel movingPanel, Vector3 anchor, Vector2 lip)
            {
                mesh = target;
                panel = movingPanel;
                fixedBase = anchor;
                movingLip = lip;
            }

            internal void Apply(float angle)
            {
                Vector3 end = panel.AtSource(movingLip, angle);
                Vector3 delta = end - fixedBase;
                Vector2 transverse = new Vector2(-delta.y, delta.x).normalized;
                if (transverse.sqrMagnitude < .001f)
                {
                    transverse = Vector2.right;
                }

                for (int i = 0; i <= 12; i++)
                {
                    float t = i / 12f;
                    float bow = Mathf.Sin(t * Mathf.PI);
                    Vector3 center = Vector3.Lerp(fixedBase, end, t);
                    center += new Vector3(transverse.x * .008f * bow, transverse.y * .008f * bow, -.008f * bow);
                    float halfWidth = .012f * (1f - .2f * bow);
                    Vector3 side = new Vector3(transverse.x, transverse.y, 0f) * halfWidth;
                    vertices[i * 2] = center - side;
                    vertices[i * 2 + 1] = center + side;
                }

                mesh.vertices = vertices;
                mesh.RecalculateBounds();
            }
        }
    }
}
