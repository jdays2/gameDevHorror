using System.Collections;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Одноразовый разрыв тела. Лужа и куски независимы от игрового поведения врага.</summary>
    public sealed class GoreBurst2D : MonoBehaviour
    {
        [SerializeField] private BloodPool2D bloodPoolPrefab;
        [SerializeField] private Sprite[] fragments;
        [SerializeField] private Material litMaterial;
        [SerializeField, Min(0.1f)] private float poolSize=1.35f;
        [SerializeField] private Sprite bloodSpraySprite;
        [SerializeField] private Sprite contactShadowSprite;
        [SerializeField] private Material shadowMaterial;
        [Header("Rupture — impulse and landing")]
        [SerializeField, Min(0)] private float releaseDelay=.12f;
        [SerializeField] private Vector2 impulseRange=new Vector2(3.2f,9.5f);
        [SerializeField] private Vector2 liftRange=new Vector2(3f,5.5f);
        [SerializeField, Min(1)] private float fragmentGravity=26f;
        private bool emitted;
        public event System.Action<GibMotion2D> FragmentLaunched;
        public void Configure(BloodPool2D pool,Sprite[] pieces,Material material)
        {bloodPoolPrefab=pool;fragments=pieces;litMaterial=material;}
        public void ConfigureRupture(Sprite blood,Sprite shadow,Material material)
        {bloodSpraySprite=blood;contactShadowSprite=shadow;shadowMaterial=material;}
        public void Play(LayerMask obstacles)
        {
            if(emitted)return;emitted=true;
            // Кровь появляется сразу при переходе в Dead; разлёт частей следует за разрывом.
            if(bloodPoolPrefab!=null)
            {
                BloodPool2D pool=Instantiate(bloodPoolPrefab,new Vector3(transform.position.x,transform.position.y,-0.02f),Quaternion.identity);
                pool.name="Blood pool";
                pool.Initialize(poolSize*Random.Range(0.85f,1.18f),Random.Range(1,int.MaxValue),obstacles);
                pool.BeginRupture();
                if(bloodSpraySprite!=null)
                {
                    var spray=new GameObject("Blood rupture burst",typeof(BloodRuptureBurst2D));
                    spray.transform.position=new Vector3(transform.position.x,transform.position.y,-.034f);
                    spray.GetComponent<BloodRuptureBurst2D>().Initialize(pool,bloodSpraySprite,litMaterial);
                }
            }
            StartCoroutine(Burst(obstacles));
        }
        private IEnumerator Burst(LayerMask obstacles)
        {
            // Первые кадры показывают разрыв, затем отсоединённые части получают своё движение.
            yield return new WaitForSeconds(releaseDelay);
            if(fragments==null||fragments.Length==0)yield break;
            for(int i=0;i<8;i++)
            {
                while(!GoreSpawnBudget2D.TryReserve())yield return null;
                var piece=new GameObject("Stalker fragment",typeof(GibMotion2D));
                piece.transform.position=new Vector3(transform.position.x,transform.position.y,RemainsVisualStyle.SurfaceZ);
                var body=new GameObject("Fragment visual",typeof(SpriteRenderer));body.transform.SetParent(piece.transform,false);
                body.transform.localScale=Vector3.one*Random.Range(0.65f,0.95f);
                var renderer=body.GetComponent<SpriteRenderer>();
                renderer.sprite=fragments[i%fragments.Length];renderer.sharedMaterial=litMaterial;
                renderer.sortingOrder=RemainsVisualStyle.DeadOrder;renderer.color=RemainsVisualStyle.Tint;
                float angle=i*Mathf.PI*2/8+Random.Range(-.33f,.33f);
                body.transform.rotation=Quaternion.Euler(0,0,Random.Range(0f,360f));
                SpriteRenderer shadow=null;
                if(contactShadowSprite!=null&&shadowMaterial!=null)
                {
                    var obj=new GameObject("Fragment contact shadow",typeof(SpriteRenderer));obj.transform.SetParent(piece.transform,false);
                    obj.transform.localPosition=new Vector3(0,0,.006f);shadow=obj.GetComponent<SpriteRenderer>();
                    shadow.sprite=contactShadowSprite;shadow.sharedMaterial=shadowMaterial;shadow.sortingOrder=RemainsVisualStyle.DeadOrder;
                }
                var motion=piece.GetComponent<GibMotion2D>();motion.ConfigureVisual(renderer,shadow);
                // Чередуем тяжёлые близкие куски и быстрые дальние, а не восемь одинаковых импульсов.
                float power=Random.Range(impulseRange.x,impulseRange.y)*(i%3==0?.65f:1);
                motion.Launch(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*power,Random.Range(-650f,650f),obstacles.value|(1<<9),
                    Random.Range(liftRange.x,liftRange.y),fragmentGravity);
                FragmentLaunched?.Invoke(motion);
            }
        }
    }
}
