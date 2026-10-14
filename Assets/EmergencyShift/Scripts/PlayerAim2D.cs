using UnityEngine;
using UnityEngine.InputSystem;

namespace EmergencyShift
{
    /// <summary>Unsmooth cursor target on the XY floor; independent of the flashlight's rotation.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-100)]
    public sealed class PlayerAim2D : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        private Vector2 pointer, scriptedPoint;
        private bool pointerValid, scripted;
        public bool HasTarget => isActiveAndEnabled && (scripted || (pointerValid && worldCamera != null));
        public bool UsesScriptedTarget => scripted;
        public Vector2 ScreenPosition => scripted && worldCamera != null ? (Vector2)worldCamera.WorldToScreenPoint(scriptedPoint) : pointer;
        public Vector2 WorldPoint
        {
            get
            {
                if (scripted) return scriptedPoint;
                if (!HasTarget) return transform.position;
                Ray ray = worldCamera.ScreenPointToRay(pointer);
                var floor = new Plane(Vector3.forward, Vector3.zero);
                return floor.Raycast(ray, out float distance) ? (Vector2)ray.GetPoint(distance) : (Vector2)transform.position;
            }
        }
        public void Configure(Camera camera) { worldCamera = camera; }
        private void Awake() { if (worldCamera == null) worldCamera = Camera.main; }
        private void Update()
        {
            pointerValid = !scripted && Application.isFocused && Mouse.current != null && worldCamera != null;
            if (!pointerValid) return;
            pointer = Mouse.current.position.ReadValue();
            pointerValid = worldCamera.pixelRect.Contains(pointer);
        }
        public void SetScriptedTarget(Vector2 worldPoint) { scripted = true; scriptedPoint = worldPoint; }
        public void ClearScriptedTarget() { scripted = false; pointerValid = false; }
        private void OnDisable() { pointerValid = false; }
        private void OnApplicationFocus(bool focused) { if (!focused) pointerValid = false; }
    }
}
