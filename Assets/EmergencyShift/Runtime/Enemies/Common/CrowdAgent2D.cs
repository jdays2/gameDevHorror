using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;
namespace EmergencyShift
{
    /// <summary>Small-population local separation. Static walls still decide whether a step is safe.</summary>
    [RequireComponent(typeof(Rigidbody2D),typeof(CircleCollider2D)),DisallowMultipleComponent]
    public sealed class CrowdAgent2D : MonoBehaviour
    {
        [SerializeField,Min(0)] private float clearance=.16f;
        [SerializeField,Range(0,2)] private float strength=.85f;
        private Rigidbody2D body;
        private CircleCollider2D shape;
        private Damageable2D life;
        private int ordinal;
        private static int sequence;
        private static readonly List<CrowdAgent2D> active=new List<CrowdAgent2D>(32);
        private static readonly ProfilerMarker marker=new ProfilerMarker("EmergencyShift.Crowd.Separation");
        public float Radius => shape.radius*Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.y));
        private void Awake(){body=GetComponent<Rigidbody2D>();shape=GetComponent<CircleCollider2D>();life=GetComponent<Damageable2D>();ordinal=++sequence;}
        private void OnEnable(){if(!active.Contains(this))active.Add(this);}
        private void OnDisable(){active.Remove(this);}
        public Vector2 Adjust(Vector2 desired,LayerMask solids)
        {
            using(marker.Auto())
            {
                float speed=desired.magnitude;if(speed<.01f)return desired;
                Vector2 repel=Vector2.zero;
                foreach(var other in active)
                {
                    if(other==this || other.body==null || (other.life!=null&&!other.life.IsAlive) || other.gameObject.scene!=gameObject.scene)continue;
                    Vector2 delta=body.position-other.body.position;float range=Radius+other.Radius+clearance;
                    if(delta.sqrMagnitude>=range*range)continue;
                    if(Physics2D.Linecast(body.position,other.body.position,solids).collider!=null)continue;
                    float distance=delta.magnitude;
                    Vector2 away=distance>.001f?delta/distance:new Vector2(Mathf.Cos(ordinal*2.39996f),Mathf.Sin(ordinal*2.39996f));
                    repel+=away*(1-distance/range);
                }
                Vector2 next=Vector2.ClampMagnitude(desired+repel*speed*strength,speed);
                if(next.sqrMagnitude>.0001f && Physics2D.CircleCast(body.position,Radius*.95f,next.normalized,Mathf.Max(.08f,speed*Time.fixedDeltaTime),solids).collider!=null)return Vector2.zero;
                return next;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){active.Clear();sequence=0;}
    }
}
