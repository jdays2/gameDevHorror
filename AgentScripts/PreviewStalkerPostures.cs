using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.Enemies.Config;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class PreviewStalkerPostures
{
    public static async Task<string> Capture()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Requires stopped Editor.");
        }
        const string folder = "Concepts/Used/Stalker/Postures04/PreviewFrames";
        Directory.CreateDirectory(folder);
        Scene scene = EditorSceneManager.NewPreviewScene();
        RenderTexture target = null;
        Texture2D pixels = null;
        Sprite grid = null;
        try
        {
            var set = AssetDatabase.LoadAssetAtPath<StalkerSpriteSet>("Assets/EmergencyShift/Data/Enemies/StalkerSprites.asset");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
            var actor = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
            foreach (Behaviour behaviour in actor.GetComponentsInChildren<Behaviour>(true))
            {
                behaviour.enabled = false;
            }
            foreach (Rigidbody2D physics in actor.GetComponentsInChildren<Rigidbody2D>(true))
            {
                physics.simulated = false;
            }
            actor.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Transform visual = actor.transform.Find("Visual");
            var body = visual.Find("Body").GetComponent<SpriteRenderer>();
            var iris = visual.Find("Eye iris").GetComponent<SpriteRenderer>();
            var pupil = visual.Find("Eye pupil").GetComponent<SpriteRenderer>();
            var animator = new SerializedObject(actor.GetComponent<StalkerSpriteAnimator>());
            var eyes = animator.FindProperty("eyeFrames");
            var props = new MaterialPropertyBlock();
            foreach (SpriteRenderer shadow in actor.transform.Find("Ground shadows").GetComponentsInChildren<SpriteRenderer>())
            {
                shadow.enabled = false;
            }
            grid = Sprite.Create(Texture2D.whiteTexture, new Rect(0,0,2,2), new Vector2(.5f,.5f), 2);
            for (int line = -120; line <= 120; line++)
            {
                foreach (bool horizontal in new[] { false, true })
                {
                    var renderer = New("Quarter unit floor grid", scene).AddComponent<SpriteRenderer>();
                    renderer.sprite = grid;
                    renderer.sharedMaterial = body.sharedMaterial;
                    renderer.color = new Color(0,.72f,0);
                    renderer.sortingOrder = -100;
                    renderer.transform.position = horizontal ? new Vector3(0,line*.25f,.2f) : new Vector3(line*.25f,0,.2f);
                    renderer.transform.localScale = horizontal ? new Vector3(60,.006f,1) : new Vector3(.006f,60,1);
                }
            }
            var light = New("Preview light", scene).AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1;
            var camera = New("Sprite review camera", scene).AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = scene;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.gameObject.AddComponent<UniversalAdditionalCameraData>().SetRenderer(0);
            target = new RenderTexture(1280,720,24);
            target.Create();
            pixels = new Texture2D(1280,720,TextureFormat.RGB24,false);
            float phase = 0;
            float displayedClosure = 0;
            Vector2 position = Vector2.zero;
            var metadata = new System.Collections.Generic.List<object>();
            for (int frame = 0; frame < 480; frame++)
            {
                float speed = frame < 120 ? 1.2f : 3.8f;
                if (frame>=120 && frame<132)
                {
                    speed=0;
                }
                if (frame >= 240)
                {
                    speed = frame < 300 || frame >= 450 ? 0 : 1.2f;
                }
                float closure = 0;
                if (frame >= 128 && frame < 132)
                {
                    closure = (frame-128)/3f;
                }
                else if (frame >= 132 && frame < 300)
                {
                    closure = 1;
                }
                else if (frame >= 300 && frame < 320)
                {
                    closure = 1-(frame-300)/20f;
                }
                float targetFold=0;
                if (frame>=120 && frame<132 || frame>=240 && frame<300)
                {
                    targetFold=1;
                }
                else if (frame>=132 && frame<240)
                {
                    targetFold=set.CompactWalkingFold;
                }
                displayedClosure = Mathf.MoveTowards(displayedClosure, targetFold, 1f/(60*set.ClosureSeconds));
                float heading = Mathf.Clamp((frame-360)*1.5f,0,90);
                phase = Mathf.Repeat(phase + speed/(60*set.StrideScale(displayedClosure)),1);
                position += (Vector2)(Quaternion.Euler(0,0,heading)*Vector2.up) * (speed/60);
                actor.transform.position = position;
                visual.rotation = Quaternion.Euler(0,0,heading);
                int selected = set.FrameIndex(phase,displayedClosure);
                body.sprite = set.Frame(selected);
                int eyeFrame = Mathf.RoundToInt(closure*3);
                iris.sprite = (Sprite)eyes.GetArrayElementAtIndex(eyeFrame).objectReferenceValue;
                pupil.enabled = closure<.166f;
                iris.GetPropertyBlock(props);
                props.SetFloat("_EyeFrame",eyeFrame);
                props.SetFloat("_BurnAmount", frame>=120 ? .65f : 0);
                props.SetFloat("_WarningAmount", frame>=90 && frame<120 ? (frame-90)/30f : 0);
                props.SetVector("_AtlasUVPerUnit", new Vector4(iris.sprite.pixelsPerUnit/iris.sprite.texture.width,
                    iris.sprite.pixelsPerUnit/iris.sprite.texture.height,0,0));
                iris.SetPropertyBlock(props);
                camera.transform.position = (Vector3)position + Vector3.back*10;
                camera.backgroundColor = Color.green;
                camera.orthographicSize = 1.5f;
                Save(camera,target,pixels,folder+"/green-"+frame.ToString("D3")+".png");
                if (frame%4==0)
                {
                    camera.backgroundColor = new Color(.075f,.085f,.095f);
                    camera.orthographicSize = 2.4f;
                    Save(camera,target,pixels,folder+"/game-"+(frame/4).ToString("D3")+".png");
                }
                metadata.Add(new { frame, speed, phase, closure, targetFold, displayedClosure, selected, body.sprite.name });
                if (frame%12==0)
                {
                    await Task.Yield();
                }
            }
            File.WriteAllText("Concepts/Used/Stalker/Postures04/preview-timeline.json",
                Newtonsoft.Json.JsonConvert.SerializeObject(metadata,Newtonsoft.Json.Formatting.Indented));
            return "480 green frames at 60 FPS and 120 gameplay frames at 15 FPS; actual prefab, no runtime IK.";
        }
        finally
        {
            RenderTexture.active = null;
            if (pixels != null)
            {
                UnityEngine.Object.DestroyImmediate(pixels);
            }
            if (target != null)
            {
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
            EditorSceneManager.ClosePreviewScene(scene);
            if (grid != null)
            {
                UnityEngine.Object.DestroyImmediate(grid);
            }
        }
    }

    private static void Save(Camera camera, RenderTexture target, Texture2D pixels, string path)
    {
        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=target });
        RenderTexture.active=target;
        pixels.ReadPixels(new Rect(0,0,1280,720),0,0);
        pixels.Apply();
        File.WriteAllBytes(path,pixels.EncodeToPNG());
    }

    private static GameObject New(string name, Scene scene)
    {
        var result=new GameObject(name);
        SceneManager.MoveGameObjectToScene(result,scene);
        return result;
    }
}
