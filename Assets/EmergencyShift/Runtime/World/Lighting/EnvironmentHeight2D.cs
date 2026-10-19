using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    public enum EnvironmentHeightClass
    {
        Low,
        Furniture,
        Tall
    }

    /// <summary>Authoring height of a static surface. The Editor bakes URP receiver/caster layers.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class EnvironmentHeight2D : MonoBehaviour
    {
        [Tooltip("Low: bin/chair. Furniture: worktop/drum. Tall: cupboard/wall. Shadows reach the same class and lower surfaces.")]
        public EnvironmentHeightClass heightClass = EnvironmentHeightClass.Furniture;
        [Header("Floor projection")]
        public FloorShadowProfile floorShadowProfile;
        [Tooltip("Full-height walls/doors remain opaque across the entire light range.")]
        public bool infiniteFloorShadow;
        public bool overrideFloorLength;
        [Min(0)] public float floorShadowLength = 1.4f;
        [SerializeField, HideInInspector]
        private FloorShadowContour2D[] floorContours = Array.Empty<FloorShadowContour2D>();
        private static readonly List<EnvironmentHeight2D> active = new List<EnvironmentHeight2D>();
        public static IReadOnlyList<EnvironmentHeight2D> Active => active;
        public static int GeometryVersion { get; private set; }
        public IReadOnlyList<FloorShadowContour2D> FloorContours => floorContours;
        public float FloorLength => overrideFloorLength ? floorShadowLength : floorShadowProfile.Length(heightClass);

        public void SetBakedContours(FloorShadowContour2D[] contours)
        {
            floorContours = contours ?? Array.Empty<FloorShadowContour2D>();
            GeometryVersion++;
        }

        private void OnEnable()
        {
            if (Application.isPlaying && floorContours.Length > 0 && floorShadowProfile == null)
            {
                Debug.LogError("Environment height needs its floor shadow profile. Apply the prefab's shadow settings.", this);
            }

            if (gameObject.scene.IsValid() && !active.Contains(this))
            {
                active.Add(this);
                GeometryVersion++;
            }
        }

        private void OnDisable()
        {
            if (active.Remove(this))
            {
                GeometryVersion++;
            }
        }

        private void OnValidate()
        {
            floorShadowLength = Mathf.Max(0, floorShadowLength);
            GeometryVersion++;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            active.Clear();
            GeometryVersion++;
        }
    }
}
