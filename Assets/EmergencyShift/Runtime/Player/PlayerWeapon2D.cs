using UnityEngine;
using UnityEngine.InputSystem;

namespace EmergencyShift
{
    /// <summary>Player input, common aiming and projectile presentation; definitions create independent firing cycles.</summary>
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
        private readonly WeaponInventory inventory = new WeaponInventory();
        private WeaponCycle boundCycle;
        private Vector3 restingWeaponScale;
        private float nextEmptyTrigger;
        public bool HasWeapon => inventory.ActiveCycle != null;
        public int Ammo => inventory.ActiveCycle != null ? inventory.ActiveCycle.TotalAmmo : 0;
        public int MaximumAmmo => Definition.maximumAmmo;
        public int LoadedAmmo => inventory.ActiveCycle != null ? inventory.ActiveCycle.Loaded : 0;
        public int ReserveAmmo => inventory.ActiveCycle != null ? inventory.ActiveCycle.Reserve : 0;
        public int MagazineCapacity => inventory.ActiveCycle != null ? inventory.ActiveCycle.MagazineCapacity : 0;
        public WeaponPhase Phase => inventory.ActiveCycle != null ? inventory.ActiveCycle.Phase : WeaponPhase.Ready;
        public int OwnedCount => inventory.Count;
        public int Damage => Definition.damage;
        public float ShotInterval => Mathf.Max(.05f, Definition.secondsBetweenShots);
        public float BulletRange => Definition.range;
        public WeaponDefinition Definition => inventory.ActiveDefinition != null ? inventory.ActiveDefinition : definition;
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
        public event System.Action CasingEjected;
        public event System.Action<WeaponOperation> Operation;
        public event System.Action<Collider2D, Vector2> SurfaceHit;
        public int LastImpactVolleyId { get; private set; }
        public void ReportImpact(Collider2D target, Vector2 point)
        {
            ReportImpact(target, point, 0);
        }

        public void ReportImpact(Collider2D target, Vector2 point, int volleyId)
        {
            LastImpactVolleyId = volleyId;
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

            value.Validate();
            inventory.ReplaceActiveDefinition(value);
            definition = value;
            if (applyVisuals && value.visuals != null)
            {
                visualProfile = value.visuals;
            }

            BindCycle();
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
                restingWeaponScale = weaponPivot.localScale;
                weaponPivot.gameObject.SetActive(false);
            }

