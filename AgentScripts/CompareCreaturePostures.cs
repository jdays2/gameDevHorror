using System;
using System.IO;
using EmergencyShift;
using EmergencyShift.Features.Enemies.Config;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class CompareCreaturePostures
{
    public static string Capture()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Requires stopped Editor.");
        }
        Scene scene=EditorSceneManager.NewPreviewScene();
        RenderTexture target=null;
        Texture2D pixels=null;
        try
        {
            var sprites=AssetDatabase.LoadAssetAtPath<StalkerSpriteSet>("Assets/EmergencyShift/Data/Enemies/StalkerSprites.asset");
            for (int index=0;index<4;index++)
            {
                string family=index==3 ? "Sleeper" : "ShadowStalker";
                var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/"+family+".prefab");
                var actor=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);
                foreach (Behaviour behaviour in actor.GetComponentsInChildren<Behaviour>(true))
                {
                    behaviour.enabled=false;
                }
                foreach (Rigidbody2D physics in actor.GetComponentsInChildren<Rigidbody2D>(true))
                {
                    physics.simulated=false;
                }
                actor.transform.SetPositionAndRotation(new Vector3((index-1.5f)*1.8f,0,0),Quaternion.identity);
                Transform visual=actor.transform.Find("Visual");
                visual.localRotation=Quaternion.identity;
                var animator=new SerializedObject(actor.GetComponent<CreatureSpriteAnimator2D>());
                var body=visual.Find("Body").GetComponent<SpriteRenderer>();
                if (index<3)
                {
                    float fold=index==0 ? 0 : 1;
                    if (index==2)
                    {
                        fold=sprites.CompactWalkingFold;
                    }
                    body.sprite=sprites.Frame(sprites.FrameIndex(.25f,fold));
                }
                bool eyesShut=index>=2;
                int eye=eyesShut ? 3 : 0;
                var iris=visual.Find("Eye iris").GetComponent<SpriteRenderer>();
                iris.sprite=(Sprite)animator.FindProperty("eyeFrames").GetArrayElementAtIndex(eye).objectReferenceValue;
                visual.Find("Eye pupil").GetComponent<SpriteRenderer>().enabled=!eyesShut;
                if (index==3)
                {
                    body.sprite=(Sprite)animator.FindProperty("recoilFrames").GetArrayElementAtIndex(3).objectReferenceValue;
                    Vector2 offset=animator.FindProperty("eyeFrameOffsets").GetArrayElementAtIndex(3).vector2Value;
                    foreach (string part in new[] {"Eye iris","Eye pupil","Eye backing · covers baked pupil"})
                    {
                        Transform transform=visual.Find(part);
                        if (transform!=null)
                        {
                            transform.localPosition+=(Vector3)offset;
                        }
                    }
                }
                var block=new MaterialPropertyBlock();
                iris.GetPropertyBlock(block);
                block.SetFloat("_EyeFrame",eye);
                block.SetFloat("_BurnAmount",0);
                block.SetFloat("_WarningAmount",0);
                iris.SetPropertyBlock(block);
                foreach (SpriteRenderer shadow in actor.transform.Find("Ground shadows").GetComponentsInChildren<SpriteRenderer>())
                {
                    shadow.enabled=false;
                }
            }
            var light=New("Comparison light",scene).AddComponent<Light2D>();
            light.lightType=Light2D.LightType.Global;
            light.intensity=1;
            var camera=New("Same scale posture camera",scene).AddComponent<Camera>();
            camera.enabled=false;
            camera.scene=scene;
            camera.orthographic=true;
            camera.orthographicSize=2.25f;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=Color.green;
            camera.transform.position=Vector3.back*10;
            camera.gameObject.AddComponent<UniversalAdditionalCameraData>().SetRenderer(0);
            target=new RenderTexture(1600,900,24);
            target.Create();
            pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest {destination=target});
            RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,1600,900),0,0);
            pixels.Apply();
            File.WriteAllBytes("Concepts/Used/Stalker/Postures04/pose-comparison.png",pixels.EncodeToPNG());
            return "Same camera/scale: open Stalker, folded Stalker with OPEN eye, compact retreat, actual Sleeping Sleeper.";
        }
        finally
        {
            RenderTexture.active=null;
            if (pixels!=null)
            {
                UnityEngine.Object.DestroyImmediate(pixels);
            }
            if (target!=null)
            {
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static GameObject New(string name,Scene scene)
    {
        var result=new GameObject(name);
        SceneManager.MoveGameObjectToScene(result,scene);
        return result;
    }
}
