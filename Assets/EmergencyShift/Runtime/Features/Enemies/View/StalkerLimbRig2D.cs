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
        private StalkerSkinMesh skin;
        private MeshRenderer surface;
        private MeshRenderer contactMesh;
        private MeshRenderer projectionMesh;
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
            skin = new StalkerSkinMesh(profile);
            mesh = skin.Mesh;
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
            skin.Update(gait, visual, body.color);
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
