#if UNITY_EDITOR || UNITY_INCLUDE_INSTRUMENTATION
using System;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Art-directed responses for the isolated furniture study; values are not physical heights.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Art review/Furniture light study")]
    public sealed class FurnitureLightStudyProfile : ScriptableObject
    {
        [Serializable]
        public sealed class Response
        {
            public string label;
            [Range(0, 1)] public float topResponse;
            [Range(0, 1)] public float edgeResponse = .9f;
            [Min(.01f), Tooltip("Width of the matte edge band in local sprite units.")]
            public float edgeWidth = .06f;
        }

        public Response[] surfaces = new Response[0];
        [Min(.1f)] public float inspectionClearance = 1.5f;

        public void Validate()
        {
            if (surfaces == null || surfaces.Length == 0 || !float.IsFinite(inspectionClearance) || inspectionClearance <= 0)
            {
                throw new InvalidOperationException("Furniture study requires responses and a positive inspection clearance.");
            }

            foreach (var surface in surfaces)
            {
                if (surface == null || string.IsNullOrWhiteSpace(surface.label)
                    || !float.IsFinite(surface.topResponse) || surface.topResponse < 0 || surface.topResponse > 1
                    || !float.IsFinite(surface.edgeResponse) || surface.edgeResponse < 0 || surface.edgeResponse > 1
                    || !float.IsFinite(surface.edgeWidth) || surface.edgeWidth <= 0)
                {
                    throw new InvalidOperationException("Invalid furniture response. Gains must be 0–1; edge width must be positive.");
                }
            }
        }
    }
}
#endif
