using System;
using EmergencyShift.Features.Pickups.Config;
using UnityEngine;

namespace EmergencyShift.Features.Pickups.View
{
    /// <summary>Only a masked stripe over source sprites. Never changes their materials or creates light.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(100)]
    public sealed class PickupGlint2D : MonoBehaviour
    {
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int ColorId = Shader.PropertyToID("_GlintColor");
        private static readonly int WidthId = Shader.PropertyToID("_StripeWidth");
        private static readonly int PlaneId = Shader.PropertyToID("_SweepPlane");
        private static readonly int OriginId = Shader.PropertyToID("_BeamOriginRange");
        private static readonly int DirectionId = Shader.PropertyToID("_BeamDirectionCos");
        private PickupGlintProfile profile;
        private Flashlight2D flashlight;
        private PlayerInteraction actor;
        private MonoBehaviour owner;
        private IPickupGlintTarget target;
        private PickupGlintPulse pulse;
        private SpriteRenderer[] sources;
        private SpriteRenderer[] overlays;
        private MaterialPropertyBlock properties;

        public bool IsShowing { get; private set; }
        public float Progress => pulse?.Progress ?? 0;

        public void Configure(PickupGlintProfile settings, Flashlight2D light, PlayerInteraction recipient, MonoBehaviour pickup)
        {
            if (profile != null)
            {
                throw new InvalidOperationException("Pickup glint is already configured.");
            }

            profile = settings ?? throw new ArgumentNullException(nameof(settings));
            profile.Validate();
            flashlight = light;
            actor = recipient;
            owner = pickup;
            target = pickup as IPickupGlintTarget ?? throw new ArgumentException("Pickup must expose read-only acceptance.");
            pulse = new PickupGlintPulse(profile.Duration, profile.RepeatPause);
        }

        private void Start()
        {
            if (profile == null)
            {
                throw new InvalidOperationException("Pickup glint requires level composition.");
            }

            properties = new MaterialPropertyBlock();
            sources = GetComponentsInChildren<SpriteRenderer>(true);
            overlays = new SpriteRenderer[sources.Length];
            for (int index = 0; index < sources.Length; index++)
            {
                var child = new GameObject("Pickup glint stripe");
                child.hideFlags = HideFlags.DontSave;
                child.transform.SetParent(sources[index].transform, false);
                var overlay = child.AddComponent<SpriteRenderer>();
                overlay.sharedMaterial = profile.Material;
                overlay.enabled = false;
                overlays[index] = overlay;
            }
        }

        private void LateUpdate()
        {
            if (pulse == null || overlays == null || Time.deltaTime == 0)
            {
                return;
            }

            bool visible = owner != null && owner.isActiveAndEnabled && actor != null &&
                flashlight != null && flashlight.isActiveAndEnabled && flashlight.IsOwned && flashlight.IsOn &&
                target.CanReceive(actor) && flashlight.Illuminates(target.PickupPosition, actor.BlockingGeometry);
            pulse.Step(visible, Time.deltaTime);
            if (!pulse.Active)
            {
                Hide();
                return;
            }

            Render();
        }

        private void OnDisable()
        {
            pulse?.Reset();
            Hide();
        }

        private void OnDestroy()
        {
            if (overlays == null)
            {
                return;
            }

            foreach (SpriteRenderer overlay in overlays)
            {
                if (overlay != null)
                {
                    Destroy(overlay.gameObject);
                }
            }
        }

        private void Render()
        {
            IsShowing = false;
            var bounds = new Bounds(transform.position, Vector3.zero);
            bool hasBounds = false;
            for (int index = 0; index < sources.Length; index++)
            {
                SpriteRenderer source = sources[index];
                if (source == null || !source.enabled || !source.gameObject.activeInHierarchy || source.sprite == null)
                {
                    continue;
                }

                if (hasBounds)
                {
                    bounds.Encapsulate(source.bounds);
                }
                else
                {
                    bounds = source.bounds;
                    hasBounds = true;
                }
            }

            float span = Mathf.Max(.001f, bounds.size.x + bounds.size.y);
            properties.SetVector(PlaneId, new Vector4(1, 1, bounds.min.x + bounds.min.y, span));
            properties.SetFloat(ProgressId, pulse.Progress);
            properties.SetFloat(WidthId, profile.StripeWidth);
            Color color = profile.Color;
            color.a *= profile.Intensity;
            properties.SetColor(ColorId, color);
            Vector2 origin = flashlight.Origin;
            Vector2 direction = flashlight.Direction.normalized;
            properties.SetVector(OriginId, new Vector4(origin.x, origin.y, flashlight.Range, 0));
            properties.SetVector(DirectionId, new Vector4(direction.x, direction.y,
                Mathf.Cos(flashlight.BeamAngle * .5f * Mathf.Deg2Rad), 0));
            for (int index = 0; index < sources.Length; index++)
            {
                SpriteRenderer source = sources[index];
                SpriteRenderer overlay = overlays[index];
                bool show = hasBounds && source != null && source.enabled && source.gameObject.activeInHierarchy && source.sprite != null;
                overlay.enabled = show;
                if (!show)
                {
                    continue;
                }

                overlay.sprite = source.sprite;
                overlay.flipX = source.flipX;
                overlay.flipY = source.flipY;
                overlay.drawMode = source.drawMode;
                overlay.size = source.size;
                overlay.color = new Color(1, 1, 1, source.color.a);
                overlay.sortingLayerID = source.sortingLayerID;
                overlay.sortingOrder = source.sortingOrder + 1;
                overlay.maskInteraction = source.maskInteraction;
                overlay.SetPropertyBlock(properties);
                IsShowing = true;
            }
        }

        private void Hide()
        {
            IsShowing = false;
            if (overlays == null)
            {
                return;
            }

            foreach (SpriteRenderer overlay in overlays)
            {
                if (overlay != null)
                {
                    overlay.enabled = false;
                }
            }
        }
    }
}
