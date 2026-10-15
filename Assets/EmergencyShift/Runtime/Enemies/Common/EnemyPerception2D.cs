using UnityEngine;
namespace EmergencyShift
{
    /// <summary>Shared sensing, independent of the decision to flee, sleep or attack.</summary>
    [DisallowMultipleComponent]
    public sealed class EnemyPerception2D : MonoBehaviour
    {
        public Transform Target { get; private set; }
        public Damageable2D TargetHealth { get; private set; }
        public LayerMask Obstacles { get; private set; }
        public IThreatLight2D ThreatLight { get; private set; }
        public bool TargetAlive => Target!=null&&(TargetHealth==null||TargetHealth.IsAlive);
        public void Configure(Transform target,LayerMask obstacles,IThreatLight2D light)
        {Target=target;Obstacles=obstacles;ThreatLight=light;TargetHealth=target!=null?target.GetComponent<Damageable2D>():null;}
        public bool CanSeeTarget(Vector2 origin)=>TargetAlive&&Clear(origin,Target.position);
        public bool Clear(Vector2 from,Vector2 to)=>Physics2D.Linecast(from,to,Obstacles).collider==null;
        public bool Within(Vector2 origin,float radius)=>TargetAlive&&((Vector2)Target.position-origin).sqrMagnitude<=radius*radius;
        public bool Exposed(Vector2 point)=>ThreatLight!=null&&ThreatLight.Illuminates(point,Obstacles);
    }
}
