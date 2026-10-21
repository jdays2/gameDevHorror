using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Unity serialization boundary; extension syntax preserves existing source call sites.</summary>
    public static class RemainsSpriteAdapter
    {
        public static void Capture(this RemainsSpriteState state, SpriteRenderer renderer)
        {
            state.position = renderer.transform.localPosition;
            state.scale = renderer.transform.localScale;
            state.rotation = renderer.transform.localRotation;
            state.color = renderer.color;
            state.visible = renderer.enabled;
        }

        public static void Apply(this RemainsSpriteState state, SpriteRenderer renderer)
        {
            renderer.transform.localPosition = state.position;
            renderer.transform.localScale = state.scale;
            renderer.transform.localRotation = state.rotation;
            renderer.color = state.color;
            renderer.enabled = state.visible;
        }
    }
}
