using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using EmergencyShift.Features.Enemies.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class PreviewStalkerRig
{
    public static async Task<string> Capture()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Preview needs stopped Editor.");
        }
        string folder = "Concepts/Used/Stalker/Locomotion02/PreviewFrames";
        Directory.CreateDirectory(folder);
        Scene scene = EditorSceneManager.NewPreviewScene();
        RenderTexture target = null;
        Texture2D image = null;
        Sprite gridSprite = null;
        try
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
            var actors = new GameObject[2];
            var bodies = new SpriteRenderer[2];
            var visuals = new Transform[2];
            StalkerLimbRig2D rig = null;
            Sprite[] walk = null;
            var profile = AssetDatabase.LoadAssetAtPath<StalkerRigProfile>("Assets/EmergencyShift/Data/Enemies/StalkerLimbRig.asset");
            for (int i = 0; i < 2; i++)
            {
                actors[i] = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
                foreach (Behaviour b in actors[i].GetComponentsInChildren<Behaviour>(true))
                {
                    b.enabled = false;
                }
                foreach (Rigidbody2D b in actors[i].GetComponentsInChildren<Rigidbody2D>(true))
                {
                    b.simulated = false;
                }
                visuals[i] = actors[i].transform.Find("Visual");
                bodies[i] = visuals[i].Find("Body").GetComponent<SpriteRenderer>();
                var serialized = new SerializedObject(actors[i].GetComponent<StalkerSpriteAnimator>());
                var frames = serialized.FindProperty("walkFrames");
                if (walk == null)
                {
                    walk = new Sprite[frames.arraySize];
                    for (int f = 0; f < walk.Length; f++)
                    {
                        walk[f] = (Sprite)frames.GetArrayElementAtIndex(f).objectReferenceValue;
                    }
                }
                var contact = actors[i].transform.Find("Ground shadows/Contact shadow").GetComponent<SpriteRenderer>();
                contact.color = new Color(0, 0, 0, .35f);
                actors[i].transform.Find("Ground shadows/Flashlight shadow").GetComponent<SpriteRenderer>().enabled = false;
                if (i == 1)
                {
                    rig = actors[i].GetComponent<StalkerLimbRig2D>();
                    typeof(StalkerLimbRig2D).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(rig, null);
                    typeof(StalkerLimbRig2D).GetField("gait", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rig,
                        new StalkerGaitModel(profile.Limbs, 1, profile.CycleDistance, profile.SupportFraction, profile.LiftHeight, profile.BodyHeight, profile.WeightTransfer));
                    bodies[i].sprite = profile.Torso;
                }
            }
            gridSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f), 2);
            for (int i = -80; i <= 80; i++)
            {
                foreach (bool horizontal in new[] { false, true })
                {
                    var line = New("Floor grid", scene).AddComponent<SpriteRenderer>();
                    line.sprite = gridSprite;
                    line.sharedMaterial = bodies[0].sharedMaterial;
                    line.color = new Color(.13f, .15f, .17f);
                    line.sortingOrder = -100;
                    line.transform.position = horizontal ? new Vector3(0, i * .25f, .2f) : new Vector3(i * .25f, 0, .2f);
                    line.transform.localScale = horizontal ? new Vector3(40, .008f, 1) : new Vector3(.008f, 40, 1);
                }
            }
            var light = New("Preview global light", scene).AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1;
            var camera = New("Comparison camera", scene).AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = scene;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.075f, .085f, .095f);
            camera.gameObject.AddComponent<UniversalAdditionalCameraData>().SetRenderer(0);
            target = new RenderTexture(1280, 720, 24);
            target.Create();
            image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            float distance = 0;
            Vector2 rootPosition = Vector2.zero;
            float visualHeading = 0;
            float headingVelocity = 0;
            float oldMovementWeight = 0;
            const int count = 480;
            for (int frame = 0; frame < count; frame++)
            {
                float speed = 1.2f;
                if (frame >= 180 && frame < 240)
                {
                    speed = 1.5f;
                }
                else if (frame >= 240 && frame < 360)
                {
                    speed = 3.8f;
                }
                if (frame >= 405 && frame < 450)
                {
                    speed = 0;
                }
                distance += speed / 60;
                float heading = Mathf.Clamp((frame - 360) * 2.25f, 0, 90);
                rootPosition += (Vector2)(Quaternion.Euler(0, 0, heading) * Vector2.up) * (speed / 60);
                if (speed > 0)
                {
                    visualHeading = Mathf.SmoothDampAngle(visualHeading, heading, ref headingVelocity, .1f, 160, 1f / 60);
                }
                float phase = Mathf.Repeat(distance / 1.05f, 1);
                float framePhase = phase - Mathf.Sin(phase * Mathf.PI * 4) * .35f / (Mathf.PI * 4);
                for (int actor = 0; actor < 2; actor++)
                {
                    actors[actor].transform.position = (Vector3)rootPosition + new Vector3(actor == 0 ? -1.25f : 1.25f, 0, 0);
                    visuals[actor].rotation = Quaternion.Euler(0, 0, visualHeading);
                }
                bodies[0].sprite = walk[Mathf.Clamp(Mathf.FloorToInt(framePhase * walk.Length), 0, walk.Length - 1)];
                float oldTargetWeight = Mathf.Clamp01(speed / 1.1f);
                float response = oldTargetWeight > oldMovementWeight ? .12f : .1f;
                oldMovementWeight = Mathf.MoveTowards(oldMovementWeight, oldTargetWeight, (1f / 60) / response);
                visuals[0].Rotate(0, 0, Mathf.Sin(phase * Mathf.PI * 2) * .9f * oldMovementWeight);
                rig.Gait.Step(actors[1].transform.position, visualHeading, 1f / 60, true, 0);
                rig.RenderPose();
                Vector3 localOffset = visuals[1].InverseTransformVector(rig.Gait.BodyOffset);
                bodies[1].transform.localPosition = localOffset;
                foreach (string part in new[] { "Eye iris", "Eye pupil", "Eye backing · covers baked pupil" })
                {
                    Transform eye = visuals[1].Find(part);
                    if (eye != null)
                    {
                        eye.localPosition = new Vector3(0, 0, eye.localPosition.z) + localOffset;
                    }
                }
                foreach (GameObject actor in actors)
                {
                    var contact = actor.transform.Find("Ground shadows/Contact shadow").GetComponent<SpriteRenderer>();
                    contact.sprite = actor.transform.Find("Visual/Body").GetComponent<SpriteRenderer>().sprite;
                    contact.transform.position = actor.transform.position + new Vector3(.02f, -.03f, .01f);
                    contact.transform.rotation = actor.transform.Find("Visual").rotation;
                }
                rig.SyncShadows();
                camera.transform.position = (Vector3)rootPosition + new Vector3(0, 0, -10);
                camera.orthographicSize = 1.5f;
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(folder + "/close-" + frame.ToString("D3") + ".png", image.EncodeToPNG());
                if (frame % 4 == 0)
                {
                    camera.orthographicSize = 4.6f;
                    RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                    image.Apply();
                    File.WriteAllBytes(folder + "/game-" + (frame / 4).ToString("D3") + ".png", image.EncodeToPNG());
                    camera.orthographicSize = 2.4f;
                    RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                    image.Apply();
                    File.WriteAllBytes(folder + "/morgue-" + (frame / 4).ToString("D3") + ".png", image.EncodeToPNG());
                    camera.orthographicSize = 3.4f;
                    RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                    image.Apply();
                    File.WriteAllBytes(folder + "/morgue-lit-" + (frame / 4).ToString("D3") + ".png", image.EncodeToPNG());
                }
                if (frame % 12 == 0)
                {
                    await Task.Yield();
                }
            }
            var metadata = new { frames = count, seconds = 8, closeFps = 60, scaleFps = 15,
                cameraOrthoSizes = new[] { 1.5f, 4.6f, 2.4f, 3.4f }, speeds = new[] { 1.2f, 1.5f, 3.8f, 0f },
                reachCorrections = rig.Gait.ReachCorrections, recoverySteps = rig.Gait.RecoverySteps, initialPhase = 0,
                timeline = "0-3 walk 1.2; 3-4 walk 1.5; 4-6 retreat 3.8; 6-6.75 turn 1.2; 6.75-7.5 stop; 7.5-8 resume 1.2",
                scope = "Controlled actual prefab + gait-model URP render, no AI/physics. Live captures are separate.",
                shadowShader = profile.ShadowMaterial.shader.name };
            File.WriteAllText("Concepts/Used/Stalker/Locomotion02/preview-metadata.json", Newtonsoft.Json.JsonConvert.SerializeObject(metadata, Newtonsoft.Json.Formatting.Indented));
            return count + " close frames + three game camera scales; reach corrections=" + rig.Gait.ReachCorrections;
        }
        finally
        {
            RenderTexture.active = null;
            if (image != null)
            {
                UnityEngine.Object.DestroyImmediate(image);
            }
            if (target != null)
            {
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
            EditorSceneManager.ClosePreviewScene(scene);
            if (gridSprite != null)
            {
                UnityEngine.Object.DestroyImmediate(gridSprite);
            }
        }
    }

    private static GameObject New(string name, Scene scene)
    {
        var obj = new GameObject(name);
        SceneManager.MoveGameObjectToScene(obj, scene);
        return obj;
    }
}
