using UnityEngine;
using UnityEngine.InputSystem;

namespace EmergencyShift
{
    /// <summary>Pistol ownership/ammo and a straight projectile from the muzzle through the cursor target.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(20)]
    [RequireComponent(typeof(Flashlight2D))]
    public sealed class PlayerWeapon2D : MonoBehaviour
    {
        [SerializeField]
        private Transform weaponPivot;
        [SerializeField]
        private Transform muzzle;
        [SerializeField]
        private BulletProjectile2D bulletPrefab;
        [SerializeField]
        private ShotEffect2D effectPrefab;
        [SerializeField]
        private ShotVisualProfile visualProfile;
        [SerializeField]
        private PlayerAim2D aim;
        [SerializeField]
        private WeaponDefinition definition;
        [SerializeField]
        private LayerMask blockingGeometry = 1 << 8;
        [SerializeField]
        private LayerMask bulletCollisionMask = (1 << 8) | (1 << 9) | 1;
        private Flashlight2D flashlight;
        private PlayerLife2D life;
        private PlayerShotShadow2D shotShadow;
        private float nextShot;
        private float nextEmptyTrigger;
        public bool HasWeapon
        {
            get; private set;
        }
        public int Ammo
        {
            get; private set;
        }
        public int MaximumAmmo => definition.maximumAmmo;
        public int Damage => definition.damage;
        public float ShotInterval => Mathf.Max(.05f, definition.secondsBetweenShots);
        public float BulletRange => definition.range;
        public WeaponDefinition Definition => definition;
        public int ShotsFired
        {
            get; private set;
        }
        public ShotVisualProfile VisualProfile => visualProfile;
        public Vector2 LastShotDirection
        {
            get; private set;
        }
        public Vector2 LastShotOrigin
        {
            get; private set;
        }
        public Vector2 LastShotTarget
        {
            get; private set;
        }
        public bool IsOperational => CanUse;
        public bool HasForwardAim => aim != null && aim.HasTarget && muzzle != null && weaponPivot != null && Vector2.Dot(aim.WorldPoint - (Vector2)muzzle.position, weaponPivot.up) > .025f;

        public event System.Action OwnershipChanged;
        public event System.Action ShotFired;
        public event System.Action EmptyTriggerPulled;
        public event System.Action<Collider2D, Vector2> SurfaceHit;
        public void ReportImpact(Collider2D target, Vector2 point)
        {
            SurfaceHit?.Invoke(target, point);
        }

        public void Configure(Transform pivot, Transform exit, BulletProjectile2D bullet, ShotEffect2D effect, ShotVisualProfile style)
        {
            weaponPivot = pivot;
            muzzle = exit;
            bulletPrefab = bullet;
            effectPrefab = effect;
            visualProfile = style;
        }

        public void SetVisualProfile(ShotVisualProfile style)
        {
            if (style != null)
            {
                visualProfile = style;
            }
        }

        public void ConfigureAim(PlayerAim2D target)
        {
            aim = target;
        }

        public void SetDefinition(WeaponDefinition value, bool applyVisuals = true)
        {
            if (value == null)
            {
                throw new System.ArgumentNullException(nameof(value));
            }

            definition = value;
            if (applyVisuals && value != null && value.visuals != null)
            {
                visualProfile = value.visuals;
            }

            Ammo = Mathf.Min(Ammo, MaximumAmmo);
        }

        private void Awake()
        {
            if (definition == null)
            {
                Debug.LogError("Missing weapon definition: " + name, this);
                enabled = false;
                return;
            }

            flashlight = GetComponent<Flashlight2D>();
            life = GetComponent<PlayerLife2D>();
            shotShadow = GetComponent<PlayerShotShadow2D>();
            if (weaponPivot != null)
            {
                weaponPivot.gameObject.SetActive(false);
            }
        }

        public void RestoreState(bool owned, int ammo, WeaponDefinition savedDefinition)
        {
            if (savedDefinition != null)
            {
                SetDefinition(savedDefinition);
            }

            HasWeapon = owned;
            Ammo = Mathf.Clamp(ammo, 0, MaximumAmmo);
            nextShot = 0;
            nextEmptyTrigger = 0;
            UpdatePose();
            OwnershipChanged?.Invoke();
        }

        private bool CanUse => isActiveAndEnabled && (life == null || !life.IsDead);

