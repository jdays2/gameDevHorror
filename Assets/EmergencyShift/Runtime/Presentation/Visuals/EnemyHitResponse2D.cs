using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Короткая реакция изображения на пулю; здоровье и решения остаются вне представления.</summary>
    [DefaultExecutionOrder(20)]
    public sealed class EnemyHitResponse2D : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Sprite bloodSprite;
        [SerializeField] private Material bloodMaterial;
        [SerializeField, Min(.02f)] private float reactionSeconds=.16f;
        [SerializeField, Range(0,.2f)] private float visualPush=.07f;
        private EnemyHealth2D actor;
        private Vector3 restingPosition,restingScale;
        private Color restingColor;
        private Vector2 direction;
        private float remaining;
        private bool initialized;
        public int HitsShown { get; private set; }
        public bool Reacting => remaining>0;
        public void Configure(Transform root,SpriteRenderer renderer,Sprite blood,Material material)
        {visual=root;body=renderer;bloodSprite=blood;bloodMaterial=material;}
        private void Awake()
        {
            actor=GetComponent<EnemyHealth2D>();
            if(visual!=null){restingPosition=visual.localPosition;restingScale=visual.localScale;}
            if(body!=null)restingColor=body.color;
            initialized=true;
        }
        public void Play(Vector2 point,Vector2 heading,bool lethal)
        {
            HitsShown++;
            if(bloodSprite!=null&&bloodMaterial!=null)
            {
                var obj=new GameObject("Bullet blood spray",typeof(BloodHitSpray2D));
                obj.transform.position=new Vector3(point.x,point.y,-.12f);
                obj.GetComponent<BloodHitSpray2D>().Initialize(bloodSprite,bloodMaterial,heading);
            }
            direction=heading.normalized;remaining=lethal?0:reactionSeconds;
            if(lethal)ResetVisual(true);
        }
        private void LateUpdate()
        {
            if(remaining<=0||visual==null||body==null)return;
            bool dead=actor!=null&&!actor.IsAlive;
            if(dead){remaining=0;ResetVisual(true);return;}
            remaining=Mathf.Max(0,remaining-Time.deltaTime);
            float weight=remaining/reactionSeconds;
            Vector3 local=visual.parent!=null?visual.parent.InverseTransformVector(direction):new Vector3(direction.x,direction.y,0);
            visual.localPosition=restingPosition+local*visualPush*weight;
            visual.localScale=Vector3.Scale(restingScale,new Vector3(1+.08f*weight,1-.10f*weight,1));
            body.color=Color.Lerp(restingColor,new Color(1,.48f,.42f),weight*.65f);
            if(remaining<=0)ResetVisual(false);
        }
        private void ResetVisual(bool dead)
        {
            if(visual!=null){visual.localPosition=restingPosition;visual.localScale=restingScale;}
            if(body!=null)body.color=dead?RemainsVisualStyle.Tint:restingColor;
        }
        private void OnDisable(){if(initialized&&visual!=null)ResetVisual(actor!=null&&!actor.IsAlive);remaining=0;}
    }
}
