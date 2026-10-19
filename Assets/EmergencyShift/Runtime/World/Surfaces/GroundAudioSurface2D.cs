using EmergencyShift.Core.Composition;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Optional floor region; independent from the material of a bullet's target.</summary>
    [RequireComponent(typeof(BoxCollider2D)), DisallowMultipleComponent]
    public sealed class GroundAudioSurface2D : MonoBehaviour, IGroundAudioRegion
    {
        [SerializeField]
        private GroundSoundSurface surface;
        [SerializeField]
        private int priority;
        private BoxCollider2D area;
        private SceneRegistry<IGroundAudioRegion> registry;

        public GroundSoundSurface Surface => surface;
        public int Priority => priority;

        [Inject]
        private void Construct(SceneRegistry<IGroundAudioRegion> sceneRegistry)
        {
            registry?.Remove(this);
            registry = sceneRegistry;
            area = GetComponent<BoxCollider2D>();
            if (isActiveAndEnabled)
            {
                registry.Register(this);
            }
        }

        private void Awake()
        {
            area = GetComponent<BoxCollider2D>();
            area.isTrigger = true;
        }

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnEnable()
        {
            registry?.Register(this);
        }

        private void OnDisable()
        {
            registry?.Remove(this);
        }

        public bool Contains(Vector2 point)
        {
            return this != null && isActiveAndEnabled && area != null && area.enabled && area.OverlapPoint(point);
        }
    }
}
