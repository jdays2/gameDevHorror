using UnityEngine;
using EmergencyShift.Features.Scorpion.Config;

namespace EmergencyShift.Features.Scorpion.View
{
    /// <summary>Capture the displayed pose before the common death emitter replaces it.</summary>
    [DefaultExecutionOrder(-5), RequireComponent(typeof(EnemyHealth2D), typeof(GoreBurst2D))]
    public sealed class ScorpionCorpsePose2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private BloodPool2D pool;
        [SerializeField] private Sprite[] flesh = System.Array.Empty<Sprite>();
        [SerializeField] private ScorpionRuptureSet rupture;
        private EnemyHealth2D health;
        private GoreBurst2D gore;
        private void Awake()
        {
            health = GetComponent<EnemyHealth2D>();
            gore = GetComponent<GoreBurst2D>();
            if (rupture == null || body == null || flesh.Length != 3)
            {
                throw new System.InvalidOperationException("Scorpion requires its rupture catalogue, body and stable legacy bank.");
            }
            rupture.Validate();
        }
        private void OnEnable() => health.Died += FreezePose;
        private void OnDisable() => health.Died -= FreezePose;
        private void FreezePose()
        {
            // The serialized bank is stable across death poses and checkpoint restoration.
            int poseIndex = rupture.IndexOf(body.sprite);
            ScorpionRuptureSet.Pose pose = rupture.PoseAt(poseIndex);
            int firstPart = flesh.Length + 1;
            var indices = new int[firstPart + pose.Count];
            var offsets = new Vector2[firstPart + rupture.PoseCount * pose.Count];
            for (int i = 0; i < pose.Count; i++)
            {
                int bankIndex = firstPart + poseIndex * pose.Count + i;
                indices[firstPart + i] = bankIndex;
                offsets[bankIndex] = pose.Offset(i);
            }
            gore.ConfigurePose(offsets, body.transform, indices);
        }
    }
}