            definition.Validate();
        }

        public void RestoreState(bool owned, int ammo, WeaponDefinition savedDefinition)
        {
            UnbindCycle();
            definition = savedDefinition != null ? savedDefinition : definition;
            inventory.RestoreLegacy(owned, ammo, definition);
            nextEmptyTrigger = 0;
            Equipped();
        }

        public WeaponInventorySnapshot CaptureInventory() => inventory.Capture(Time.time);

        public void RestoreInventory(WeaponInventorySnapshot state, WeaponCatalog catalog)
        {
            WeaponInventory.Validate(state, catalog);
            UnbindCycle();
            inventory.Restore(state, catalog, Time.time);
            Equipped();
        }

        private bool CanUse => !GameplayInputBlock.IsBlocked && isActiveAndEnabled && (life == null || !life.IsDead);

        public bool Acquire(int cartridges, WeaponDefinition selected = null)
        {
            if (!CanUse || !inventory.Acquire(selected != null ? selected : Definition, cartridges))
            {
                return false;
            }

            Equipped();
            return true;
        }

        public bool TryAddAmmo(int cartridges, WeaponDefinition target = null)
        {
            return AddAmmo(cartridges, target) > 0;
        }

        public int AddAmmo(int cartridges, WeaponDefinition target = null)
        {
            return CanUse ? inventory.AddAmmo(cartridges, target) : 0;
        }

        public bool TryEquip(int slot)
        {
            if (!CanUse || !inventory.Equip(slot))
            {
                return false;
            }

            Equipped();
            return true;
        }

        public bool TryReload()
        {
            return CanUse && inventory.ActiveCycle != null && inventory.ActiveCycle.TryReload(Time.time);
        }

        private void Equipped()
        {
            if (Definition.visuals != null)
            {
                visualProfile = Definition.visuals;
            }

            BindCycle();
            if (weaponPivot != null)
            {
                weaponPivot.localScale = Vector3.Scale(restingWeaponScale, Definition.HeldScale);
            }

            UpdatePose();
            OwnershipChanged?.Invoke();
        }

        private void BindCycle()
        {
            UnbindCycle();
            boundCycle = inventory.ActiveCycle;
            if (boundCycle != null)
            {
                boundCycle.Operation += OnOperation;
            }
        }

        private void UnbindCycle()
        {
            if (boundCycle != null)
            {
                boundCycle.CancelReload();
                boundCycle.Operation -= OnOperation;
                boundCycle = null;
            }
        }

        private void OnOperation(WeaponOperation operation)
        {
            Operation?.Invoke(operation);
            if (operation == WeaponOperation.Pump)
            {
                CasingEjected?.Invoke();
            }
        }

        private void Update()
        {
            if (GameplayInputBlock.IsBlocked)
            {
                return;
            }

            inventory.ActiveCycle?.Tick(Time.time);
            ReloadEmptyWeapon();
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame)
                {
                    TryEquip(0);
                }

                if (keyboard.digit2Key.wasPressedThisFrame)
                {
                    TryEquip(1);
                }

                if (keyboard.xKey.wasPressedThisFrame)
                {
                    TryReload();
                }
            }

            UpdatePose();
            if (Mouse.current != null)
            {
                bool fire = Definition != null && Definition.automatic ? Mouse.current.leftButton.isPressed : Mouse.current.leftButton.wasPressedThisFrame;
                if (fire)
                {
                    TryFire();
                }
            }
        }

        private void ReloadEmptyWeapon()
        {
            var cycle = inventory.ActiveCycle;
            if (CanUse && cycle != null && cycle.MagazineCapacity > 0 && cycle.Loaded == 0 && cycle.Reserve > 0)
            {
                cycle.TryReload(Time.time);
            }
        }

        private void OnDisable()
        {
            inventory.ActiveCycle?.CancelReload();
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
            var cycle = inventory.ActiveCycle;
            if (CanUse && cycle != null && cycle.CanDryFire)
            {
                if (Time.time >= nextEmptyTrigger)
                {
                    nextEmptyTrigger = Time.time + .25f;
                    EmptyTriggerPulled?.Invoke();
                }

                return false;
            }

            if (!CanUse || cycle == null || !cycle.CanFire(Time.time) || visualProfile == null || bulletPrefab == null || effectPrefab == null || muzzle == null)
            {
                return false;
            }

            UpdatePose();
            Vector2 origin = muzzle.position;
            if (aim != null && !HasForwardAim)
            {
                return false; // Never fire backwards into the owner at a cursor under the gun.
            }

            if (!cycle.TryFire(Time.time))
            {
                return false;
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
            ShotsFired++;
            ShotFired?.Invoke();
            if (cycle.EjectsOnShot)
            {
                CasingEjected?.Invoke();
            }
            var flash = Instantiate(effectPrefab, new Vector3(origin.x, origin.y, -0.1f), Quaternion.identity);
            flash.Play(visualProfile, false, direction);
            if (shotShadow != null)
            {
                shotShadow.Follow(flash.LightSource);
            }

            if (blockedMuzzle.collider == null)
            {
                for (int index = 0; index < Definition.ProjectileCount; index++)
                {
                    Vector2 heading = Definition.ProjectileDirection(direction, index);
                    var bullet = Instantiate(bulletPrefab, new Vector3(origin.x, origin.y, -0.1f), Quaternion.identity);
                    bullet.Launch(transform, heading, visualProfile, effectPrefab, bulletCollisionMask, Damage, BulletRange, ShotsFired);
                }
            }
            else
            {
                ReportImpact(blockedMuzzle.collider, blockedMuzzle.point, ShotsFired);
                var impact = Instantiate(effectPrefab, new Vector3(origin.x, origin.y, -0.1f), Quaternion.identity);
                impact.Play(visualProfile, true, -direction);
            }

            ReloadEmptyWeapon();
            return true;
        }

        private void OnDestroy()
        {
            UnbindCycle();
        }
    }
}
