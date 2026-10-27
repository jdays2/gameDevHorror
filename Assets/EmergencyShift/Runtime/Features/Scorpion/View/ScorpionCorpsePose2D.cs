using UnityEngine;

namespace EmergencyShift.Features.Scorpion.View
{
    /// <summary>Capture the displayed pose before the common death emitter replaces it.</summary>
    [DefaultExecutionOrder(-5), RequireComponent(typeof(EnemyHealth2D), typeof(GoreBurst2D))]
    public sealed class ScorpionCorpsePose2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private BloodPool2D pool;
        [SerializeField] private Sprite[] flesh = System.Array.Empty<Sprite>();
        private EnemyHealth2D health;
        private GoreBurst2D gore;
        private void Awake()
        {
            health = GetComponent<EnemyHealth2D>();
            gore = GetComponent<GoreBurst2D>();
        }
        private void OnEnable() => health.Died += FreezePose;
        private void OnDisable() => health.Died -= FreezePose;
        private void FreezePose()
        {
            // Index zero stays the current full pose; later indices are stable shared flesh assets.
            var pieces = new Sprite[flesh.Length + 1];
            pieces[0] = body.sprite;
            System.Array.Copy(flesh, 0, pieces, 1, flesh.Length);
            gore.Configure(pool, pieces, body.sharedMaterial);
        }
    }
}
