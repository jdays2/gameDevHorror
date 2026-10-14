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
        private bool emitted;
        public void Configure(BloodPool2D pool,Sprite[] pieces,Material material)
        {bloodPoolPrefab=pool;fragments=pieces;litMaterial=material;}
        public void Play(LayerMask obstacles)
        {
            if(emitted)return;emitted=true;
            // Кровь появляется сразу при переходе в Dead; разлёт частей следует за разрывом.
            if(bloodPoolPrefab!=null)
            {
                BloodPool2D pool=Instantiate(bloodPoolPrefab,new Vector3(transform.position.x,transform.position.y,-0.02f),Quaternion.identity);
                pool.name="Blood pool";
                pool.Initialize(poolSize*Random.Range(0.85f,1.18f),Random.Range(1,int.MaxValue),obstacles);
            }
            StartCoroutine(Burst(obstacles));
        }
        private IEnumerator Burst(LayerMask obstacles)
        {
            // Первые кадры показывают разрыв, затем отсоединённые части получают своё движение.
            yield return new WaitForSeconds(0.45f);
            if(fragments==null||fragments.Length==0)yield break;
            for(int i=0;i<8;i++)
            {
                var piece=new GameObject("Stalker fragment",typeof(SpriteRenderer),typeof(GibMotion2D));
                piece.transform.position=new Vector3(transform.position.x,transform.position.y,RemainsVisualStyle.SurfaceZ);
                piece.transform.localScale=Vector3.one*Random.Range(0.65f,0.95f);
                var renderer=piece.GetComponent<SpriteRenderer>();
                renderer.sprite=fragments[i%fragments.Length];renderer.sharedMaterial=litMaterial;
                renderer.sortingOrder=RemainsVisualStyle.DeadOrder;renderer.color=RemainsVisualStyle.Tint;
                float angle=i*Mathf.PI*2/8+Random.Range(-0.24f,0.24f);
                piece.transform.rotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg);
                piece.GetComponent<GibMotion2D>().Launch(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*Random.Range(3f,6f),Random.Range(-220f,220f),obstacles);
            }
        }
    }
}
