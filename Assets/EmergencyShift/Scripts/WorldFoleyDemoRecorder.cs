#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using Unity.Collections;
using UnityEngine;
namespace EmergencyShift
{
    /// <summary>Temporary Play-only recording of actual door events and collected prefab loot.</summary>
    public sealed class WorldFoleyDemoRecorder : MonoBehaviour
    {
        private string folder;
        private PlayerInteraction player;
        private Door2D door;
        private DoorAudio2D doorAudio;
        private PlayerAudio2D audioPlayer;
        private BinaryWriter writer;
        private int previousCapture, stage;
        private bool previousBackground, rendering;
        public int Frames { get; private set; }
        public bool Finished { get; private set; }
        public string Failure { get; private set; }
        public float Peak { get; private set; }
        public void Begin(string output)
        {
            folder=output;Directory.CreateDirectory(folder);
            foreach(var enemy in FindObjectsByType<ShadowStalker2D>())enemy.gameObject.SetActive(false);
            foreach(var item in FindObjectsByType<WeaponLootPickup>())item.gameObject.SetActive(false);
            foreach(var item in FindObjectsByType<FlashlightUpgradePickup>())item.gameObject.SetActive(false);
            player=FindFirstObjectByType<PlayerInteraction>();audioPlayer=player.GetComponent<PlayerAudio2D>();
            typeof(PlayerLife2D).GetField("<Health>k__BackingField",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(player.GetComponent<PlayerLife2D>(),2);
            player.enabled=true;player.GetComponent<PlayerWeapon2D>().enabled=true;
            player.GetComponent<PlayerMotor2D>().enabled=false;player.GetComponent<Rigidbody2D>().simulated=false;
            door=FindFirstObjectByType<Door2D>();doorAudio=door.GetComponent<DoorAudio2D>();
            player.transform.position=(Vector3)door.InteractionPosition+Vector3.left*.8f;
            previousCapture=Time.captureFramerate;previousBackground=Application.runInBackground;
            Time.captureFramerate=30;Application.runInBackground=true;
            if(!AudioRenderer.Start())throw new InvalidOperationException("Audio capture already active.");rendering=true;
            writer=new BinaryWriter(File.Open(Path.Combine(folder,"unity-mix.f32"),FileMode.Create));StartCoroutine(Capture());
        }
        private void Update()
        {
            if(folder==null||Finished)return;
            if(stage==0&&Frames>=9){door.Toggle();stage++;}
            else if(stage==1&&Frames>=42){door.Toggle();stage++;}
            else if(stage==2&&Frames>=78){Loot("AmmoPickup");stage++;}
            else if(stage==3&&Frames>=120){Loot("PistolPickup");stage++;}
            else if(stage==4&&Frames>=168){Loot("FlashlightRangeUpgrade");stage++;}
        }
        private void Loot(string name)
        {
            var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/"+name+".prefab");
            Instantiate(prefab,player.transform.position+new Vector3(.1f,.1f,-.045f),Quaternion.identity);
            Physics2D.SyncTransforms();player.CollectNearbyLoot();
        }
        private IEnumerator Capture()
        {
            while(Frames<210)
            {
                yield return new WaitForEndOfFrame();
                try
                {
                    int samples=AudioRenderer.GetSampleCountForCaptureFrame();
                    using(var b=new NativeArray<float>(samples*2,Allocator.Temp))
                    {if(!AudioRenderer.Render(b))throw new IOException("Audio render failed");foreach(float x in b){writer.Write(x);Peak=Mathf.Max(Peak,Mathf.Abs(x));}}
                    Frames++;
                }
                catch(Exception e){Failure=e.ToString();break;}
            }
            Finished=true;Restore();
            string report=UnityEngine.JsonUtility.ToJson(new Result {frames=Frames,peak=Peak,starts=doorAudio.StartsPlayed,stops=doorAudio.StopsPlayed,sliding=doorAudio.Sliding,
                ammo=audioPlayer.PickupCount(PickupKind.Ammo),weapon=audioPlayer.PickupCount(PickupKind.Weapon),upgrade=audioPlayer.PickupCount(PickupKind.FlashlightUpgrade),rate=AudioSettings.outputSampleRate,failure=Failure},true);
            File.WriteAllText(Path.Combine(folder,"recording.json"),report);
        }
        [Serializable] private sealed class Result {public int frames,starts,stops,ammo,weapon,upgrade,rate;public float peak;public bool sliding;public string failure;}
        private void Restore(){if(rendering){AudioRenderer.Stop();rendering=false;}writer?.Dispose();writer=null;Time.captureFramerate=previousCapture;Application.runInBackground=previousBackground;}
        private void OnDestroy(){if(folder!=null)Restore();}
    }
}
#endif
