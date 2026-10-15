using UnityEngine;

namespace EmergencyShift
{
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class LevelExit2D : MonoBehaviour
    {
        [SerializeField]
        private LevelRunController2D flow;
        public void Configure(LevelRunController2D level)
        {
            flow = level;
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerLife2D>();
            if (flow != null && player != null)
            {
                flow.TryFinish(player);
            }
        }
    }
}
