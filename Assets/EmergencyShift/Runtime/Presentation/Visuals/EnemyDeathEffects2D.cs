using UnityEngine;
namespace EmergencyShift
{
    /// <summary>One death notification produces one blood/gib burst, independently of the AI species.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(EnemyHealth2D))]
    public sealed class EnemyDeathEffects2D : MonoBehaviour
    {
        [SerializeField] private LayerMask blockingGeometry;
        private EnemyHealth2D health; private GoreBurst2D gore;
        private void Awake(){health=GetComponent<EnemyHealth2D>();gore=GetComponent<GoreBurst2D>();health.Died+=OnDied;}
        private void OnDied(){if(gore!=null)gore.Play(blockingGeometry);}
        private void OnDestroy(){if(health!=null)health.Died-=OnDied;}
    }
}
