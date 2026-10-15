using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Быстрый веер крови: летящие штрихи приходят в позиции постоянных капель лужи.</summary>
    public sealed class BloodRuptureBurst2D : MonoBehaviour
    {
        private struct Drop { public SpriteRenderer image; public Vector2 target; public Vector3 size; public float duration; }
        private Drop[] drops;
        private SpriteRenderer cloud;
        private Vector2 origin;
        private float age;
        public void Initialize(BloodPool2D pool,Sprite sprite,Material material)
        {
            origin=transform.position;age=0;
            var targets=pool.DropletRenderers;drops=new Drop[targets.Length];
            for(int i=0;i<drops.Length;i++)
            {
                var renderer=Create("Blood spray",sprite,material);
                Vector2 offset=(Vector2)targets[i].transform.position-origin;
                renderer.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(offset.y,offset.x)*Mathf.Rad2Deg-90);
                renderer.transform.localScale=new Vector3(targets[i].transform.localScale.x*.7f,targets[i].transform.localScale.y*2.5f,1);
                drops[i]=new Drop{image=renderer,target=offset,size=targets[i].transform.localScale,duration=pool.DropArrivalTime(i)};
            }
            cloud=Create("Body rupture mist",sprite,material);
            cloud.transform.localScale=Vector3.one*.12f;
        }
        private SpriteRenderer Create(string name,Sprite sprite,Material material)
        {
            var obj=new GameObject(name,typeof(SpriteRenderer));obj.transform.SetParent(transform,false);
            var image=obj.GetComponent<SpriteRenderer>();image.sprite=sprite;image.sharedMaterial=material;
            image.sortingOrder=RemainsVisualStyle.DeadOrder;image.color=new Color(.88f,.015f,.025f,.95f);return image;
        }
        private void Update()
        {
            if(drops==null)return;age+=Time.deltaTime;
            for(int i=0;i<drops.Length;i++)
            {
                var d=drops[i];float t=Mathf.Clamp01(age/d.duration);
                d.image.enabled=t<1;
                d.image.transform.localPosition=d.target*(1-(1-t)*(1-t));
                d.image.transform.localScale=new Vector3(d.size.x*.7f,d.size.y*Mathf.Lerp(2.5f,1,t),1);
            }
            float swell=Mathf.Clamp01(age/.07f);
            cloud.transform.localScale=new Vector3(1.2f,.85f,1)*Mathf.Lerp(.12f,1,swell);
            cloud.color=new Color(.85f,.01f,.02f,.75f*(1-Mathf.Clamp01(age/.22f)));
            if(age>=.36f)Destroy(gameObject);
        }
    }
}
