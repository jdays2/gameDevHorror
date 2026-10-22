using System;
using System.Collections.Generic;
using EmergencyShift.Core.Composition;
using UnityEngine;
using EmergencyShift.Features.PlayerInput.Logic;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Player input, common aiming and projectile presentation; definitions create independent firing cycles.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(20)]
    [RequireComponent(typeof(Flashlight2D))]
    public sealed class PlayerWeapon2D : MonoBehaviour, IWeaponUseGate, IWeaponShotView
    {
        [SerializeField] private Transform weaponPivot;
        [SerializeField] private Transform muzzle;
        [SerializeField] private BulletProjectile2D bulletPrefab;
        [SerializeField] private ShotEffect2D effectPrefab;
        [SerializeField] private ShotVisualProfile visualProfile;
        [SerializeField] private PlayerAim2D aim;
        [SerializeField] private WeaponDefinition definition;
        [SerializeField] private LayerMask blockingGeometry = 1 << 8;
        [SerializeField] private LayerMask bulletCollisionMask = (1 << 8) | (1 << 9) | 1;
        private Flashlight2D flashlight;
        private PlayerLife2D life;
        private PlayerShotShadow2D shotShadow;
        private WeaponController controller;
        private WeaponInventory Inventory => controller?.Inventory;
        private readonly Dictionary<string, WeaponDefinition> definitions = new Dictionary<string, WeaponDefinition>();
        private IPlayerInput input;
        private GameplayInputBlock inputBlock;
        private ScenePrefabFactory projectilePrefabs;

        [Inject]
        private void Construct(IPlayerInput playerInput, GameplayInputBlock block, WeaponControllerFactory factory,
            ScenePrefabFactory scenePrefabs)
        {
            if (controller != null)
            {
                throw new InvalidOperationException("Weapon controller is already composed.");
            }

            input = playerInput;
            inputBlock = block;
            projectilePrefabs = scenePrefabs;
            controller = factory.Create(this, this);
            controller.SelectionChanged += Equipped;
            controller.Operation += OnOperation;
        }

        private RaycastHit2D pendingMuzzleHit;
        private WeaponAimEnvironment2D aimEnvironment;
        bool IWeaponUseGate.InputBlocked => inputBlock == null || inputBlock.IsBlocked;
        bool IWeaponUseGate.CanUseWeapon => CanUse;

        private Vector3 restingWeaponScale;
        public bool HasWeapon => Inventory?.ActiveCycle != null;
        public int Ammo => Inventory?.ActiveCycle?.TotalAmmo ?? 0;
        public int MaximumAmmo => Inventory?.ActiveDefinition?.MaximumAmmo ?? definition.MaximumAmmo;
        public int LoadedAmmo => Inventory?.ActiveCycle?.Loaded ?? 0;
        public int ReserveAmmo => Inventory?.ActiveCycle?.Reserve ?? 0;
        public int MagazineCapacity => Inventory?.ActiveCycle?.MagazineCapacity ?? 0;
        public WeaponPhase Phase => Inventory?.ActiveCycle?.Phase ?? WeaponPhase.Ready;
        public int OwnedCount => Inventory?.Count ?? 0;
        public int Damage => Inventory?.ActiveDefinition?.Damage ?? definition.Damage;
        public float ShotInterval => Inventory?.ActiveDefinition?.SecondsBetweenShots ?? definition.SecondsBetweenShots;
        public float BulletRange => Inventory?.ActiveDefinition?.Range ?? definition.Range;

        public WeaponDefinition Definition
        {
            get
            {
                WeaponSettings active = Inventory?.ActiveDefinition;
                return active != null && definitions.TryGetValue(active.ContentId, out WeaponDefinition asset) ? asset : definition;
            }
        }

        public int ShotsFired => controller?.ShotsFired ?? 0;
        public ShotVisualProfile VisualProfile => visualProfile;
        public Vector2 LastShotDirection => controller?.LastShot?.Request.Direction ?? Vector2.zero;
        public Vector2 LastShotOrigin => controller?.LastShot?.Request.Origin ?? Vector2.zero;
        public Vector2 LastShotTarget => controller?.LastShot?.Request.Target ?? Vector2.zero;

        public bool IsOperational => CanUse;

        public bool HasForwardAim => aimEnvironment != null && aimEnvironment.HasForwardAim;

        public event System.Action OwnershipChanged;
        public event System.Action ShotFired;
        public event System.Action EmptyTriggerPulled;
        public event System.Action CasingEjected;
        public event System.Action<WeaponOperation> Operation;
        public event System.Action<Collider2D, Vector2> SurfaceHit;

        public int LastImpactVolleyId
        {
            get;
            private set;
        }

        public void ReportImpact(Collider2D target, Vector2 point)
        {
            ReportImpact(target, point, 0);
        }

        public void ReportImpact(Collider2D target, Vector2 point, int volleyId)
        {
            LastImpactVolleyId = volleyId;
            SurfaceHit?.Invoke(target, point);
        }

        public void Configure(Transform pivot, Transform exit, BulletProjectile2D bullet, ShotEffect2D effect,
            ShotVisualProfile style)
        {
            weaponPivot = pivot;
            muzzle = exit;
            bulletPrefab = bullet;
            effectPrefab = effect;
            visualProfile = style;
            RefreshAimEnvironment();
        }

        public void SetVisualProfile(ShotVisualProfile style)
        {
            if (style != null)
            {
                style.Validate();
                visualProfile = style;
            }
        }

        public void ConfigureAim(PlayerAim2D target)
        {
            aim = target;
            RefreshAimEnvironment();
        }

        public void SetDefinition(WeaponDefinition value, bool applyVisuals = true)
        {
            if (value == null)
            {
                throw new System.ArgumentNullException(nameof(value));
            }

            value.Validate();
            string previousId = Inventory?.ActiveDefinition?.ContentId;
            controller?.ReplaceDefinition(value.ToSettings());
            if (previousId != null)
            {
                definitions.Remove(previousId);
                definitions[value.ContentId] = value;
            }
            definition = value;
            if (applyVisuals && value.Visuals != null)
            {
                visualProfile = value.Visuals;
            }
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
            RefreshAimEnvironment();
            if (weaponPivot != null)
            {
                restingWeaponScale = weaponPivot.localScale;
                weaponPivot.gameObject.SetActive(false);
            }

            definition.Validate();
        }

        public void RestoreState(bool owned, int ammo, WeaponDefinition savedDefinition)
        {
            RequireController();
            definition = savedDefinition != null ? savedDefinition : definition;
            controller.RestoreLegacy(owned, ammo, definition.ToSettings());
            definitions.Clear();
            if (owned)
            {
                definitions[definition.ContentId] = definition;
            }
            Equipped();
        }

        public WeaponInventorySnapshot CaptureInventory() => RequireController().Capture();

        public void RestoreInventory(WeaponInventorySnapshot state, WeaponCatalog catalog)
        {
            WeaponSettingsCatalog settings = catalog.ToSettings();
            WeaponInventory.Validate(state, settings);
            RequireController().Restore(state, settings);
            definitions.Clear();
            foreach (OwnedWeaponSnapshot record in state.weapons)
            {
                definitions.Add(record.contentId, catalog.Find(record.contentId));
            }
            Equipped();
        }

        private bool CanUse => controller != null && inputBlock != null && !inputBlock.IsBlocked
            && isActiveAndEnabled && (life == null || !life.IsDead);

        public bool Acquire(int cartridges, WeaponDefinition selected = null)
        {
            WeaponDefinition asset = selected != null ? selected : Definition;
            if (!CanUse || !controller.Acquire(asset.ToSettings(), cartridges))
            {
                return false;
            }

            definitions[asset.ContentId] = asset;
            Equipped();
            return true;
        }

        public bool TryAddAmmo(int cartridges, WeaponDefinition target = null)
        {
            return AddAmmo(cartridges, target) > 0;
        }

        public int AddAmmo(int cartridges, WeaponDefinition target = null)
        {
            return CanUse ? controller.AddAmmo(cartridges, target != null ? target.ToSettings() : null) : 0;
        }

        public bool TryEquip(int slot) => controller != null && controller.Equip(slot);

        public bool TryReload() => controller != null && controller.Reload();

        private void Equipped()
        {
            if (Definition.Visuals != null)
            {
                visualProfile = Definition.Visuals;
            }

            if (weaponPivot != null)
            {
                weaponPivot.localScale = Vector3.Scale(restingWeaponScale, Definition.HeldScale);
            }

            UpdatePose();
            OwnershipChanged?.Invoke();
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
            if (controller == null || inputBlock.IsBlocked)
            {
                return;
            }

            bool fire = controller.Update(new WeaponInputIntent(input.SelectPistolPressed,
                input.SelectShotgunPressed, input.ReloadPressed, input.FirePressed, input.FireHeld));
            UpdatePose();
            if (fire)
            {
                TryFire();
            }
        }

        private void OnDisable()
        {
            controller?.CancelReload();
            if (weaponPivot != null)
            {
                weaponPivot.gameObject.SetActive(false);
            }
        }

        private void RefreshAimEnvironment()
        {
            var rig = new WeaponAimRig2D(transform, weaponPivot, muzzle);
            var source = new WeaponAimSource2D(aim, flashlight);
            aimEnvironment = new WeaponAimEnvironment2D(rig, source, blockingGeometry);
        }

        private void UpdatePose()
        {
            if (weaponPivot == null || flashlight == null)
            {
                return;
            }

            weaponPivot.gameObject.SetActive(HasWeapon && CanUse);
            aimEnvironment?.UpdateRotation();
        }

        public bool TryFire()
        {
            if (controller == null || !controller.CheckFireIntent() || visualProfile == null
                || bulletPrefab == null || effectPrefab == null || muzzle == null)
            {
                return false;
            }

            visualProfile.Validate();
            UpdatePose();
            if (aimEnvironment == null || !aimEnvironment.TryBuildShot(BulletRange,
                out WeaponShotRequest request, out pendingMuzzleHit))
            {
                return false;
            }

            return controller.TryFire(request);
        }

        void IWeaponShotView.PresentEmptyTrigger() => EmptyTriggerPulled?.Invoke();

        void IWeaponShotView.PresentShot(WeaponShotPlan plan)
        {
            Vector2 origin = plan.Request.Origin;
            Vector2 direction = plan.Request.Direction;
            ShotFired?.Invoke();
            if (plan.EjectsOnShot)
            {
                CasingEjected?.Invoke();
            }

            var flash = projectilePrefabs.Instantiate(effectPrefab.gameObject,
                new Vector3(origin.x, origin.y, -.1f), Quaternion.identity).GetComponent<ShotEffect2D>();
            flash.Play(visualProfile, false, direction);
            if (shotShadow != null)
            {
                shotShadow.Follow(flash.LightSource);
            }

            if (!plan.Request.MuzzleBlocked)
            {
                foreach (Vector2 heading in plan.Directions)
                {
                    var bullet = projectilePrefabs.Instantiate(bulletPrefab.gameObject,
                        new Vector3(origin.x, origin.y, -.1f), Quaternion.identity).GetComponent<BulletProjectile2D>();
                    bullet.Launch(transform, heading, visualProfile, effectPrefab, bulletCollisionMask, plan.Damage,
                        plan.Range, plan.VolleyId);
                }
            }
            else
            {
                ReportImpact(pendingMuzzleHit.collider, pendingMuzzleHit.point, plan.VolleyId);
                var impact = projectilePrefabs.Instantiate(effectPrefab.gameObject,
                    new Vector3(origin.x, origin.y, -.1f), Quaternion.identity).GetComponent<ShotEffect2D>();
                impact.Play(visualProfile, true, -direction);
            }
        }

        private WeaponController RequireController()
        {
            return controller ?? throw new InvalidOperationException("Weapon requires its level DI composition.");
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.Dispose();
                controller.SelectionChanged -= Equipped;
                controller.Operation -= OnOperation;
            }
        }
    }
}
