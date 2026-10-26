using System.Collections.Generic;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.View
{
    [DefaultExecutionOrder(25), DisallowMultipleComponent]
    public sealed class StalkerLimbRig2D : MonoBehaviour
    {
        [SerializeField] private StalkerRigProfile profile;
        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer iris;
        [SerializeField] private SpriteRenderer pupil;
        [SerializeField] private SpriteRenderer backing;
        [SerializeField] private SpriteRenderer contact;
        [SerializeField] private SpriteRenderer projection;
        private EnemyPresentationSource2D presentation;
        private StalkerGaitModel gait;
        private Mesh mesh;
        private MeshRenderer surface;
        private MeshRenderer contactMesh;
        private MeshRenderer projectionMesh;
        private readonly Vector3[] vertices = new Vector3[48];
        private readonly Vector3[] restVertices = new Vector3[48];
        private readonly Color[] colors = new Color[48];
        private readonly Dictionary<Sprite, Sprite> torsoCrops = new Dictionary<Sprite, Sprite>();
        private MaterialPropertyBlock properties;
        private Vector3 eyeOffset;
        private Vector3 restingBodyPosition;
        private static readonly int TextureId = Shader.PropertyToID("_MainTex");
        public StalkerGaitModel Gait => gait;

        private void Awake()
        {
            profile.Validate();
            presentation = GetComponent<StalkerPresentationSource2D>();
            float scale = Mathf.Abs(transform.lossyScale.x);
            gait = new StalkerGaitModel(profile.Limbs, scale, profile.CycleDistance,
                profile.SupportFraction, profile.LiftHeight, profile.BodyHeight, profile.WeightTransfer,
                (float)new System.Random(System.Guid.NewGuid().GetHashCode()).NextDouble());
            restingBodyPosition = body.transform.localPosition;
            properties = new MaterialPropertyBlock();
            BuildMesh();
            surface = CreateRenderer("Stalker articulated skin", visual, profile.SurfaceMaterial, body.sortingOrder - 1);
            contactMesh = CreateRenderer("Stalker limb contact shadow", contact.transform.parent, profile.ShadowMaterial, contact.sortingOrder);
            projectionMesh = CreateRenderer("Stalker limb beam shadow", projection.transform.parent, profile.ShadowMaterial, projection.sortingOrder);
        }

        public void RemoveEyeOffset()
        {
            if (iris != null)
            {
                iris.transform.localPosition -= eyeOffset;
            }
            if (pupil != null)
            {
                pupil.transform.localPosition -= eyeOffset;
            }
            if (backing != null)
            {
                backing.transform.localPosition -= eyeOffset;
            }
            eyeOffset = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (presentation == null || gait == null)
            {
                return;
            }
            bool alive = !presentation.IsDead && body.enabled;
            surface.enabled = alive;
            if (!alive)
            {
                body.transform.localPosition = restingBodyPosition;
                return;
            }
            bool allowSteps = !presentation.IsStaggered && !presentation.IsAttacking;
            gait.Step(transform.position, visual.eulerAngles.z, Time.deltaTime, allowSteps, presentation.Closure);
            RenderPose();
            // Every walking/idle pose uses the exact same torso paint. Existing
            // mouth and lid poses remain authored sprites, cropped to their torso.
            if (presentation.IsAttacking || presentation.Closure >= .05f)
            {
                body.sprite = TorsoCrop(body.sprite);
            }
            else
            {
                body.sprite = profile.Torso;
            }
            Vector3 localOffset = visual.InverseTransformVector(gait.BodyOffset);
            body.transform.localPosition = restingBodyPosition + localOffset;
            eyeOffset = localOffset;
            if (iris != null)
            {
                iris.transform.localPosition += eyeOffset;
            }
            if (pupil != null)
            {
                pupil.transform.localPosition += eyeOffset;
            }
            if (backing != null)
            {
                backing.transform.localPosition += eyeOffset;
            }
        }

        public void RenderPose()
        {
            for (int i = 0; i < 12; i++)
            {
                int leg = i / 2;
                StalkerLimb limb = profile.Limbs[leg];
                Vector2 sourceA = i % 2 == 0 ? limb.Hip : limb.Knee;
                Vector2 sourceB = i % 2 == 0 ? limb.Knee : limb.Tip;
                Vector2 targetA = visual.InverseTransformPoint(i % 2 == 0 ? gait.Hips[leg] : gait.Knees[leg]);
                Vector2 targetB = visual.InverseTransformPoint(i % 2 == 0 ? gait.Knees[leg] : gait.Feet[leg]);
                Vector2 sourceAxis = (sourceB - sourceA).normalized;
                Vector2 targetAxis = (targetB - targetA).normalized;
                float foreshortening = Vector2.Distance(targetA, targetB) / Vector2.Distance(sourceA, sourceB);
                for (int j = 0; j < 4; j++)
                {
                    int index = i * 4 + j;
                    Vector2 relative = (Vector2)restVertices[index] - sourceA;
                    float along = Vector2.Dot(relative, sourceAxis);
                    float across = Vector2.Dot(relative, new Vector2(-sourceAxis.y, sourceAxis.x));
                    vertices[index] = targetA + targetAxis * (along * foreshortening) + new Vector2(-targetAxis.y, targetAxis.x) * across;
                    colors[index] = body.color;
                }
            }
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.RecalculateBounds();
            surface.GetPropertyBlock(properties);
            properties.SetTexture(TextureId, profile.Atlas);
            surface.SetPropertyBlock(properties);
        }

        public void SyncShadows()
        {
            if (gait == null)
            {
                return;
            }
            SyncShadow(contactMesh, contact);
            SyncShadow(projectionMesh, projection);
        }

        private void SyncShadow(MeshRenderer target, SpriteRenderer source)
        {
            target.enabled = surface.enabled && source.enabled;
            target.transform.position = source.transform.position - visual.TransformVector(body.transform.localPosition - restingBodyPosition);
            target.transform.rotation = source.transform.rotation;
            target.transform.localScale = source.transform.localScale;
            source.GetPropertyBlock(properties);
            properties.SetTexture(TextureId, profile.Atlas);
            // The mesh has skin vertex tint; the shadow shader supplies only alpha.
            properties.SetColor("_Color", source.color);
            target.SetPropertyBlock(properties);
        }

        private Sprite TorsoCrop(Sprite source)
        {
            if (source == null || source == profile.Torso)
            {
                return profile.Torso;
            }
            if (torsoCrops.TryGetValue(source, out Sprite cached))
            {
                return cached;
            }
            float ppu = source.pixelsPerUnit;
            Rect original = source.rect;
            Vector2 center = original.position + source.pivot;
            var crop = new Rect(center.x - .34f * ppu, center.y - .29f * ppu, .68f * ppu, .64f * ppu);
            crop.xMin = Mathf.Max(crop.xMin, original.xMin);
            crop.yMin = Mathf.Max(crop.yMin, original.yMin);
            crop.xMax = Mathf.Min(crop.xMax, original.xMax);
            crop.yMax = Mathf.Min(crop.yMax, original.yMax);
            Vector2 pivot = (center - crop.position) / crop.size;
            Sprite sprite = Sprite.Create(source.texture, crop, pivot, ppu, 0, SpriteMeshType.FullRect);
            sprite.name = source.name + " torso";
            torsoCrops.Add(source, sprite);
            return sprite;
        }

        private MeshRenderer CreateRenderer(string name, Transform parent, Material material, int order)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingLayerID = body.sortingLayerID;
            renderer.sortingOrder = order;
            return renderer;
        }

        private void BuildMesh()
        {
            mesh = new Mesh { name = "Stalker twelve fixed bone surfaces" };
            mesh.MarkDynamic();
            var uv = new Vector2[48];
            var triangles = new int[72];
            for (int segment = 0; segment < 12; segment++)
            {
                int layer = segment + 1;
                Rect bounds = profile.LayerBounds[layer];
                for (int corner = 0; corner < 4; corner++)
                {
                    float x = (corner == 1 || corner == 2) ? bounds.xMax : bounds.xMin;
                    float y = corner >= 2 ? bounds.yMax : bounds.yMin;
                    restVertices[segment * 4 + corner] = new Vector3((x - profile.Pivot.x) / profile.PixelsPerUnit, (profile.Pivot.y - y) / profile.PixelsPerUnit, 0);
                    uv[segment * 4 + corner] = new Vector2((layer % 4 * profile.CellSize.x + x) / profile.Atlas.width,
                        1 - (layer / 4 * profile.CellSize.y + y) / profile.Atlas.height);
                    colors[segment * 4 + corner] = Color.white;
                }
                int start = segment * 6;
                int vertex = segment * 4;
                triangles[start] = vertex;
                triangles[start + 1] = vertex + 1;
                triangles[start + 2] = vertex + 2;
                triangles[start + 3] = vertex;
                triangles[start + 4] = vertex + 2;
                triangles[start + 5] = vertex + 3;
            }
            mesh.vertices = restVertices;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }

        private void OnDisable()
        {
            RemoveEyeOffset();
            if (surface != null)
            {
                surface.enabled = false;
            }
            if (contactMesh != null)
            {
                contactMesh.enabled = false;
            }
            if (projectionMesh != null)
            {
                projectionMesh.enabled = false;
            }
        }

        private void OnDestroy()
        {
            foreach (Sprite crop in torsoCrops.Values)
            {
                Release(crop);
            }
            if (mesh != null)
            {
                Release(mesh);
            }
        }

        private static void Release(Object resource)
        {
            if (Application.isPlaying)
            {
                Destroy(resource);
            }
            else
            {
                DestroyImmediate(resource);
            }
        }
    }
}
