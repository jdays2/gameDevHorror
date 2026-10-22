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
            [SerializeField]
            private string label;
            [Range(0, 1)]
            [SerializeField]
            private float topResponse;
            [Range(0, 1)]
            [SerializeField]
            private float edgeResponse = .9f;
            [Min(.01f), Tooltip("Width of the matte edge band in local sprite units.")]
            [SerializeField]
            private float edgeWidth = .06f;

            public string Label => label;
            public float TopResponse => topResponse;
            public float EdgeResponse => edgeResponse;
            public float EdgeWidth => edgeWidth;

            public Response()
            {
            }
            public Response(string label, float topResponse, float edgeResponse = .9f, float edgeWidth = .06f)
            {
                this.label = label;
                this.topResponse = topResponse;
                this.edgeResponse = edgeResponse;
                this.edgeWidth = edgeWidth;
            }
        }

        [SerializeField]
        private Response[] surfaces = new Response[0];
        [Min(.1f)]
        [SerializeField]
        private float inspectionClearance = 1.5f;

        public int SurfaceCount => surfaces != null ? surfaces.Length : 0;
        public Response SurfaceAt(int index) => surfaces[index];
        public float InspectionClearance => inspectionClearance;

        public void Validate()
        {
            if (surfaces == null || surfaces.Length == 0 || !float.IsFinite(inspectionClearance) || inspectionClearance <= 0)
            {
                throw new InvalidOperationException("Furniture study requires responses and a positive inspection clearance.");
            }

            foreach (var surface in surfaces)
            {
                if (surface == null || string.IsNullOrWhiteSpace(surface.Label)
                    || !float.IsFinite(surface.TopResponse) || surface.TopResponse < 0 || surface.TopResponse > 1
                    || !float.IsFinite(surface.EdgeResponse) || surface.EdgeResponse < 0 || surface.EdgeResponse > 1
                    || !float.IsFinite(surface.EdgeWidth) || surface.EdgeWidth <= 0)
                {
                    throw new InvalidOperationException("Invalid furniture response. Gains must be 0–1; edge width must be positive.");
                }
            }
        }
    }
}
#endif