        public bool Acquire(int cartridges, WeaponDefinition selected = null)
        {
            if (!CanUse || HasWeapon)
            {
                return false;
            }

            if (selected != null && selected != definition)
            {
                SetDefinition(selected);
            }

            HasWeapon = true;
            Ammo = Mathf.Min(MaximumAmmo, Ammo + Mathf.Max(0, cartridges));
            UpdatePose();
            OwnershipChanged?.Invoke();
            return true;
        }

        public bool TryAddAmmo(int cartridges)
        {
            if (!CanUse || cartridges <= 0 || Ammo >= MaximumAmmo)
            {
                return false;
            }

            Ammo += Mathf.Min(cartridges, MaximumAmmo - Ammo);
            return true;
        }

        private void Update()
        {
            UpdatePose();
            if (Mouse.current != null)
            {
                bool fire = definition != null && definition.automatic ? Mouse.current.leftButton.isPressed : Mouse.current.leftButton.wasPressedThisFrame;
                if (fire)
                {
                    TryFire();
                }
            }
        }

        private void OnDisable()
        {
            if (weaponPivot != null)
            {
                weaponPivot.gameObject.SetActive(false);
            }
        }

        private void UpdatePose()
        {
            if (weaponPivot == null || flashlight == null)
            {
                return;
            }

            weaponPivot.gameObject.SetActive(HasWeapon && CanUse);
            if (aim != null)
            {
                if (!aim.HasTarget || muzzle == null)
                {
                    return;
                }

                Vector2 offset = aim.WorldPoint - (Vector2)weaponPivot.position;
                float distance = offset.magnitude;
                // Solve the sideways muzzle offset: aiming the player centre at the cursor would miss nearby targets.
                float lateral = Vector2.Dot(muzzle.position - weaponPivot.position, weaponPivot.right);
                if (distance <= Mathf.Abs(lateral) + .001f)
                {
                    return;
                }

                float heading = Mathf.Atan2(offset.y, offset.x) + Mathf.Asin(Mathf.Clamp(lateral / distance, -.999f, .999f));
                weaponPivot.rotation = Quaternion.Euler(0, 0, heading * Mathf.Rad2Deg - 90f);
            }
            else
            {
                Vector2 direction = flashlight.Direction; // Compatibility for older, unconfigured scenes.
                weaponPivot.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            }
        }

        public bool TryFire()
        {
            if (CanUse && HasWeapon && Ammo <= 0)
            {
                if (Time.time >= nextEmptyTrigger)
                {
                    nextEmptyTrigger = Time.time + .25f;
                    EmptyTriggerPulled?.Invoke();
                }

                return false;
            }

            if (!CanUse || !HasWeapon || Ammo <= 0 || Time.time < nextShot || visualProfile == null || bulletPrefab == null || effectPrefab == null || muzzle == null)
            {
                return false;
            }

            UpdatePose();
            Vector2 origin = muzzle.position;
            if (aim != null && !HasForwardAim)
            {
                return false; // Never fire backwards into the owner at a cursor under the gun.
            }

            Vector2 target = aim != null ? aim.WorldPoint : origin + flashlight.Direction * BulletRange;
            Vector2 direction = (target - origin).normalized;
            // Не выпускаем пулю за преградой, даже если дуло выступает сквозь низкую мебель.
            RaycastHit2D blockedMuzzle = Physics2D.Linecast(transform.position, origin, blockingGeometry.value | (1 << 9));
            if (blockedMuzzle.collider != null)
            {
                origin = blockedMuzzle.point - direction * 0.05f;
            }

            LastShotDirection = direction;
            LastShotOrigin = origin;
            LastShotTarget = target;
            Ammo--;
            ShotsFired++;
            nextShot = Time.time + ShotInterval;
            ShotFired?.Invoke();
            var flash = Instantiate(effectPrefab, new Vector3(origin.x, origin.y, -0.1f), Quaternion.identity);
            flash.Play(visualProfile, false, direction);
            if (shotShadow != null)
            {
                shotShadow.Follow(flash.LightSource);
            }

            if (blockedMuzzle.collider == null)
            {
                var bullet = Instantiate(bulletPrefab, new Vector3(origin.x, origin.y, -0.1f), Quaternion.identity);
                bullet.Launch(transform, direction, visualProfile, effectPrefab, bulletCollisionMask, Damage, BulletRange);
            }
            else
            {
                ReportImpact(blockedMuzzle.collider, blockedMuzzle.point);
                var impact = Instantiate(effectPrefab, new Vector3(origin.x, origin.y, -0.1f), Quaternion.identity);
                impact.Play(visualProfile, true, -direction);
            }

            return true;
        }
    }
}
