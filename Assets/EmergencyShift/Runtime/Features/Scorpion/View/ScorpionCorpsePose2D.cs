using UnityEngine;

namespace EmergencyShift.Features.Scorpion.View
{
    /// <summary>Capture the displayed pose before the common death emitter replaces it.</summary>
    [DefaultExecutionOrder(-5), RequireComponent(typeof(EnemyHealth2D), typeof(GoreBurst2D))]
    public sealed class ScorpionCorpsePose2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private BloodPool2D pool;
        private EnemyHealth2D health;
        private GoreBurst2D gore;
        private void Awake()
        {
            health = GetComponent<EnemyHealth2D>();
            gore = GetComponent<GoreBurst2D>();
        }
        private void OnEnable() => health.Died += FreezePose;
        private void OnDisable() => health.Died -= FreezePose;
        private void FreezePose() => gore.Configure(pool, new[]{body.sprite}, body.sharedMaterial);
    }
}
