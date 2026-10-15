using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Переиспользуемое пятно: набор круговых спрайтов даёт нерегулярную лужу и капли.</summary>
    public sealed class BloodPool2D : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer[] patches;
        [SerializeField]
        private SpriteRenderer[] droplets;
        private Vector3[] patchScales;
        private float[] dropArrival;
        private float ruptureAge;
        private bool rupturing;
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
                patches[i].transform.localScale *= .30f;
            }

            for (int i = 0; i < droplets.Length; i++)
            {
                dropArrival[i] = Random.Range(.18f, .34f);
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
            float t = Mathf.Clamp01(ruptureAge / .22f);
            float grow = Mathf.Lerp(.30f, 1, 1 - (1 - t) * (1 - t));
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
            var random = new System.Random(seed);
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

        public void Configure(SpriteRenderer[] pools, SpriteRenderer[] drops)
        {
            patches = pools;
            droplets = drops;
        }
    }
}
