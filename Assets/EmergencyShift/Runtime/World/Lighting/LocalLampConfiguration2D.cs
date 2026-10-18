using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Scene authoring entry point; clips remain shared, playback stays on the audio component.</summary>
    [ExecuteAlways, DefaultExecutionOrder(-100), DisallowMultipleComponent]
    [RequireComponent(typeof(LocalLamp2D), typeof(LocalLampAudio2D))]
    public sealed class LocalLampConfiguration2D : MonoBehaviour
    {
        public const float SourceInset = .025f;
        [SerializeField]
        private LocalLampSceneSettings settings = new LocalLampSceneSettings();
        [SerializeField, HideInInspector]
        private Transform fixture;
        [SerializeField, HideInInspector]
        private float referenceLength = 1.1f;
        [SerializeField, HideInInspector]
        private float backEdgeOffset = .078262295f;

        public LocalLampSceneSettings Settings => settings;
        public Vector3 WallAnchor => transform.TransformPoint(Vector3.up * SourceInset);

        public void ConfigureGeometry(Transform visual, float length, float backOffset)
        {
            fixture = visual;
            referenceLength = length;
            backEdgeOffset = backOffset;
            ApplyConfiguration();
        }

        private void OnEnable()
        {
            ApplyConfiguration();
        }

        private void OnValidate()
        {
            ApplyConfiguration();
        }

        [ContextMenu("Apply lamp configuration")]
        public void ApplyConfiguration()
        {
            settings.Validate();
            GetComponent<LocalLamp2D>().ConfigureScene(settings);
            GetComponent<LocalLampAudio2D>().ConfigureScene(settings);
            if (fixture == null)
            {
                if (Application.isPlaying)
                {
                    Debug.LogError("LocalLampConfiguration2D needs its fixture geometry.", this);
                }

                return;
            }

            if (!float.IsFinite(referenceLength) || referenceLength <= 0 || !float.IsFinite(backEdgeOffset))
            {
                throw new System.InvalidOperationException("Invalid lamp geometry calibration.");
            }

            float scale = settings.fixtureLength / referenceLength;
            fixture.localScale = Vector3.one * scale;
            fixture.localPosition = Vector3.up * (SourceInset - backEdgeOffset * scale);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(.35f, .85f, 1, .7f);
            Gizmos.DrawWireSphere(transform.position, settings.lightRadius);
            Gizmos.color = new Color(1, .75f, .3f, .7f);
            Gizmos.DrawWireSphere(transform.position, settings.soundRadius);
            Gizmos.DrawLine(transform.position, WallAnchor);
        }
    }
}
