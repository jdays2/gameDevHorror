#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Временная запись Game View. Позиции/камера/заморозка существуют только в текущем Play.</summary>
    public sealed class LightingDemoRecorder : MonoBehaviour
    {
        [Serializable] private sealed class Sample {public int frame;public bool flashlight;public float red,blue;}
        [Serializable] private sealed class Report {public List<Sample> samples;}
        private readonly List<Sample> samples=new List<Sample>();
        private PlayerWeapon2D player;
        private Flashlight2D flashlight;
        private Camera camera;
        private LocalLamp2D red,blue;
        private string folder;
        private int oldCapture;
        private bool oldBackground;
        public int FramesWritten {get;private set;}
        public bool Finished {get;private set;}
        public string Failure {get;private set;}
        public void Begin(string directory)
        {
            folder=directory;Directory.CreateDirectory(folder);oldCapture=Time.captureFramerate;oldBackground=Application.runInBackground;
            Time.captureFramerate=30;Application.runInBackground=true;
            player=FindAnyObjectByType<PlayerWeapon2D>();flashlight=player.GetComponent<Flashlight2D>();camera=Camera.main;
            player.GetComponent<PlayerMotor2D>().enabled=false;player.GetComponent<Rigidbody2D>().simulated=false;flashlight.enabled=false;
            foreach(var e in FindObjectsByType<ShadowStalker2D>()){e.enabled=false;e.AttacksEnabled=false;e.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;}
            camera.GetComponent<CameraFollow2D>().enabled=false;
            var lights=GameObject.Find("Occlusion playground").transform.Find("Local lights");
            red=lights.Find("Corridor emergency lamp").GetComponent<LocalLamp2D>();blue=lights.Find("Flickering lamp — Room B").GetComponent<LocalLamp2D>();
            StartCoroutine(Record());
        }
        private void Update()
        {
            if(folder==null||Finished)return;int f=FramesWritten;
            camera.orthographicSize=f<180?4.8f:4.2f;camera.transform.position=f<180?new Vector3(-2,0,-10):new Vector3(13.5f,.3f,-10);
            if(f<90){player.transform.position=new Vector3(2.8f,0,0);flashlight.SetPower(false);}
            else if(f<180){player.transform.position=new Vector3(.3f,-.8f,0);flashlight.SetPower(true);flashlight.AimAt(new Vector2(-3,-2.15f));}
            else if(f<270){player.transform.position=new Vector3(11.2f,1.2f,0);flashlight.SetPower(false);}
            else if(f<360){player.transform.position=new Vector3(11.2f,1.2f,0);flashlight.SetPower(true);flashlight.AimAt(new Vector2(14.3f,1.1f));}
            else{player.transform.position=new Vector3(14.8f,3.1f,0);flashlight.SetPower(true);flashlight.AimAt(new Vector2(14.3f,1.1f));}
        }
        private IEnumerator Record()
        {
            while(FramesWritten<450)
            {
                yield return new WaitForEndOfFrame();Texture2D image=null;
                try
                {
                    image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"frame-"+FramesWritten.ToString("0000")+".png"),image.EncodeToPNG());
                    samples.Add(new Sample{frame=FramesWritten,flashlight=flashlight.IsOn,red=red.CurrentIntensity,blue=blue.CurrentIntensity});FramesWritten++;
                }
                catch(Exception e){Failure=e.ToString();break;}
                finally{if(image!=null)Destroy(image);}
            }
            Finished=true;Time.captureFramerate=oldCapture;Application.runInBackground=oldBackground;
            File.WriteAllText(Path.Combine(folder,"lighting-samples.json"),JsonUtility.ToJson(new Report{samples=samples},true));
        }
        private void OnGUI()
        {
            if(folder==null||Finished)return;
            string label=FramesWritten<90?"Границы видны; внутри густая темнота":FramesWritten<180?"Фонарь и укрытия":FramesWritten<270?"Локальные лампы разной яркости":FramesWritten<360?"Высокий шкаф перекрывает свет":"Обходим шкаф — осматриваем врага и лут";
            GUI.color=new Color(0,0,0,.8f);GUI.DrawTexture(new Rect(0,0,Screen.width,44),Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.Label(new Rect(0,0,Screen.width,44),label,new GUIStyle(GUI.skin.label){fontSize=19,alignment=TextAnchor.MiddleCenter});
        }
        private void OnDestroy(){if(folder!=null){Time.captureFramerate=oldCapture;Application.runInBackground=oldBackground;}}
    }
}
#endif
