using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class PreviewMonsterEyes03
{
    private const string Output = "Docs/Validation/MonsterEyes03";

    public static async Task<string> Eyes()
    {
        LevelContext2D context = Prepare();
        Camera camera = Camera.main;
        camera.transform.position = new Vector3(100, 100, -10);
        camera.orthographicSize = .8f;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
        var actor = context.CreateInstance(prefab, new Vector3(100, 100, 0), Quaternion.identity).GetComponent<ShadowStalker2D>();
        actor.AttacksEnabled = false;
        actor.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        actor.enabled = false;
        MovePlayer(context, new Vector2(102.8f, 100));
        context.Flashlight.Acquire();
        context.Flashlight.AimAt(actor.transform.position);
        context.Flashlight.SetPower(true);
        await Task.Delay(80);
        var samples = new List<object>();
        await Capture("eye-healthy", actor, samples);
        actor.enabled = true;
        await Task.Delay(400);
        actor.enabled = false;
        await Capture("eye-warning", actor, samples);
        foreach (float damage in new[] { .35f, .9f })
        {
            ShadowStalker2D.Snapshot state = actor.CaptureState();
            state.state = ShadowStalker2D.BehaviourState.Sneaking;
            state.age = 0;
            state.closure = 0;
            state.hasEyeDamageProgress = true;
            state.eyeDamage = damage;
            state.burnContact = 0;
            state.exposure = 0;
            actor.RestoreState(state);
            actor.enabled = false;
            await Capture(damage < .5f ? "eye-mild" : "eye-clouded", actor, samples);
        }

        camera.orthographicSize = 5.4f;
        await Capture("eye-clouded-game-scale", actor, samples);
        SetLighting(.08f);
        await Capture("eye-clouded-dark", actor, samples);
        ShadowStalker2D.Snapshot healthy = actor.CaptureState();
        healthy.eyeDamage = 0;
        actor.RestoreState(healthy);
        actor.enabled = false;
        await Capture("eye-healthy-dark", actor, samples);
        string json = JsonConvert.SerializeObject(samples, Formatting.Indented);
        File.WriteAllText(Output + "/eye-preview.json", json);
        return json;
    }

    public static async Task<string> Remains()
    {
        LevelContext2D context = Prepare();
        context.Flashlight.SetPower(false);
        MovePlayer(context, new Vector2(120, 120));
        Camera camera = Camera.main;
        var samples = new List<object>();
        foreach (string species in new[] { "ShadowStalker", "Sleeper" })
        {
            foreach (RemainsRecord2D prior in Roots().SelectMany(root => root.GetComponentsInChildren<RemainsRecord2D>(true)))
            {
                prior.gameObject.SetActive(false);
            }

            foreach (EnemyHealth2D prior in Roots().SelectMany(root => root.GetComponentsInChildren<EnemyHealth2D>(true)))
            {
                prior.gameObject.SetActive(false);
            }

            SetLighting(.65f);
            camera.transform.position = new Vector3(102, 100, -10);
            camera.orthographicSize = 3.4f;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/" + species + ".prefab");
            GameObject living = context.CreateInstance(prefab, new Vector3(98, 100, 0), Quaternion.identity);
            Freeze(living);
            var bursts = new List<GoreBurst2D>();
            for (int index = 0; index < 2; index++)
            {
                bool withEye = index == 0;
                GameObject actor = context.CreateInstance(prefab, new Vector3(102 + index * 4, 100, 0), Quaternion.identity);
                Freeze(actor);
                var burst = actor.GetComponent<GoreBurst2D>();
                var fields = new SerializedObject(burst);
                var original = (GoreProfile)fields.FindProperty("profile").objectReferenceValue;
                GoreVariant variant = Enumerable.Range(0, original.VariantCount).Select(original.VariantAt)
                    .First(value => !value.RestorationOnly && value.Id.EndsWith(withEye ? "-eye03" : "-flesh03", StringComparison.Ordinal));
                var profile = Object.Instantiate(original);
                JsonUtility.FromJsonOverwrite("{\"variants\":[" + JsonUtility.ToJson(variant) + "]}", profile);
                fields.FindProperty("profile").objectReferenceValue = profile;
                fields.ApplyModifiedPropertiesWithoutUndo();
                var stalker = actor.GetComponent<ShadowStalker2D>();
                if (stalker != null)
                {
                    ShadowStalker2D.Snapshot state = stalker.CaptureState();
                    state.eyeDamage = .65f;
                    state.hasEyeDamageProgress = true;
                    stalker.RestoreState(state);
                    stalker.enabled = false;
                }

                actor.GetComponent<EnemyHealth2D>().Kill();
                bursts.Add(burst);
            }

            float deadline = Time.realtimeSinceStartup + 10;
            while (bursts.Any(value => !value.IsSettled) && Time.realtimeSinceStartup < deadline)
            {
                await Task.Delay(100);
            }

            if (bursts.Any(value => !value.IsSettled))
            {
                throw new InvalidOperationException("Preview fragments did not settle.");
            }

            await Task.Delay(200);
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Output + "/remains-" + species + ".png"));
            samples.Add(new { species, layout = "living at x98 / eye outcome at x102 / soft flesh outcome at x106",
                remains = bursts.Select(value => JsonConvert.DeserializeObject(JsonUtility.ToJson(value.CaptureRemains()))).ToArray() });
            await Task.Delay(250);
        }

        string json = JsonConvert.SerializeObject(samples, Formatting.Indented);
        File.WriteAllText(Output + "/remains-preview.json", json);
        return Output + "/remains-preview.json";
    }

    private static void Freeze(GameObject actor)
    {
        actor.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        var stalker = actor.GetComponent<ShadowStalker2D>();
        if (stalker != null)
        {
            stalker.AttacksEnabled = false;
            stalker.enabled = false;
        }

        var sleeper = actor.GetComponent<Sleeper2D>();
        if (sleeper != null)
        {
            sleeper.enabled = false;
        }
    }

    private static async Task Capture(string stage, ShadowStalker2D actor, List<object> samples)
    {
        await Task.Delay(180);
        ScreenCapture.CaptureScreenshot(Path.GetFullPath(Output + "/" + stage + ".png"));
        samples.Add(new { stage, frame = Time.frameCount, actor.EyeDamageProgress, actor.EyeWarning, actor.Closure,
            pupilScale = actor.GetComponentsInChildren<SpriteRenderer>().First(value => value.name == "Eye pupil").transform.localScale.x });
        await Task.Delay(180);
    }

    private static GameObject[] Roots() => SceneManager.GetActiveScene().GetRootGameObjects();

    private static LevelContext2D Prepare()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "MorgueRouteTest")
        {
            throw new InvalidOperationException("Preview requires ordinary Play in the authored MorgueRouteTest.");
        }

        Directory.CreateDirectory(Output);
        LevelContext2D context = Roots().SelectMany(root => root.GetComponentsInChildren<LevelContext2D>(true)).Single();
        foreach (EnemyHealth2D actor in Roots().SelectMany(root => root.GetComponentsInChildren<EnemyHealth2D>(true)))
        {
            actor.gameObject.SetActive(false);
        }

        foreach (Canvas canvas in Roots().SelectMany(root => root.GetComponentsInChildren<Canvas>(true)))
        {
            canvas.enabled = false;
        }

        context.Player.GetComponent<PlayerMotor2D>().enabled = false;
        context.Player.GetComponent<PlayerAim2D>().enabled = false;
        context.Player.GetComponent<PlayerWeapon2D>().enabled = false;
        Camera.main.GetComponent<CameraFollow2D>().enabled = false;
        SetLighting(.65f);
        return context;
    }

    private static void SetLighting(float intensity)
    {
        foreach (Light2D light in Roots().SelectMany(root => root.GetComponentsInChildren<Light2D>(true)))
        {
            if (light.lightType == Light2D.LightType.Global)
            {
                light.color = Color.white;
                light.intensity = intensity;
            }
        }
    }

    private static void MovePlayer(LevelContext2D context, Vector2 position)
    {
        context.Player.GetComponent<Rigidbody2D>().position = position;
        context.Player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        context.Player.transform.position = position;
        Physics2D.SyncTransforms();
    }
}
