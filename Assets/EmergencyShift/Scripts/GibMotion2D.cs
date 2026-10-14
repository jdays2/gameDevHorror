using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Кусок летит в плоскости пола, отскакивает от стены и остаётся лежать.</summary>
    public sealed class GibMotion2D : MonoBehaviour
    {
        private Vector2 velocity;
        private float spin;
        private LayerMask walls;
        public void Launch(Vector2 initialVelocity,float angularSpeed,LayerMask obstacles)
        { velocity=initialVelocity;spin=angularSpeed;walls=obstacles; }
        private void Update()
        {
            if(velocity.sqrMagnitude<0.0025f){velocity=Vector2.zero;enabled=false;return;}
            Vector2 position=transform.position;
            float step=velocity.magnitude*Time.deltaTime;
            RaycastHit2D hit=Physics2D.CircleCast(position,0.03f,velocity.normalized,step,walls);
            if(hit.collider!=null)
            { position=hit.point+hit.normal*0.05f;velocity=Vector2.Reflect(velocity,hit.normal)*0.35f; }
            else position+=velocity*Time.deltaTime;
            transform.position=new Vector3(position.x,position.y,transform.position.z);
            transform.Rotate(0,0,spin*Time.deltaTime);
            velocity*=Mathf.Exp(-2.8f*Time.deltaTime);spin*=Mathf.Exp(-2.4f*Time.deltaTime);
        }
    }
}
