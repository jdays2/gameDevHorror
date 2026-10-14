using UnityEngine;
namespace EmergencyShift
{
    /// <summary>Recorded rail movement follows actual door events; two saved voices, no timed fake close.</summary>
    [RequireComponent(typeof(Door2D)), DisallowMultipleComponent]
    public sealed class DoorAudio2D : MonoBehaviour
    {
        [SerializeField] private DoorAudioSet sounds;
        [SerializeField] private AudioSource motion, transient;
        [SerializeField] private Transform listener;
        [SerializeField, Range(0,1)] private float volume=.85f, movementVolume=.4f;
        [SerializeField, Min(1)] private float hearingDistance=12;
        private Door2D door;
        private System.Random random;
        private int lastStop=-1;
        public int StartsPlayed { get; private set; }
        public int StopsPlayed { get; private set; }
        public bool Sliding => motion != null && motion.isPlaying;
        public void Configure(DoorAudioSet set, AudioSource body, AudioSource click, Transform target)
        { sounds=set; motion=body; transient=click; listener=target; }
        public void ConfigureListener(Transform target) { listener=target; }
        private void Awake()
        {
            door=GetComponent<Door2D>(); random=new System.Random(System.Guid.NewGuid().GetHashCode());
            var context=LevelContext2D.For(this);
            if(listener==null && context!=null && context.Player!=null)listener=context.Player.transform;
            if(listener==null && Camera.main!=null)listener=Camera.main.transform;
        }
        private void OnEnable() { door.MotionStarted+=Begin; door.MotionStopped+=End; }
        private void OnDisable()
        {
            door.MotionStarted-=Begin; door.MotionStopped-=End;
            motion?.Stop(); transient?.Stop();
        }
        private void Begin(bool opening)
        {
            if(sounds==null || motion==null || transient==null || !motion.isActiveAndEnabled || !transient.isActiveAndEnabled)return;
            var clips=opening?sounds.openStart:sounds.closeStart;
            if(clips!=null && clips.Length>0){transient.clip=clips[random.Next(clips.Length)];transient.pitch=1;transient.Play();StartsPlayed++;}
            motion.clip=sounds.slideLoop;motion.loop=true;motion.pitch=1;
            Mix();if(motion.clip!=null)motion.Play();
        }
        private void End(bool opened)
        {
            motion?.Stop();
            // Child sources may already be disabled when Door2D emits its interrupted-motion event.
            if(!isActiveAndEnabled || sounds==null || transient==null || !transient.isActiveAndEnabled || sounds.stop==null || sounds.stop.Length==0)return;
            int index=random.Next(sounds.stop.Length);
            if(sounds.stop.Length>1 && index==lastStop)index=(index+1)%sounds.stop.Length;
            lastStop=index;transient.clip=sounds.stop[index];transient.pitch=1;Mix();transient.Play();StopsPlayed++;
        }
        private void Update() { if((motion!=null&&motion.isPlaying)||(transient!=null&&transient.isPlaying))Mix(); }
        private void Mix()
        {
            if(listener==null)return;
            Vector2 delta=(Vector2)transform.position-(Vector2)listener.position;
            float gain=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2,hearingDistance,delta.magnitude));
            float pan=Mathf.Clamp(delta.x/6,-.75f,.75f);
            if(motion!=null){motion.volume=volume*movementVolume*gain;motion.panStereo=pan;}
            if(transient!=null){transient.volume=volume*gain;transient.panStereo=pan;}
        }
    }
}
