using System.Collections.Generic;
using UnityEngine;

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
        private System.Random random;
        private int cursor;
        public bool CasingsEnabled
        {
            get => casingsEnabled; set => casingsEnabled = value;
        }
        public int EmittedCount
        {
            get; private set;
        }
        public int PooledCount => pool.Count;

        public event System.Action<ShellCasing2D> CasingEmitted;
        private void Awake()
        {
            weapon = GetComponent<PlayerWeapon2D>();
            audioOwner = GetComponent<PlayerAudio2D>();
            random = new System.Random(System.Guid.NewGuid().GetHashCode());
        }

        private void OnEnable()
        {
            weapon.CasingEjected += Eject;
        }

        private void OnDisable()
        {
            weapon.CasingEjected -= Eject;
        }

        private float Between(float a, float b) => Mathf.Lerp(a, b, (float)random.NextDouble());
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
                casing = Instantiate(casingPrefab, root);
                pool.Add(casing);
            }
            else
            {
                casing = pool[cursor++ % pool.Count];
            }

            Vector2 direction = weapon.LastShotDirection, side = new Vector2(direction.y, -direction.x);
            Vector2 port = (Vector2)transform.position + direction * .16f + side * .16f;
            var blocked = Physics2D.Linecast(transform.position, port, obstacles);
            if (blocked.collider != null)
            {
                port = blocked.point - (port - (Vector2)transform.position).normalized * .03f;
            }

            casing.Launch(port, side * Between(1.2f, 1.8f) + direction * Between(-.25f, .4f), Between(1.35f, 1.85f), Between(-650, 650), audioOwner, obstacles, weapon.Definition.audioProfile);
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
            random = new System.Random(seed);
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
