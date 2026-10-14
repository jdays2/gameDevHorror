using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Планарный импульс + условная высота: быстрый выброс, удар об пол и остановка.</summary>
    public sealed class GibMotion2D : MonoBehaviour
    {
        private Vector2 velocity;
        private float spin;
        private LayerMask walls;
        private Vector2 floorPosition;
        private float height;
        private float verticalSpeed;
        private float gravity;
        private bool landed;
        private SpriteRenderer visual;
        private SpriteRenderer shadow;
        private Vector3 baseScale;
        public float Height => height;
        public Vector2 Velocity => velocity;
        public bool Settled { get; private set; }
        public void ConfigureVisual(SpriteRenderer body, SpriteRenderer contact) { visual = body; shadow = contact; }
        public void Launch(Vector2 initialVelocity,float angularSpeed,LayerMask obstacles,float lift=4f,float fallGravity=26f)
        {
            velocity=initialVelocity;spin=angularSpeed;walls=obstacles;
            floorPosition=transform.position;height=0;verticalSpeed=Mathf.Max(0.1f,lift);gravity=Mathf.Max(1,fallGravity);
            landed=false;Settled=false;
            if(visual==null)visual=GetComponent<SpriteRenderer>();
            baseScale=visual.transform.localScale;enabled=true;
            if(shadow!=null){shadow.transform.localScale=new Vector3(.22f,.10f,1);shadow.color=new Color(0,0,0,.35f);}
        }
        private void Update()
        {
            float dt=Time.deltaTime;
            float step=velocity.magnitude*dt;
            RaycastHit2D hit=step>0?Physics2D.CircleCast(floorPosition,0.05f,velocity.normalized,step,walls):default(RaycastHit2D);
            if(hit.collider!=null)
            {floorPosition=hit.centroid+hit.normal*0.015f;velocity=Vector2.Reflect(velocity,hit.normal)*0.25f;}
            else floorPosition+=velocity*dt;
            verticalSpeed-=gravity*dt;height+=verticalSpeed*dt;
            if(height<=0 && verticalSpeed<0)
            {
                height=0;
                if(!landed)
                {
                    landed=true;verticalSpeed=-verticalSpeed*0.18f;
                    velocity*=0.16f;spin*=0.2f;
                }
                else {Settle();return;}
            }
            velocity*=Mathf.Exp(-(landed?8f:0.65f)*dt);
            if(landed)velocity=Vector2.MoveTowards(velocity,Vector2.zero,32f*dt);
            transform.position=new Vector3(floorPosition.x,floorPosition.y,RemainsVisualStyle.SurfaceZ);
            // Камера остаётся строго сверху: подлёт читается масштабом и контактной тенью, без наклона вида.
            visual.transform.localScale=baseScale*(1+height*0.5f);
            visual.transform.Rotate(0,0,spin*dt);
            visual.color=Color.Lerp(RemainsVisualStyle.Tint,new Color(.74f,.68f,.72f),Mathf.Clamp01(height*1.5f));
            if(shadow!=null)
            {
                shadow.transform.localScale=new Vector3(.22f,.10f,1)/(1+height*.7f);
                shadow.color=new Color(0,0,0,Mathf.Lerp(.35f,.16f,Mathf.Clamp01(height)));
            }
        }
        private void Settle()
        {
            Settled=true;velocity=Vector2.zero;spin=0;height=0;
            transform.position=new Vector3(floorPosition.x,floorPosition.y,RemainsVisualStyle.SurfaceZ);
            visual.transform.localScale=baseScale;visual.color=RemainsVisualStyle.Tint;
            if(shadow!=null){shadow.transform.localScale=new Vector3(.22f,.10f,1);shadow.color=new Color(0,0,0,.35f);}
            enabled=false;
        }
    }
}
