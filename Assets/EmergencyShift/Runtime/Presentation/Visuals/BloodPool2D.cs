using UnityEngine;

namespace EmergencyShift
{
    /// <summary>One persistent pool per death; baked procedural shapes share the original remains lifecycle.</summary>
    public sealed class BloodPool2D : MonoBehaviour
    {
        [SerializeField]
        private BloodPoolProfile profile;
        [SerializeField]
        private SpriteRenderer[] patches;
        [SerializeField]
        private SpriteRenderer[] droplets;
        private Vector3[] patchScales;
        private float[] dropArrival;
        private float ruptureAge;
        private bool rupturing;
        private string variantId;
        public string VariantId => variantId;
        public SpriteRenderer[] DropletRenderers => droplets;

        public float DropArrivalTime(int index) => dropArrival[index];
        public void BeginRupture()
        {
            enabled = true;
            ruptureAge = 0;
            rupturing = true;
            patchScales = new Vector3[patches.Length];
            dropArrival = new float[droplets.Length];
            for (int i = 0; i < patches.Length; i++)
            {
                patchScales[i] = patches[i].transform.localScale;
                patches[i].transform.localScale *= InitialScale;
            }

            for (int i = 0; i < droplets.Length; i++)
            {
                Vector2 arrival = profile != null ? profile.ArrivalSeconds : new Vector2(.18f, .34f);
                dropArrival[i] = Random.Range(arrival.x, arrival.y);
                droplets[i].enabled = false;
            }
        }

        private void Update()
        {
            if (!rupturing)
            {
                return;
            }

            ruptureAge += Time.deltaTime;
            float duration = profile != null ? profile.GrowSeconds : .22f;
            float t = Mathf.Clamp01(ruptureAge / duration);
            float grow = Mathf.Lerp(InitialScale, 1, 1 - (1 - t) * (1 - t));
            for (int i = 0; i < patches.Length; i++)
            {
                patches[i].transform.localScale = patchScales[i] * grow;
            }

            bool all = true;
            for (int i = 0; i < droplets.Length; i++)
            {
                bool arrived = ruptureAge >= dropArrival[i];
                droplets[i].enabled = arrived;
                all &= arrived;
            }

            if (t >= 1 && all)
            {
                rupturing = false;
                enabled = false;
            }
        }

        public void Initialize(float size, int seed, LayerMask walls)
        {
            enabled = false;
            variantId = null;
            var random = new System.Random(seed);
            if (profile != null)
            {
                InitializeTextured(size, random, walls);
                return;
            }

            for (int i = 0; i < patches.Length; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2;
                float distance = i == 0 ? 0 : (float)random.NextDouble() * size * 0.42f;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                patches[i].transform.localPosition = new Vector3(offset.x, offset.y, 0);
                float diameter = size * (i == 0 ? 1.5f : Mathf.Lerp(0.7f, 1.1f, (float)random.NextDouble()));
                patches[i].transform.localScale = new Vector3(diameter, diameter * Mathf.Lerp(0.62f, 1.0f, (float)random.NextDouble()), 1);
                patches[i].transform.localRotation = Quaternion.Euler(0, 0, (float)random.NextDouble() * 360);
                patches[i].color = i % 3 == 0 ? new Color(0.62f, 0.012f, 0.025f, 0.96f) : new Color(0.88f, 0.025f, 0.04f, 0.96f);
            }

            for (int i = 0; i < droplets.Length; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2;
                float distance = size * Mathf.Lerp(0.7f, 1.8f, (float)random.NextDouble());
                Vector2 origin = transform.position;
                Vector2 goal = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                RaycastHit2D hit = Physics2D.Linecast(origin, goal, walls);
                if (hit.collider != null)
                {
                    goal = hit.point + hit.normal * 0.05f;
                }

                droplets[i].transform.position = new Vector3(goal.x, goal.y, transform.position.z);
                float scale = size * Mathf.Lerp(0.06f, 0.18f, (float)random.NextDouble());
                droplets[i].transform.localScale = new Vector3(scale, scale * Mathf.Lerp(0.6f, 1.5f, (float)random.NextDouble()), 1);
                droplets[i].transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
                droplets[i].color = new Color(0.82f, 0.015f, 0.03f, 0.98f);
            }
        }

        private float InitialScale => profile != null ? profile.InitialScale : .3f;

        public bool SupportsVariant(string id)
        {
            return string.IsNullOrEmpty(id) || (profile != null && profile.IsValid() && profile.FindVariant(id) >= 0);
        }

        public void RestoreVariant(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                variantId = null;
                return;
            }

            if (!SupportsVariant(id))
            {
                throw new System.InvalidOperationException("Missing saved blood variant: " + id);
            }

            variantId = id;
            ApplySprites(profile.VariantAt(profile.FindVariant(id)));
        }

        private void ApplySprites(BloodPoolVariant variant)
        {
            patches[0].sprite = variant.Sprite;
            for (int i = 0; i < droplets.Length; i++)
            {
                droplets[i].sprite = profile.DropletSpriteAt(i % profile.DropletSpriteCount);
            }
        }

        private void InitializeTextured(float size, System.Random random, LayerMask walls)
        {
            if (!profile.IsValid() || patches == null || patches.Length == 0)
            {
                throw new System.InvalidOperationException("Invalid blood pool profile: " + name);
            }

            BloodPoolVariant variant = profile.VariantAt(random.Next(profile.VariantCount));
            variantId = variant.Id;
            ApplySprites(variant);
            for (int i = 0; i < patches.Length; i++)
            {
                patches[i].enabled = i == 0;
            }

            SpriteRenderer pool = patches[0];
            pool.transform.localPosition = Vector3.zero;
            pool.transform.localRotation = Quaternion.Euler(0, 0, (float)random.NextDouble() * 360);
            float width = size * variant.CanvasSize * Mathf.Lerp(profile.ScaleRange.x, profile.ScaleRange.y, (float)random.NextDouble());
            Vector2 bounds = pool.sprite.bounds.size;
            pool.transform.localScale = new Vector3(width / bounds.x, width / bounds.y, 1);
            pool.color = profile.PoolTint;

            for (int i = 0; i < droplets.Length; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2;
                float distance = size * Mathf.Lerp(profile.DropletDistance.x, profile.DropletDistance.y, (float)random.NextDouble());
                Vector2 origin = transform.position;
                Vector2 goal = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                RaycastHit2D hit = Physics2D.Linecast(origin, goal, walls);
                if (hit.collider != null)
                {
                    goal = hit.point + hit.normal * .05f;
                }

                SpriteRenderer drop = droplets[i];
                drop.transform.position = new Vector3(goal.x, goal.y, transform.position.z);
                drop.transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
                float diameter = size * Mathf.Lerp(profile.DropletSize.x, profile.DropletSize.y, (float)random.NextDouble());
                float aspect = Mathf.Lerp(profile.DropletAspect.x, profile.DropletAspect.y, (float)random.NextDouble());
                Vector2 dropBounds = drop.sprite.bounds.size;
                drop.transform.localScale = new Vector3(diameter / dropBounds.x, diameter * aspect / dropBounds.y, 1);
                drop.color = profile.DropletTint;
            }
        }

        public void Configure(SpriteRenderer[] pools, SpriteRenderer[] drops)
        {
            patches = pools;
            droplets = drops;
        }
    }
}
