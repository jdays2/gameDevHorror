using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Editor
{
    /// <summary>
    /// Temporary textured geometry for the offline Sleeper baker. This is not a runtime
    /// component, a pixel processor or an approved anatomical asset. Input portraits use
    /// top-left pixel coordinates; the baker supplies their prepared straight-alpha cutouts.
    /// </summary>
    public sealed class SleeperJawBakeSource : IDisposable
    {
        private const int SourceWidth = 1024;
        private const int SourceHeight = 1536;
        private const float SourceScale = .00155f;
        private const float JawRadius = .18f;
        private const float MaximumAngle = 110f;
        private const float EyeWidth = .24f;
        private const float JawZ = -.035f;
        private const int MovingSortingOrder = 20;
        private static readonly Vector2 BodyPixel = new Vector2(512f, 730f);
        private static readonly Vector2 MouthPixel = new Vector2(512f, 150f);
        private static readonly Vector2 EyePixel = new Vector2(512f, 350f);
        private static readonly float[] HingeAngles = { -90f, 30f, 150f };
        private readonly List<Mesh> ownedMeshes = new List<Mesh>();
        private readonly List<Material> ownedMaterials = new List<Material>();
        private readonly List<JawPanel> panels = new List<JawPanel>(3);
        private readonly Sprite[] eyeFrames;
        private GameObject ownedRoot;
        private SpriteRenderer eye;
        private Mesh pupil;
        private MeshRenderer pupilRenderer;
        private bool disposed;

        /// <param name="eyes">Existing Stalker frames in Open, Half, Slit, Shut order.</param>
        public SleeperJawBakeSource(Transform root, Texture2D closed, Texture2D clean,
            Texture2D open, Sprite[] eyes)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            ValidatePortrait(closed, nameof(closed));
            ValidatePortrait(clean, nameof(clean));
            ValidatePortrait(open, nameof(open));
            if (eyes == null || eyes.Length != 4)
            {
                throw new ArgumentException("Four eye sprites are required: Open, Half, Slit, Shut.", nameof(eyes));
            }

            for (int i = 0; i < eyes.Length; i++)
            {
                if (eyes[i] == null || eyes[i].bounds.size.x <= 0f)
                {
                    throw new ArgumentException("Every eye frame must contain a valid sprite.", nameof(eyes));
                }
            }

            eyeFrames = (Sprite[])eyes.Clone();
            try
            {
                ownedRoot = new GameObject("Sleeper jaw bake source");
                ownedRoot.hideFlags = HideFlags.HideAndDontSave;
                ownedRoot.layer = root.gameObject.layer;
                if (root.gameObject.scene.IsValid() && ownedRoot.scene != root.gameObject.scene)
                {
                    SceneManager.MoveGameObjectToScene(ownedRoot, root.gameObject.scene);
                }

                ownedRoot.transform.SetParent(root, false);
                Material bodyMaterial = CreateMaterial(closed, "Sleeper native neck");
                Material outerMaterial = CreateMaterial(clean, "Sleeper clean outer membrane");
                Material innerMaterial = CreateMaterial(open, "Sleeper native inner tissue and teeth");
                CreateNeck(bodyMaterial, closed);
                CreateRim(outerMaterial, clean);
                CreateCavity(innerMaterial, open);
                CreatePanels(outerMaterial, clean, innerMaterial, open);
                CreateEye(outerMaterial, clean, innerMaterial, open);
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
                throw new ArgumentException("Jaw and eye progress must be finite.");
            }

            float angle = Mathf.Clamp01(jawOpen01) * MaximumAngle * Mathf.Deg2Rad;
            foreach (JawPanel panel in panels)
            {
                panel.Apply(angle);
            }

            float eyeProgress = Mathf.Clamp01(eyeOpen01);
            int frame = EyeFrame(eyeProgress);
            eye.sprite = eyeFrames[frame];
            float scale = EyeWidth / eyeFrames[0].bounds.size.x;
            eye.transform.localScale = Vector3.one * scale;
            Vector3 frameCenter = eye.sprite.bounds.center * scale;
            eye.transform.localPosition = SourcePoint(EyePixel, -.045f) - frameCenter;
            pupilRenderer.enabled = frame < 2;
            float pupilHeight = frame == 0 ? .023f : .009f;
            UpdatePupil(pupilHeight);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            if (ownedRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(ownedRoot);
            }

            foreach (Mesh mesh in ownedMeshes)
            {
                if (mesh != null)
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
            }

            foreach (Material material in ownedMaterials)
            {
                if (material != null)
                {
                    UnityEngine.Object.DestroyImmediate(material);
                }
            }

            panels.Clear();
            ownedMeshes.Clear();
            ownedMaterials.Clear();
        }

        private static void ValidatePortrait(Texture2D texture, string argument)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(argument);
            }

            if (texture.width != SourceWidth || texture.height != SourceHeight)
            {
                throw new ArgumentException("The native portrait must be 1024 by 1536.", argument);
            }
        }

        private Material CreateMaterial(Texture2D texture, string name)
        {
            // Both built-in unlit alpha shaders are double-sided. Do not use a lit/culling
            // fallback: the inner faces rotate beyond ninety degrees during the bake.
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("UI/Default");
            }

            if (shader == null)
            {
                throw new InvalidOperationException("No supported double-sided unlit alpha shader is available.");
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

            ownedMaterials.Add(material);
            return material;
        }

        private Mesh CreateMesh(string name, Vector3[] vertices, Vector2[] pixels,
            int[] triangles, Material material, Texture2D texture, int sortingOrder,
            out MeshRenderer renderer)
        {
            var child = new GameObject(name);
            child.hideFlags = HideFlags.HideAndDontSave;
            child.transform.SetParent(ownedRoot.transform, false);
            child.layer = ownedRoot.layer;
            var mesh = new Mesh();
            mesh.name = name;
            mesh.hideFlags = HideFlags.HideAndDontSave;
            mesh.vertices = vertices;
            var uv = new Vector2[pixels.Length];
            var colors = new Color32[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                uv[i] = TexturePoint(pixels[i], texture);
                colors[i] = new Color32(255, 255, 255, 255);
            }

            mesh.uv = uv;
            mesh.colors32 = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            ownedMeshes.Add(mesh);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return mesh;
        }

        private void CreateNeck(Material material, Texture2D texture)
        {
            Vector3 mouth = SourcePoint(MouthPixel, -.016f);
            Vector3 socket = SourcePoint(EyePixel, -.016f);
            Vector3[] vertices =
            {
                mouth + new Vector3(0f, -.025f, 0f),
                socket + new Vector3(.135f, -.065f, 0f),
                socket + new Vector3(-.135f, -.065f, 0f)
            };
            Vector2[] uv = { new Vector2(510f, 435f), new Vector2(588f, 526f), new Vector2(438f, 526f) };
            CreateMesh("Tapered native neck bridge", vertices, uv, new[] { 0, 1, 2 }, material, texture, 3, out _);
        }

        private void CreateRim(Material material, Texture2D texture)
        {
            const int segments = 48;
            var vertices = new Vector3[segments * 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[segments * 6];
            Vector3 center = SourcePoint(MouthPixel, -.022f);
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float support = 0f;
                foreach (float hingeAngle in HingeAngles)
                {
                    Vector2 normal = Direction(hingeAngle);
                    support = Mathf.Max(support, Vector2.Dot(normal, direction));
                }

                float innerRadius = JawRadius * .5f / support;
                Vector2 inner = direction * innerRadius * .99f;
                // Rear edge stops at source y approximately 266, ahead of the small eye.
                var outer = new Vector2(direction.x * .215f, direction.y * .183f + .003f);
                vertices[i * 2] = center + (Vector3)inner;
                vertices[i * 2 + 1] = center + (Vector3)outer;
                uv[i * 2] = new Vector2(440f + direction.x * 25f, 182f + direction.y * 44f);
                uv[i * 2 + 1] = new Vector2(440f + direction.x * 35f, 182f + direction.y * 65f);
                int next = (i + 1) % segments;
                int offset = i * 6;
                triangles[offset] = i * 2;
                triangles[offset + 1] = i * 2 + 1;
                triangles[offset + 2] = next * 2;
                triangles[offset + 3] = next * 2;
                triangles[offset + 4] = i * 2 + 1;
                triangles[offset + 5] = next * 2 + 1;
            }

            CreateMesh("Fixed rounded tissue rim", vertices, uv, triangles, material, texture, 5, out _);
        }

        private void CreateCavity(Material material, Texture2D texture)
        {
            Vector3 center = SourcePoint(MouthPixel, -.019f);
            Vector3[] vertices =
            {
                center + new Vector3(0f, JawRadius, 0f),
                center + new Vector3(JawRadius * .8660254f, -JawRadius * .5f, 0f),
                center + new Vector3(-JawRadius * .8660254f, -JawRadius * .5f, 0f)
            };
            Vector2[] uv = { new Vector2(495f, 160f), new Vector2(530f, 165f), new Vector2(511f, 226f) };
            CreateMesh("Short native mouth cavity", vertices, uv, new[] { 0, 1, 2 }, material, texture, 4, out _);
        }

        private void CreatePanels(Material outerMaterial, Texture2D clean, Material innerMaterial, Texture2D open)
        {
            float halfWidth = JawRadius * .8660254f;
            float depth = JawRadius * .5f;
            ToothOutline[][] teeth = ToothOutlines();
            for (int i = 0; i < HingeAngles.Length; i++)
            {
                Vector2 normal = Direction(HingeAngles[i]);
                var panel = new JawPanel(SourcePoint(MouthPixel, JawZ), normal, JawRadius * .5f);
                Vector3[] outer =
                {
                    new Vector3(-halfWidth, 0f, .001f),
                    new Vector3(halfWidth, 0f, .001f),
                    new Vector3(0f, depth, .001f)
                };
                Vector2[] outerUv =
                {
                    new Vector2(400f + i * 4f, 171f),
                    new Vector2(474f + i * 3f, 180f),
                    new Vector2(442f + i * 2f, 244f)
                };
                Mesh outside = CreateMesh("Jaw " + i + " outer", new Vector3[3], outerUv,
                    new[] { 0, 1, 2 }, outerMaterial, clean, MovingSortingOrder, out _);
                panel.Add(outside, outer);
                Vector3[] inner =
                {
                    new Vector3(-halfWidth, 0f, -.001f),
                    new Vector3(halfWidth, 0f, -.001f),
                    new Vector3(0f, depth, -.001f)
                };
                Vector2[] innerUv = { new Vector2(470f, 136f), new Vector2(550f, 140f), new Vector2(512f, 220f) };
                Mesh inside = CreateMesh("Jaw " + i + " inner", new Vector3[3], innerUv,
                    new[] { 0, 1, 2 }, innerMaterial, open, MovingSortingOrder, out _);
                panel.Add(inside, inner);
                for (int tooth = 0; tooth < 2; tooth++)
                {
                    float lateral = tooth == 0 ? -.027f : .027f;
                    AddTooth(panel, teeth[i][tooth], lateral, i, tooth, innerMaterial, open);
                }

                panels.Add(panel);
            }
        }

        private void AddTooth(JawPanel panel, ToothOutline outline, float lateral,
            int panelIndex, int toothIndex, Material material, Texture2D texture)
        {
            Vector2 minimum = outline.Pixels[0];
            Vector2 maximum = minimum;
            foreach (Vector2 point in outline.Pixels)
            {
                minimum = Vector2.Min(minimum, point);
                maximum = Vector2.Max(maximum, point);
            }

            var local = new Vector3[outline.Pixels.Length];
            for (int i = 0; i < local.Length; i++)
            {
                Vector2 pixel = outline.Pixels[i];
                float across = Mathf.InverseLerp(minimum.x, maximum.x, pixel.x) - .5f;
                float along = Mathf.InverseLerp(minimum.y, maximum.y, pixel.y);
                if (outline.TipAtLowY)
                {
                    along = 1f - along;
                }

                local[i] = new Vector3(lateral + across * .026f, .012f + along * .055f,
                    -.004f - along * .015f);
            }

            Mesh mesh = CreateMesh("Jaw " + panelIndex + " native tooth " + toothIndex,
                new Vector3[local.Length], outline.Pixels, TriangleFan(local.Length),
                material, texture, MovingSortingOrder, out _);
            panel.Add(mesh, local);
        }

        private void CreateEye(Material skinMaterial, Texture2D clean, Material pupilMaterial, Texture2D open)
        {
            const int segments = 24;
            Vector3 center = SourcePoint(EyePixel, -.024f);
            var vertices = new Vector3[segments];
            var uv = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                vertices[i] = center + new Vector3(Mathf.Cos(angle) * .16f, Mathf.Sin(angle) * .09f, 0f);
                uv[i] = new Vector2(440f + Mathf.Cos(angle) * 30f, 190f + Mathf.Sin(angle) * 40f);
            }

            CreateMesh("Opaque curved eye socket", vertices, uv, TriangleFan(segments),
                skinMaterial, clean, 6, out _);
            var eyeObject = new GameObject("One small existing-family eye");
            eyeObject.hideFlags = HideFlags.HideAndDontSave;
            eyeObject.layer = ownedRoot.layer;
            eyeObject.transform.SetParent(ownedRoot.transform, false);
            eye = eyeObject.AddComponent<SpriteRenderer>();
            eye.sortingOrder = 25;
            var eyeMaterial = CreateMaterial(eyeFrames[0].texture, "Sleeper existing eye atlas");
            eye.sharedMaterial = eyeMaterial;
            var pupilUv = new Vector2[segments];
            for (int i = 0; i < pupilUv.Length; i++)
            {
                // Sample the existing open portrait's black pupil, never paint a new bitmap.
                pupilUv[i] = new Vector2(512f, 350f);
            }

            pupil = CreateMesh("Native sampled pupil", new Vector3[segments], pupilUv,
                TriangleFan(segments), pupilMaterial, open, 26, out pupilRenderer);
        }

        private void UpdatePupil(float height)
        {
            var vertices = new Vector3[24];
            Vector3 center = SourcePoint(EyePixel, -.047f);
            for (int i = 0; i < vertices.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / vertices.Length;
                vertices[i] = center + new Vector3(Mathf.Cos(angle) * .023f, Mathf.Sin(angle) * height, 0f);
            }

            pupil.vertices = vertices;
            pupil.RecalculateBounds();
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

        private static int[] TriangleFan(int count)
        {
            var triangles = new int[(count - 2) * 3];
            for (int i = 0; i < count - 2; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            return triangles;
        }

        private static Vector2 Direction(float angleDegrees)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        private static Vector3 SourcePoint(Vector2 point, float z)
        {
            return new Vector3((point.x - BodyPixel.x) * SourceScale,
                (BodyPixel.y - point.y) * SourceScale, z);
        }

        private static Vector2 TexturePoint(Vector2 point, Texture2D texture)
        {
            return new Vector2((point.x + .5f) / texture.width, 1f - (point.y + .5f) / texture.height);
        }

        private static ToothOutline[][] ToothOutlines()
        {
            // Convex native enamel/gum contours; no rectangular samples containing backdrop.
            return new[]
            {
                new[]
                {
                    new ToothOutline(false, new Vector2(460, 58), new Vector2(477, 58), new Vector2(486, 73), new Vector2(492, 102), new Vector2(475, 91), new Vector2(455, 70)),
                    new ToothOutline(false, new Vector2(547, 55), new Vector2(563, 57), new Vector2(571, 72), new Vector2(563, 87), new Vector2(538, 102), new Vector2(538, 78))
                },
                new[]
                {
                    new ToothOutline(true, new Vector2(375, 185), new Vector2(371, 228), new Vector2(365, 260), new Vector2(352, 277), new Vector2(333, 274), new Vector2(329, 258), new Vector2(342, 213)),
                    new ToothOutline(true, new Vector2(435, 217), new Vector2(435, 250), new Vector2(425, 288), new Vector2(410, 297), new Vector2(385, 292), new Vector2(388, 271), new Vector2(409, 235))
                },
                new[]
                {
                    new ToothOutline(true, new Vector2(589, 215), new Vector2(614, 232), new Vector2(636, 267), new Vector2(638, 291), new Vector2(616, 296), new Vector2(600, 282), new Vector2(590, 250)),
                    new ToothOutline(true, new Vector2(649, 185), new Vector2(682, 213), new Vector2(696, 259), new Vector2(691, 275), new Vector2(672, 278), new Vector2(659, 260), new Vector2(653, 228))
                }
            };
        }

        private sealed class ToothOutline
        {
            internal readonly bool TipAtLowY;
            internal readonly Vector2[] Pixels;

            internal ToothOutline(bool tipAtLowY, params Vector2[] pixels)
            {
                TipAtLowY = tipAtLowY;
                Pixels = pixels;
            }
        }

        private sealed class JawPanel
        {
            private readonly Vector3 center;
            private readonly Vector2 normal;
            private readonly Vector2 tangent;
            private readonly float hingeRadius;
            private readonly List<Piece> pieces = new List<Piece>(4);

            internal JawPanel(Vector3 mouthCenter, Vector2 outwardNormal, float radius)
            {
                center = mouthCenter;
                normal = outwardNormal;
                tangent = new Vector2(-normal.y, normal.x);
                hingeRadius = radius;
            }

            internal void Add(Mesh mesh, Vector3[] coordinates)
            {
                pieces.Add(new Piece(mesh, coordinates));
            }

            internal void Apply(float angle)
            {
                float cosine = Mathf.Cos(angle);
                float sine = Mathf.Sin(angle);
                foreach (Piece piece in pieces)
                {
                    for (int i = 0; i < piece.Coordinates.Length; i++)
                    {
                        Vector3 local = piece.Coordinates[i];
                        Vector2 xy = normal * hingeRadius + tangent * local.x;
                        xy -= normal * (local.y * cosine);
                        xy += normal * (local.z * sine);
                        // Negative z is toward the baker camera. Geometry rotates around
                        // the fixed hinge, preserving every length and tooth attachment.
                        float z = -local.y * sine - local.z * cosine;
                        piece.Vertices[i] = center + new Vector3(xy.x, xy.y, z);
                    }

                    piece.Mesh.vertices = piece.Vertices;
                    piece.Mesh.RecalculateBounds();
                }
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
    }
}
