using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift
{
    /// <summary>One transient floor-only light keeps finite floor shadows independent from other lights and surface shadows.</summary>
    [DefaultExecutionOrder(200), DisallowMultipleComponent, RequireComponent(typeof(Light2D))]
    public sealed class FiniteFloorLight2D : MonoBehaviour
    {
        [SerializeField] private FloorShadowProfile profile;
        private Light2D source;
        private Light2D floor;
        private CeilingLightOcclusion2D ceiling;
        private Texture2D texture;
        private Sprite cookie;
        private FloorShadowMask mask;
        private int[] originalTargets;
        private int[] surfaceTargets;
        private int floorLayer;
        private int previousGeometry;
        private bool initialized;
        private bool ownsFloor;
        private bool affectsFloor;
        public bool UsingFiniteFloor => ownsFloor && floor != null && floor.enabled;

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            source = GetComponent<Light2D>();
            ceiling = GetComponent<CeilingLightOcclusion2D>();
            floorLayer = SortingLayer.NameToID(EnvironmentShadowLayers.Floor);
            if (profile == null || floorLayer == 0 || source.lightType != Light2D.LightType.Point)
            {
                Debug.LogError("Finite floor light requires its profile, ES Floor layer and a Point Light2D.", this);
                enabled = false;
                return;
            }

            profile.Validate();
            originalTargets = source.targetSortingLayers;
            affectsFloor = System.Array.IndexOf(originalTargets, floorLayer) >= 0;
            var surfaces = new List<int>(originalTargets);
            surfaces.Remove(floorLayer);
            surfaceTargets = surfaces.ToArray();
            initialized = false;
        }

        private void LateUpdate()
        {
            if (source == null)
            {
                return;
            }

            bool ceilingMode = ceiling != null && ceiling.isActiveAndEnabled && ceiling.CeilingLighting;
            // Keep authored cookies and ceiling-wall masks intact. These lights retain the original floor path.
            if (!affectsFloor || source.lightType != Light2D.LightType.Point || ceilingMode ||
                source.lightCookieSprite != null || !source.shadowsEnabled)
            {
                ReleaseFloor();
                return;
            }

            if (!source.enabled || source.pointLightOuterRadius <= 0)
            {
                if (floor != null)
                {
                    floor.enabled = false;
                }

                return;
            }

            if (!ownsFloor)
            {
                source.targetSortingLayers = surfaceTargets;
                ownsFloor = true;
            }

            EnsureResources();
            CopyAppearance();
            int geometry = GeometryHash();
            if (!initialized || geometry != previousGeometry)
            {
                Rebuild();
                previousGeometry = geometry;
                initialized = true;
            }
        }

        private void Update()
        {
            // URP updates its light meshes in LateUpdate. Prepare the cone/radius before that pass.
            if (source != null && affectsFloor && source.lightType == Light2D.LightType.Point &&
                source.enabled && source.shadowsEnabled && source.lightCookieSprite == null &&
                (ceiling == null || !ceiling.isActiveAndEnabled || !ceiling.CeilingLighting))
            {
                EnsureResources();
                CopyAppearance();
            }
        }

        private void EnsureResources()
        {
            if (floor == null)
            {
                var child = new GameObject("Finite floor illumination - transient");
                child.SetActive(false);
                child.hideFlags = HideFlags.HideAndDontSave;
                child.transform.SetParent(transform, false);
                floor = child.AddComponent<Light2D>();
                floor.targetSortingLayers = new[] { floorLayer };
                floor.lightType = Light2D.LightType.Point;
                floor.shadowsEnabled = false;
                child.SetActive(true);
            }

            if (mask != null && mask.Resolution == profile.resolution)
            {
                return;
            }

            UnityEngine.Rendering.CoreUtils.Destroy(cookie);
            UnityEngine.Rendering.CoreUtils.Destroy(texture);
            mask = new FloorShadowMask(profile.resolution);
            texture = new Texture2D(profile.resolution, profile.resolution, TextureFormat.RGBA32, false, true);
            texture.name = "Finite floor shadow cookie - transient";
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            cookie = Sprite.Create(texture, new Rect(0, 0, profile.resolution, profile.resolution), Vector2.one * .5f, profile.resolution);
            cookie.hideFlags = HideFlags.HideAndDontSave;
            floor.lightCookieSprite = cookie;
            initialized = false;
        }

        private void CopyAppearance()
        {
            floor.enabled = source.enabled;
            floor.color = source.color;
            floor.intensity = source.intensity;
            floor.blendStyleIndex = source.blendStyleIndex;
            floor.falloffIntensity = source.falloffIntensity;
            floor.pointLightInnerRadius = source.pointLightInnerRadius;
            floor.pointLightOuterRadius = source.pointLightOuterRadius;
            floor.pointLightInnerAngle = source.pointLightInnerAngle;
            floor.pointLightOuterAngle = source.pointLightOuterAngle;
            floor.lightOrder = source.lightOrder;
            floor.overlapOperation = source.overlapOperation;
            floor.renderingLayerMask = source.renderingLayerMask;
        }

        private int GeometryHash()
        {
            unchecked
            {
                int hash = EnvironmentHeight2D.GeometryVersion;
                hash = hash * 31 + transform.localToWorldMatrix.GetHashCode();
                hash = hash * 31 + source.pointLightOuterRadius.GetHashCode();
                hash = hash * 31 + source.shadowIntensity.GetHashCode();
                hash = hash * 31 + profile.tailFade.GetHashCode();
                for (int i = 0; i < EnvironmentHeight2D.Active.Count; i++)
                {
                    var height = EnvironmentHeight2D.Active[i];
                    if (height.gameObject.scene != gameObject.scene || height.floorShadowProfile == null)
                    {
                        continue;
                    }

                    hash = hash * 31 + height.FloorLength.GetHashCode();
                    hash = hash * 31 + height.infiniteFloorShadow.GetHashCode();
                    for (int j = 0; j < height.FloorContours.Count; j++)
                    {
                        var contour = height.FloorContours[j];
                        hash = hash * 31 + contour.Active.GetHashCode();
                        if (contour.Active)
                        {
                            hash = hash * 31 + contour.frame.localToWorldMatrix.GetHashCode();
                        }
                    }
                }

                return hash;
            }
        }

        private void Rebuild()
        {
            mask.Clear();
            float radius = source.pointLightOuterRadius;
            float worldRadius = radius;
            Matrix4x4 worldToLight = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse;
            for (int i = 0; i < EnvironmentHeight2D.Active.Count; i++)
            {
                var height = EnvironmentHeight2D.Active[i];
                if (height.gameObject.scene != gameObject.scene || height.floorShadowProfile == null)
                {
                    continue;
                }

                float length = height.infiniteFloorShadow ? worldRadius * 3 : height.FloorLength;
                float fade = height.infiniteFloorShadow ? .001f : profile.tailFade;
                for (int j = 0; j < height.FloorContours.Count; j++)
                {
                    var contour = height.FloorContours[j];
                    if (!contour.Active || contour.WorldBounds.SqrDistance(transform.position) > worldRadius * worldRadius)
                    {
                        continue;
                    }

                    mask.Project(contour.WorldPoints, transform.position, worldToLight, radius, length, fade);
                }
            }

            mask.FilterEdges();
            if (source.shadowIntensity < 1)
            {
                for (int i = 0; i < mask.Pixels.Length; i++)
                {
                    byte value = (byte)Mathf.Lerp(255, mask.Pixels[i].r, source.shadowIntensity);
                    mask.Pixels[i] = new Color32(value, value, value, 255);
                }
            }

            texture.SetPixels32(mask.Pixels);
            texture.Apply(false, false);
        }

        private void ReleaseFloor()
        {
            if (ownsFloor && source != null)
            {
                source.targetSortingLayers = originalTargets;
            }

            ownsFloor = false;
            if (floor != null)
            {
                floor.enabled = false;
            }
        }

        private void OnDisable()
        {
            ReleaseFloor();
            if (floor != null)
            {
                UnityEngine.Rendering.CoreUtils.Destroy(floor.gameObject);
            }

            UnityEngine.Rendering.CoreUtils.Destroy(cookie);
            UnityEngine.Rendering.CoreUtils.Destroy(texture);
            floor = null;
            cookie = null;
            texture = null;
            mask = null;
        }
    }
}
