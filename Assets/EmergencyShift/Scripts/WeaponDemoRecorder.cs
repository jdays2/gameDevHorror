#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Только Editor: одинаковая постановка трёх вариантов с настоящими пулями/светом/уроном.</summary>
    public sealed class WeaponDemoRecorder : MonoBehaviour
    {
        public const int Fps = 30;
        public const int FramesPerStyle = 150;
        private string folder;
        private ShotVisualProfile[] styles;
        private GameObject targetPrefab;
        private PlayerWeapon2D weapon;
        private Flashlight2D lamp;
        private Transform player;
        private ShadowStalker2D target;
        private int phase = -1;
        private bool firstShot, secondShot, wallShot;
        private int oldCapture;
        private bool oldBackground;
        public int FramesWritten { get; private set; }
        public bool Finished { get; private set; }
        public string Failure { get; private set; }
        [Serializable] private sealed class GibSample
        {
            public Vector2 position,velocity; public float height; public bool settled;
        }
        [Serializable] private sealed class EffectSample
        {
            public int frame,phase,health; public float maxLight; public int spray;
            public int hitSprays,hitsShown; public bool staggered,playerShadow;
            public List<GibSample> gibs=new List<GibSample>();
        }
        [Serializable] private sealed class EffectReport { public List<EffectSample> samples; }
        private readonly List<EffectSample> effectSamples=new List<EffectSample>();

        public void Begin(string directory, GameObject enemyPrefab, ShotVisualProfile[] profiles)
        {
            folder = directory; Directory.CreateDirectory(folder); targetPrefab = enemyPrefab; styles = profiles;
            oldCapture = Time.captureFramerate; oldBackground = Application.runInBackground;
            Time.captureFramerate = Fps; Application.runInBackground = true;
            weapon = UnityEngine.Object.FindAnyObjectByType<PlayerWeapon2D>(); player = weapon.transform;
            player.GetComponent<PlayerMotor2D>().enabled = false; player.GetComponent<PlayerInteraction>().enabled = false;
            player.GetComponent<Rigidbody2D>().simulated = false; player.position = new Vector3(-1, 0, 0);
            lamp = player.GetComponent<Flashlight2D>(); lamp.enabled = false;
            foreach (ShadowStalker2D enemy in UnityEngine.Object.FindObjectsByType<ShadowStalker2D>()) enemy.gameObject.SetActive(false);
            var corpses = GameObject.Find("Test corpses — Stalkers"); if (corpses != null) corpses.SetActive(false);
            foreach (FlashlightUpgradePickup item in UnityEngine.Object.FindObjectsByType<FlashlightUpgradePickup>()) item.gameObject.SetActive(false);
            foreach (WeaponLootPickup item in UnityEngine.Object.FindObjectsByType<WeaponLootPickup>()) item.gameObject.SetActive(false);
            Camera camera = Camera.main; camera.GetComponent<CameraFollow2D>().enabled = false;
            camera.transform.position = new Vector3(.8f, 0, -10); camera.orthographicSize = 2.9f;
            weapon.Acquire(12);
            StartCoroutine(Record());
        }

        private void Update()
        {
            if (folder == null || Finished) return;
            int next = Mathf.Min(2, FramesWritten / FramesPerStyle);
            if (next != phase)
            {
                if (target != null) Destroy(target.gameObject);
                foreach (BloodPool2D pool in UnityEngine.Object.FindObjectsByType<BloodPool2D>()) if (pool.name == "Blood pool") Destroy(pool.gameObject);
                foreach (GibMotion2D gib in UnityEngine.Object.FindObjectsByType<GibMotion2D>()) if (gib.name == "Stalker fragment") Destroy(gib.gameObject);
                phase = next; firstShot = secondShot = wallShot = false; weapon.SetVisualProfile(styles[phase]);
                GameObject obj = Instantiate(targetPrefab, new Vector3(1.7f, 0, 0), Quaternion.identity);
                target = obj.GetComponent<ShadowStalker2D>(); target.Configure(player, lamp, 1 << 8);
                target.AttacksEnabled = false; target.enabled = false;
                target.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            }
            int frame = FramesWritten % FramesPerStyle;
            // После настоящего попадания вернуть FixedUpdate: он продвигает время кадров смерти.
            if (target != null && target.Health == 0) target.enabled = true;
            lamp.AimAt(new Vector2(1.7f, 0));
            // Первый выстрел без фонаря; после второго включить луч для осмотра самого разрыва.
            lamp.SetPower(frame < 15 || (frame >= 34 && frame < 70) || frame >= 95);
            if (frame >= 18 && !firstShot) firstShot = weapon.TryFire();
            if (frame >= 32 && !secondShot) secondShot = weapon.TryFire();
            if (frame >= 76 && !wallShot) wallShot = weapon.TryFire();
        }

        private IEnumerator Record()
        {
            yield return new WaitForEndOfFrame();
            while (FramesWritten < FramesPerStyle * 3)
            {
                yield return new WaitForEndOfFrame();
                Texture2D texture = null;
                try
                {
                    texture = ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(Path.Combine(folder, "frame-" + FramesWritten.ToString("0000") + ".png"), texture.EncodeToPNG());
                    var sample=new EffectSample{frame=FramesWritten,phase=phase,health=target!=null?target.Health:2,
                        spray=UnityEngine.Object.FindObjectsByType<BloodRuptureBurst2D>().Length};
                    sample.hitSprays=UnityEngine.Object.FindObjectsByType<BloodHitSpray2D>().Length;
                    if(target!=null){sample.hitsShown=target.GetComponent<StalkerHitResponse2D>().HitsShown;sample.staggered=target.IsStaggered;}
                    var shadow=player.GetComponent<PlayerShotShadow2D>();sample.playerShadow=shadow!=null&&shadow.Visible;
                    foreach(var gib in UnityEngine.Object.FindObjectsByType<GibMotion2D>())
                        sample.gibs.Add(new GibSample{position=gib.transform.position,velocity=gib.Velocity,height=gib.Height,settled=gib.Settled});
                    foreach(var effect in UnityEngine.Object.FindObjectsByType<ShotEffect2D>())
                        sample.maxLight=Mathf.Max(sample.maxLight,effect.GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>().intensity);
                    effectSamples.Add(sample);
                    FramesWritten++;
                }
                catch (Exception ex) { Failure = ex.ToString(); break; }
                finally { if (texture != null) Destroy(texture); }
            }
            Finished = true; Time.captureFramerate = oldCapture; Application.runInBackground = oldBackground;
            File.WriteAllText(Path.Combine(folder,"effect-samples.json"),JsonUtility.ToJson(new EffectReport{samples=effectSamples},true));
        }

        private void OnGUI()
        {
            if (folder == null || Finished || phase < 0) return;
            GUI.color = new Color(0, 0, 0, .7f); GUI.DrawTexture(new Rect(0, 0, Screen.width, 45), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 19, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(0, 0, Screen.width, 45), (phase + 1) + " · " + styles[phase].displayName + " · одинаковые урон и расход", style);
        }

        private void OnDestroy()
        {
            if (folder == null) return;
            Time.captureFramerate = oldCapture; Application.runInBackground = oldBackground;
        }
    }
}
#endif
