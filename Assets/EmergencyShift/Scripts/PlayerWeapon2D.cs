using UnityEngine;
using UnityEngine.InputSystem;

namespace EmergencyShift
{
    /// <summary>Первый пистолет: владение, общий запас, одиночный выстрел по ЛКМ и направление фонаря.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(20)]
    [RequireComponent(typeof(Flashlight2D))]
    public sealed class PlayerWeapon2D : MonoBehaviour
    {
        [SerializeField] private Transform weaponPivot;
        [SerializeField] private Transform muzzle;
        [SerializeField] private BulletProjectile2D bulletPrefab;
        [SerializeField] private ShotEffect2D effectPrefab;
        [SerializeField] private ShotVisualProfile visualProfile;
        [SerializeField] private LayerMask blockingGeometry = 1 << 8;
        [SerializeField] private LayerMask bulletCollisionMask = (1 << 8) | 1;
        [SerializeField, Min(1)] private int maximumAmmo = 120;
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField, Min(0.05f)] private float secondsBetweenShots = 0.3f;
        [SerializeField, Min(0.5f)] private float bulletRange = 10f;
        private Flashlight2D flashlight;
        private PlayerLife2D life;
        private float nextShot;
        public bool HasWeapon { get; private set; }
        public int Ammo { get; private set; }
        public int MaximumAmmo => maximumAmmo;
        public int ShotsFired { get; private set; }
        public ShotVisualProfile VisualProfile => visualProfile;

        public void Configure(Transform pivot, Transform exit, BulletProjectile2D bullet, ShotEffect2D effect, ShotVisualProfile style)
        { weaponPivot = pivot; muzzle = exit; bulletPrefab = bullet; effectPrefab = effect; visualProfile = style; }
        public void SetVisualProfile(ShotVisualProfile style) { if (style != null) visualProfile = style; }

        private void Awake()
        {
            flashlight = GetComponent<Flashlight2D>(); life = GetComponent<PlayerLife2D>();
            if (weaponPivot != null) weaponPivot.gameObject.SetActive(false);
        }
        private bool CanUse => isActiveAndEnabled && (life == null || !life.IsDead);

        public bool Acquire(int cartridges)
        {
            if (!CanUse || HasWeapon) return false;
            HasWeapon = true; Ammo = Mathf.Min(maximumAmmo, Ammo + Mathf.Max(0, cartridges));
            UpdatePose(); return true;
        }

        public bool TryAddAmmo(int cartridges)
        {
            if (!CanUse || cartridges <= 0 || Ammo >= maximumAmmo) return false;
            Ammo += Mathf.Min(cartridges, maximumAmmo - Ammo); return true;
        }

        private void Update()
        {
            UpdatePose();
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) TryFire();
        }
        private void OnDisable() { if (weaponPivot != null) weaponPivot.gameObject.SetActive(false); }

        private void UpdatePose()
        {
            if (weaponPivot == null || flashlight == null) return;
            weaponPivot.gameObject.SetActive(HasWeapon && CanUse);
            Vector2 direction = flashlight.Direction;
            weaponPivot.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
        }

        public bool TryFire()
        {
            if (!CanUse || !HasWeapon || Ammo <= 0 || Time.time < nextShot
                || visualProfile == null || bulletPrefab == null || effectPrefab == null || muzzle == null) return false;
            UpdatePose();
            Vector2 direction = flashlight.Direction.normalized;
            Vector2 origin = muzzle.position;
            // Проверка от капсулы до дула запрещает выстрел/вспышку за стеной вплотную к игроку.
            RaycastHit2D blockedMuzzle = Physics2D.Linecast(transform.position, origin, blockingGeometry);
            if (blockedMuzzle.collider != null) origin = blockedMuzzle.point - direction * 0.05f;
            Ammo--; ShotsFired++; nextShot = Time.time + secondsBetweenShots;
            var flash = Instantiate(effectPrefab, new Vector3(origin.x, origin.y, -0.1f), Quaternion.identity);
            flash.Play(visualProfile, false, direction);
            if (blockedMuzzle.collider == null)
            {
                var bullet = Instantiate(bulletPrefab, new Vector3(origin.x, origin.y, -0.1f), Quaternion.identity);
                bullet.Launch(transform, direction, visualProfile, effectPrefab, bulletCollisionMask, damage, bulletRange);
            }
            else
            {
                var impact = Instantiate(effectPrefab, new Vector3(origin.x, origin.y, -0.1f), Quaternion.identity);
                impact.Play(visualProfile, true, -direction);
            }
            return true;
        }
    }
}
