using UnityEngine;
namespace EmergencyShift
{
    public abstract class EnemyPresentationSource2D : MonoBehaviour
    {
        public abstract bool IsDead {get;}
        public abstract float AttackProgress {get;}
        public abstract float DeathTime {get;}
        public abstract IThreatLight2D LightSource {get;}
        public abstract LayerMask BlockingGeometry {get;}
    }
}
