using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Малый направленный выброс при попадании; не создаёт постоянную лужу живому врагу.</summary>
    public sealed class BloodHitSpray2D : MonoBehaviour
    {
        private struct Drop { public Transform visual; public SpriteRenderer renderer; public Vector2 velocity; public Vector3 scale; }
        private Drop[] drops;
        private float age;
        public void Initialize(Sprite sprite,Material material,Vector2 heading)
        {
            drops=new Drop[8];
            float forward=Mathf.Atan2(heading.y,heading.x)*Mathf.Rad2Deg;
            for(int i=0;i<drops.Length;i++)
            {
                var obj=new GameObject("Impact blood drop",typeof(SpriteRenderer));obj.transform.SetParent(transform,false);
                var renderer=obj.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=material;
                renderer.sortingOrder=2;renderer.color=new Color(.95f,.025f,.035f,1);
                float angle=(forward+(i<2?180:0)+Random.Range(-55f,55f))*Mathf.Deg2Rad;
                Vector2 velocity=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*Random.Range(1.4f,4.2f);
                var scale=new Vector3(Random.Range(.035f,.065f),Random.Range(.10f,.23f),1);
                obj.transform.localScale=scale;obj.transform.rotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg-90);
                drops[i]=new Drop{visual=obj.transform,renderer=renderer,velocity=velocity,scale=scale};
            }
        }
        private void Update()
        {
            if(drops==null)return;age+=Time.deltaTime;
            float t=Mathf.Clamp01(age/.24f);
            for(int i=0;i<drops.Length;i++)
            {
                drops[i].visual.localPosition=(Vector3)(drops[i].velocity*(age-age*age*1.5f));
                drops[i].visual.localScale=drops[i].scale*Mathf.Lerp(1,.35f,t);
                drops[i].renderer.color=new Color(.95f,.025f,.035f,1-t*t);
            }
            if(age>=.24f)Destroy(gameObject);
        }
    }
}
