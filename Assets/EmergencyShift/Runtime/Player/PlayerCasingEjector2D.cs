using System.Collections.Generic;
using EmergencyShift.Core.Composition;
using EmergencyShift.Core.Randomness;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>One spent casing per actual shot, with a bounded reusable collection of floor props.</summary>
    [RequireComponent(typeof(PlayerWeapon2D), typeof(PlayerAudio2D)), DisallowMultipleComponent]
    public sealed class PlayerCasingEjector2D : MonoBehaviour
    {
        [SerializeField]
        private ShellCasing2D casingPrefab;
        [SerializeField]
        private bool casingsEnabled = true;
        [SerializeField, Range(8, 96)]
        private int maximumCasings = 48;
        [SerializeField]
        private LayerMask obstacles = (1 << 8) | (1 << 9);
        private readonly List<ShellCasing2D> pool = new List<ShellCasing2D>();
        private PlayerWeapon2D weapon;
        private PlayerAudio2D audioOwner;
        private Transform root;
        private IRandomSource random;
        private ScenePrefabFactory prefabs;
        private int cursor;
        public bool CasingsEnabled
        {
            get => casingsEnabled;
            set => casingsEnabled = value;
        }
        public int EmittedCount
        {
            get; private set;
        }
        public int PooledCount => pool.Count;

        public event System.Action<ShellCasing2D> CasingEmitted;

        [Inject]
        private void Construct(ScenePrefabFactory scenePrefabs, [Inject(Id = RandomSourceIds.Cosmetic)] RandomSourceFactory randomSources)
        {
            prefabs = scenePrefabs;
            random = randomSources.Create();
        }

        private void Awake()
        {
            weapon = GetComponent<PlayerWeapon2D>();
            audioOwner = GetComponent<PlayerAudio2D>();
        }

        private void OnEnable()
        {
            weapon.CasingEjected += Eject;
        }

        private void OnDisable()
        {
            weapon.CasingEjected -= Eject;
        }

        private float Between(float a, float b) => Mathf.Lerp(a, b, (float)random.NextUnit());
        private void Eject()
        {
            if (!casingsEnabled || casingPrefab == null)
            {
                return;
            }

            if (root == null)
            {
                root = new GameObject("Spent casings — runtime").transform;
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root.gameObject, gameObject.scene);
            }

            ShellCasing2D casing;
            if (pool.Count < maximumCasings)
            {
                GameObject instance = prefabs.Instantiate(casingPrefab.gameObject,
                    root.TransformPoint(casingPrefab.transform.localPosition),
                    root.rotation * casingPrefab.transform.localRotation, root);
                casing = instance.GetComponent<ShellCasing2D>();
                pool.Add(casing);
            }
            else
            {
                casing = pool[cursor];
                cursor = (cursor + 1) % pool.Count;
            }

            ShotVisualProfile visuals = weapon.Definition.Visuals;
            Vector2 direction = weapon.LastShotDirection;
            Vector2 side = new Vector2(direction.y, -direction.x);
            Vector2 port = (Vector2)transform.position + direction * visuals.CasingPort.x + side * visuals.CasingPort.y;
            var blocked = Physics2D.Linecast(transform.position, port, obstacles);
            if (blocked.collider != null)
            {
                port = blocked.point - (port - (Vector2)transform.position).normalized * visuals.CasingWallClearance;
            }

            casing.Launch(port, side * Between(visuals.CasingSideSpeed.x, visuals.CasingSideSpeed.y) + direction * Between(visuals.CasingForwardSpeed.x, visuals.CasingForwardSpeed.y), Between(visuals.CasingRiseSpeed.x, visuals.CasingRiseSpeed.y), Between(visuals.CasingSpin.x, visuals.CasingSpin.y), audioOwner, obstacles, weapon.Definition.AudioProfile);
            EmittedCount++;
            CasingEmitted?.Invoke(casing);
        }

        private void OnDestroy()
        {
            if (root != null)
            {
                Destroy(root.gameObject);
            }
        }

#if UNITY_EDITOR
        public void SetPreviewSeed(int seed)
        {
            random = new SystemRandomSource(new System.Random(seed));
        }

        public void ClearForPreview()
        {
            foreach (var casing in pool)
            {
                if (casing != null)
                {
                    Destroy(casing.gameObject);
                }
            }

            pool.Clear();
            cursor = 0;
        }
#endif
    }
}
