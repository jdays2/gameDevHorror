#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift
{
    /// <summary>Временный режиссёр и запись реального Game View. Добавляется только в Play, не сохраняется в сцену.</summary>
    public sealed class StalkerDemoRecorder : MonoBehaviour
    {
        [Serializable] public sealed class Sample
        {
            public int frame;
            public float seconds;
            public string phase;
            public string state;
            public Vector2 player;
            public Vector2 enemy;
            public Vector2 velocity;
            public Vector2 pupil;
            public bool exposed;
            public bool eyeOpen;
            public float closure;
            public int spriteFrame;
            public int playerHealth;
            public bool playerDead;
            public float lightReaction;
        }
        [Serializable] public sealed class RecordingReport
        {
            public int fps;
            public int width;
            public int height;
            public bool complete;
            public string note;
            public List<Sample> samples;
        }

        public int FramesWritten { get; private set; }
        public bool Finished { get; private set; }
        public string Failure { get; private set; }
        private const int Fps = 24;
        private const float Duration = 40;
        private string folder;
        private PlayerMotor2D motor;
        private Flashlight2D lamp;
        private Rigidbody2D playerBody;
        private ShadowStalker2D enemy;
        private Rigidbody2D enemyBody;
        private StalkerSpriteAnimator visuals;
        private Light2D ambient;
        private Camera worldCamera;
        private PlayerLife2D life;
        private PrototypeHud hud;
        private CameraFollow2D follow;
        private ShadowStalker2D extraPrefab;
        private bool killed,secondCorpse;
        private int oldCapture;
        private bool oldBackground, oldMotor, oldLamp, oldAttacks, oldHud, oldFollow;
        private float oldAmbient, oldZoom;
        private bool restored;
        private readonly List<Sample> samples = new List<Sample>();
        private float Seconds => FramesWritten / (float)Fps;
        private string Phase => Seconds < 7 ? "inspection" : Seconds < 12 ? "shadow" : Seconds < 18 ? "light" : Seconds < 23 ? "return" : Seconds < 27 ? "inspection-return" : Seconds < 34 ? "attack" : "death";

        public void Begin(string absoluteFolder,ShadowStalker2D extraCreaturePrefab=null)
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Запись разрешена только в Play.");
            folder = Path.GetFullPath(absoluteFolder);
            Directory.CreateDirectory(folder);
            motor = UnityEngine.Object.FindAnyObjectByType<PlayerMotor2D>();
            lamp = UnityEngine.Object.FindAnyObjectByType<Flashlight2D>();
            enemy = UnityEngine.Object.FindAnyObjectByType<ShadowStalker2D>();
            if (motor == null || lamp == null || enemy == null) throw new InvalidOperationException("Нет игрока, фонаря или врага.");
            playerBody = motor.GetComponent<Rigidbody2D>();
            enemyBody = enemy.GetComponent<Rigidbody2D>();
            visuals = enemy.GetComponent<StalkerSpriteAnimator>();
            foreach (Light2D light in UnityEngine.Object.FindObjectsByType<Light2D>())
                if (light.lightType == Light2D.LightType.Global) { ambient = light; break; }
            worldCamera = Camera.main;
            life=motor.GetComponent<PlayerLife2D>();hud=UnityEngine.Object.FindAnyObjectByType<PrototypeHud>();
            follow=worldCamera.GetComponent<CameraFollow2D>();extraPrefab=extraCreaturePrefab;
            oldCapture = Time.captureFramerate; oldBackground = Application.runInBackground;
            oldMotor = motor.enabled; oldLamp = lamp.enabled; oldAmbient = ambient.intensity; oldZoom = worldCamera.orthographicSize;
            oldAttacks=enemy.AttacksEnabled;oldHud=hud.enabled;oldFollow=follow.enabled;
            enemy.AttacksEnabled=false;
            motor.enabled = false; lamp.enabled = false;
            playerBody.position = new Vector2(-3.2f, 0.3f);
            enemyBody.position = new Vector2(-6f, -0.6f);
            playerBody.linearVelocity = enemyBody.linearVelocity = Vector2.zero;
            worldCamera.transform.position = new Vector3(playerBody.position.x, playerBody.position.y, -10);
            lamp.SetPower(false); lamp.AimAt(playerBody.position + Vector2.right * 10);
            Time.captureFramerate = Fps;
            Application.runInBackground = true;
            StartCoroutine(Record());
        }

        private void FixedUpdate()
        {
            if (folder == null || Finished || playerBody == null) return;
            float t = Seconds;
            if(t>=27 || (life!=null&&life.IsDead)){playerBody.linearVelocity=Vector2.zero;return;}
            Vector2 goal = t < 7 ? new Vector2(-3.2f + 0.65f*t, 0.3f + 0.35f*Mathf.Sin(t*0.8f))
                : t < 12 ? new Vector2(1.35f + (t-7)*0.1f, 0.3f + 0.35f*Mathf.Sin(5.6f))
                : t < 23 ? new Vector2(1.85f,0.3f + 0.35f*Mathf.Sin(5.6f))
                : new Vector2(1.85f + Mathf.Sin((t-23)*1.6f)*0.3f,0.1f);
            playerBody.linearVelocity = Vector2.ClampMagnitude((goal-playerBody.position)/Time.fixedDeltaTime, 1.2f);
        }

        private void LateUpdate()
        {
            if (folder == null || Finished || lamp == null) return;
            float t = Seconds;
            ambient.intensity = t < 7 || t >= 23 ? 0.55f : oldAmbient;
            worldCamera.orthographicSize = t < 7 || t >= 23 ? 3.6f : oldZoom;
            enemy.AttacksEnabled=t>=27;
            lamp.SetPower(t >= 7 && t<27);
            lamp.AimAt(t >= 12 && t < 18 ? enemyBody.position : playerBody.position + (t < 7 ? Vector2.right : Vector2.up)*10);
            if(t>=34)
            {
                if(!killed){enemy.TakeDamage(int.MaxValue);killed=true;}
                hud.enabled=false;hud.transform.Find("Defeat prompt").gameObject.SetActive(false);
                follow.enabled=false;
                worldCamera.transform.position=new Vector3(enemy.transform.position.x-0.8f,enemy.transform.position.y+0.4f,-10);
                worldCamera.orthographicSize=3.2f;
                if(t>=36 && !secondCorpse && extraPrefab!=null)
                {
                    var copy=Instantiate(extraPrefab,enemy.transform.position+new Vector3(-1.8f,0.9f,0),Quaternion.identity);
                    copy.TakeDamage(int.MaxValue);secondCorpse=true;
                }
            }
        }

        private IEnumerator Record()
        {
            // WaitForEndOfFrame и ScreenCapture захватывают Game View вместе с настоящим 2D-светом и UI.
            yield return new WaitForEndOfFrame();
            while (FramesWritten < Duration * Fps)
            {
                yield return new WaitForEndOfFrame();
                Texture2D texture = null;
                try
                {
                    texture = ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(Path.Combine(folder, "frame-" + FramesWritten.ToString("0000") + ".png"), texture.EncodeToPNG());
                    samples.Add(new Sample {
                        frame=FramesWritten,seconds=Seconds,phase=Phase,state=enemy.State.ToString(),
                        player=playerBody.position,enemy=enemyBody.position,velocity=enemy.Velocity,
                        pupil=visuals.PupilOffset,exposed=enemy.Exposed,eyeOpen=visuals.EyeOpen,
                        closure=enemy.Closure,spriteFrame=visuals.CurrentFrame,playerHealth=life.Health,
                        playerDead=life.IsDead,lightReaction=enemy.LightReactionProgress });
                    FramesWritten++;
                }
                catch (Exception ex) { Failure = ex.ToString(); break; }
                finally { if (texture != null) Destroy(texture); }
            }
            Finished = true;
            var firstPath = Path.Combine(folder,"frame-0000.png");
            var first = new Texture2D(2,2);
            if (File.Exists(firstPath)) first.LoadImage(File.ReadAllBytes(firstPath));
            var report = new RecordingReport {
                fps=Fps,width=first.width,height=first.height,complete=Failure==null&&FramesWritten==Duration*Fps,
                note="Real Unity Game View. Player movement/aim scripted. Inspection/attack/death phases use temporary ambient 0.55; shadow/light phases retain scene darkness. Attacks disabled only in the first 27 seconds to isolate locomotion/light response. At 27s real combat is enabled. At 34s lethal test damage, at 36s second corpse; no weapon is implemented. Actor position/tuning in the saved scene is preserved. Enemy AI, collisions, sprites, tracked pupil, damage and gore run in Unity.",samples=samples };
            File.WriteAllText(Path.Combine(folder,"recording.json"), JsonUtility.ToJson(report,true));
            Destroy(first);
            Restore();
            Debug.Log("STALKER_RECORDING_FINISHED: " + FramesWritten + " frames; " + (Failure??"OK"));
        }

        private void OnGUI()
        {
            if (folder == null || Finished) return;
            string caption = Phase == "inspection" ? "ПРОВЕРКА СПРАЙТОВ · временная подсветка"
                : Phase == "shadow" ? "В ТЕНИ · существо подбирается сзади"
                : Phase == "light" ? "ЛУЧ НА СУЩЕСТВЕ · сжатие и отход"
                : Phase == "return" ? "ЛУЧ ОТВЕРНУТ · пауза перед возвращением"
                : Phase == "inspection-return" ? "ПРОВЕРКА ВЗГЛЯДА · временная подсветка"
                : Phase == "attack" ? "АТАКА · подготовка, укус, поражение игрока"
                : "ГИБЕЛЬ СТАЛКЕРА · тестовый урон, кровь и куски";
            GUI.color = new Color(0,0,0,0.75f); GUI.DrawTexture(new Rect(0,0,Screen.width,48), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var style = new GUIStyle(GUI.skin.label) { fontSize=19, alignment=TextAnchor.MiddleCenter };
            GUI.Label(new Rect(0,0,Screen.width,48),caption,style);
        }

        private void Restore()
        {
            if (restored || folder == null) return; restored = true;
            Time.captureFramerate = oldCapture; Application.runInBackground = oldBackground;
            bool dead=life!=null&&life.IsDead;
            if (motor != null) motor.enabled = oldMotor&&!dead;
            if (lamp != null) lamp.enabled = oldLamp&&!dead;
            if(enemy!=null)enemy.AttacksEnabled=oldAttacks;
            if(hud!=null)hud.enabled=oldHud;
            if(follow!=null)follow.enabled=oldFollow;
            if (ambient != null) ambient.intensity = oldAmbient;
            if (worldCamera != null) worldCamera.orthographicSize = oldZoom;
            if (playerBody != null) playerBody.linearVelocity = Vector2.zero;
        }
        private void OnDisable() => Restore();
    }
}
#endif

