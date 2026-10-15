using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmergencyShift
{
    public enum GroundSoundSurface
    {
        Concrete,
        Wood,
        Tile
    }

    /// <summary>Optional floor region; independent from the material of a bullet's target.</summary>
    [RequireComponent(typeof(BoxCollider2D)), DisallowMultipleComponent]
    public sealed class GroundAudioSurface2D : MonoBehaviour
    {
        private static readonly List<GroundAudioSurface2D> regions = new List<GroundAudioSurface2D>();
        [SerializeField]
        private GroundSoundSurface surface;
        [SerializeField]
        private int priority;
        private BoxCollider2D area;
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
            regions.Add(this);
        }

        private void OnDisable()
        {
            regions.Remove(this);
        }

        public static GroundSoundSurface At(Vector2 point) => At(point, SceneManager.GetActiveScene());
        public static GroundSoundSurface At(Vector2 point, Scene scene)
        {
            GroundAudioSurface2D best = null;
            foreach (var region in regions)
            {
                if (region != null && region.gameObject.scene == scene && region.area != null && region.area.enabled && region.area.OverlapPoint(point) && (best == null || region.priority > best.priority))
                {
                    best = region;
                }
            }

            return best != null ? best.surface : GroundSoundSurface.Concrete;
        }
    }
}
