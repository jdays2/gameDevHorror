using UnityEngine;
using UnityEngine.UI;

namespace EmergencyShift
{
    /// <summary>Pointer reticle with a reload symbol driven by the active weapon's actual phase.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(100)]
    public sealed class AimReticle2D : MaskableGraphic
    {
        [SerializeField]
        private PlayerAim2D aim;
        [SerializeField]
        private PlayerWeapon2D weapon;
        [SerializeField]
        private Color nearColor = new Color(.45f, .47f, .48f, .8f);
        [SerializeField, Min(0)]
        private float reloadRotationSpeed = 120;
        [SerializeField, Range(0, 1)]
        private float reloadOpacity = .42f;
        private bool visible;
        private bool previousCursor;
        private bool forward = true;
        private bool reloading;
        private float reloadAngle;
        public bool IsVisible => visible;
        public bool IsShowingReload => visible && reloading;

        public void Configure(PlayerAim2D target, PlayerWeapon2D gun)
        {
            aim = target;
            weapon = gun;
            raycastTarget = false;
        }

        private void LateUpdate()
        {
            bool show = aim != null && aim.HasTarget && weapon != null && weapon.HasWeapon && weapon.IsOperational;
            if (show)
            {
                var parent = rectTransform.parent as RectTransform;
                Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, aim.ScreenPosition, uiCamera, out Vector2 point))
                {
                    rectTransform.localPosition = point;
                }

                bool valid = weapon.HasForwardAim;
                if (forward != valid)
                {
                    forward = valid;
                    SetVerticesDirty();
                }
            }

            bool loading = show && weapon.Phase == WeaponPhase.Loading;
            if (reloading != loading)
            {
                reloading = loading;
                reloadAngle = 0;
                SetVerticesDirty();
            }

            if (reloading)
            {
                reloadAngle = Mathf.Repeat(reloadAngle - reloadRotationSpeed * Time.deltaTime, 360);
                SetVerticesDirty();
            }

            float opacity = reloading ? reloadOpacity : 1;
            canvasRenderer.SetAlpha(show ? opacity : 0);
            if (show && !visible)
            {
                previousCursor = Cursor.visible;
                Cursor.visible = false;
            }

            if (!show && visible)
            {
                Cursor.visible = previousCursor;
            }

            visible = show;
        }

        protected override void OnDisable()
        {
            if (visible)
            {
                Cursor.visible = previousCursor;
            }

            visible = false;
            base.OnDisable();
        }

        private static readonly Vector2[] axes =
        {
            Vector2.up,
            Vector2.right,
            Vector2.down,
            Vector2.left
        };
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (reloading)
            {
                ReloadArrow(mesh, reloadAngle, 3.2f, new Color(0, 0, 0, .65f));
                ReloadArrow(mesh, reloadAngle + 180, 3.2f, new Color(0, 0, 0, .65f));
                ReloadArrow(mesh, reloadAngle, 1.5f, color);
                ReloadArrow(mesh, reloadAngle + 180, 1.5f, color);
                return;
            }

            Color tint = forward ? color : nearColor;
            Ring(mesh, 8, 2.8f, new Color(0, 0, 0, .65f));
            Ring(mesh, 8, 1.1f, tint);
            for (int i = 0; i < 4; i++)
            {
                Vector2 axis = axes[i];
                Vector2 side = new Vector2(-axis.y, axis.x) * .65f;
                Quad(mesh, axis * 10 - side, axis * 14 - side, axis * 14 + side, axis * 10 + side, tint);
            }
        }

        private static void ReloadArrow(VertexHelper mesh, float angle, float width, Color tint)
        {
            const float radius = 9;
            const int segments = 20;
            const float sweep = 130;
            for (int index = 0; index < segments; index++)
            {
                float start = (angle - index * sweep / segments) * Mathf.Deg2Rad;
                float end = (angle - (index + 1) * sweep / segments) * Mathf.Deg2Rad;
                Vector2 p = new Vector2(Mathf.Cos(start), Mathf.Sin(start));
                Vector2 q = new Vector2(Mathf.Cos(end), Mathf.Sin(end));
                Quad(mesh, p * (radius - width / 2), q * (radius - width / 2), q * (radius + width / 2), p * (radius + width / 2), tint);
            }

            float headAngle = (angle - sweep) * Mathf.Deg2Rad;
            Vector2 radial = new Vector2(Mathf.Cos(headAngle), Mathf.Sin(headAngle));
            Vector2 tangent = new Vector2(radial.y, -radial.x);
            Vector2 tip = radial * radius + tangent * 4;
            Vector2 inner = radial * (radius - width - 1.5f) - tangent;
            Vector2 outer = radial * (radius + width + 1.5f) - tangent;
            int first = mesh.currentVertCount;
            mesh.AddVert(tip, tint, Vector2.zero);
            mesh.AddVert(inner, tint, Vector2.zero);
            mesh.AddVert(outer, tint, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
        }

        private static void Ring(VertexHelper mesh, float radius, float width, Color tint)
        {
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI / 16, b = (i + 1) * Mathf.PI / 16;
                Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), q = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                Quad(mesh, p * (radius - width / 2), q * (radius - width / 2), q * (radius + width / 2), p * (radius + width / 2), tint);
            }
        }

        private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
        {
            int n = mesh.currentVertCount;
            mesh.AddVert(a, tint, Vector2.zero);
            mesh.AddVert(b, tint, Vector2.zero);
            mesh.AddVert(c, tint, Vector2.zero);
            mesh.AddVert(d, tint, Vector2.zero);
            mesh.AddTriangle(n, n + 1, n + 2);
            mesh.AddTriangle(n, n + 2, n + 3);
        }
    }
}
