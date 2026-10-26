using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Editor.Authoring;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Offline composition only. The resulting body sprites need no runtime skeleton.</summary>
public static class BakeStalkerPostures
{
    private const string Folder = "Concepts/Used/Stalker/Postures04";
    private const int Phases = 48;
    private const int Closures = 9;
    private const int Size = 576;
    private const float Ppu = 224;

    public static async Task<string> Bake()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Stop Play before baking assets.");
        }
        Directory.CreateDirectory(Folder + "/Raw");
        Scene scene = EditorSceneManager.NewPreviewScene();
        RenderTexture target = null;
        Texture2D image = null;
        Mesh mesh = null;
        try
        {
            var profile = AssetDatabase.LoadAssetAtPath<StalkerRigProfile>("Assets/EmergencyShift/Data/Enemies/StalkerLimbRig.asset");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
            var actor = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
            actor.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (Behaviour behaviour in actor.GetComponentsInChildren<Behaviour>(true))
            {
                behaviour.enabled = false;
            }
            foreach (Renderer renderer in actor.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }
            Transform visual = actor.transform.Find("Visual");
            visual.localRotation = Quaternion.identity;
            var body = visual.Find("Body").GetComponent<SpriteRenderer>();
            body.enabled = true;
            body.sprite = profile.Torso;
            body.color = Color.white;
            // The skin helper belongs to the Editor assembly; no authoring
            // MonoBehaviour is attached to the production prefab or preview.
            Type skinType = typeof(StalkerRigProfile).Assembly.GetType("EmergencyShift.Features.Enemies.View.StalkerSkinMesh");
            object skin = Activator.CreateInstance(skinType, new object[] { profile });
            mesh = (Mesh)skinType.GetProperty("Mesh").GetValue(skin);
            MethodInfo updateSkin = skinType.GetMethod("Update");
            var surface = New("Offline skin composition",scene);
            surface.transform.SetParent(visual,false);
            surface.AddComponent<MeshFilter>().sharedMesh = mesh;
            var surfaceRenderer = surface.AddComponent<MeshRenderer>();
            surfaceRenderer.sharedMaterial = profile.SurfaceMaterial;
            surfaceRenderer.sortingLayerID = body.sortingLayerID;
            surfaceRenderer.sortingOrder = body.sortingOrder - 1;
            var skinProperties = new MaterialPropertyBlock();
            skinProperties.SetTexture("_MainTex",profile.Atlas);
            surfaceRenderer.SetPropertyBlock(skinProperties);
            var light = New("Bake light", scene).AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1;
            var camera = New("Transparent body capture", scene).AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = scene;
            camera.orthographic = true;
            camera.orthographicSize = Size / (Ppu * 2);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.gameObject.AddComponent<UniversalAdditionalCameraData>().SetRenderer(0);
            target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            target.Create();
            image = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            Directory.CreateDirectory(Folder+"/Masks");
            var sprites=AssetDatabase.LoadAssetAtPath<StalkerSpriteSet>("Assets/EmergencyShift/Data/Enemies/StalkerSprites.asset");
            int[] allTriangles=mesh.triangles;
            int perLeg=mesh.vertexCount/6;
            var triangles=new int[6][];
            for (int leg=0;leg<6;leg++)
            {
                int first=leg*perLeg;
                triangles[leg]=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(allTriangles,index=>index>=first&&index<first+perLeg));
            }
            surfaceRenderer.enabled=false;
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest {destination=target});
            RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,Size,Size),0,0);
            image.Apply();
            File.WriteAllBytes(Folder+"/Masks/torso.png",image.EncodeToPNG());
            surfaceRenderer.enabled=true;
            var poses=new System.Collections.Generic.List<object>();
            float maxBoneError=0;
            float lowestKnee=float.MaxValue;
            for (int row=0;row<Closures;row++)
            {
                float fold=row/(float)(Closures-1);
                for (int phase=0;phase<Phases;phase++)
                {
                    var pose=StalkerPoseComposer.Pose(profile,sprites,phase/(float)Phases,fold);
                    updateSkin.Invoke(skin,new object[] {pose,visual,Color.white});
                    for (int leg=0;leg<6;leg++)
                    {
                        maxBoneError=Mathf.Max(maxBoneError,Mathf.Abs(Vector3.Distance(pose.Hips[leg],pose.Knees[leg])-profile.Limbs[leg].UpperLength));
                        maxBoneError=Mathf.Max(maxBoneError,Mathf.Abs(Vector3.Distance(pose.Knees[leg],pose.Feet[leg])-profile.Limbs[leg].LowerLength));
                        lowestKnee=Mathf.Min(lowestKnee,pose.Knees[leg].z);
                    }
                    poses.Add(new {row,phase,hips=System.Array.ConvertAll(pose.Hips,p=>new[]{p.x,p.y,p.z}),
                        knees=System.Array.ConvertAll(pose.Knees,p=>new[]{p.x,p.y,p.z}),feet=System.Array.ConvertAll(pose.Feet,p=>new[]{p.x,p.y,p.z})});
                    body.enabled=false;
                    for (int leg=0;leg<6;leg++)
                    {
                        mesh.triangles=triangles[leg];
                        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest {destination=target});
                        RenderTexture.active=target;
                        image.ReadPixels(new Rect(0,0,Size,Size),0,0);
                        image.Apply();
                        File.WriteAllBytes(Folder+"/Masks/leg-"+row+"-"+phase.ToString("D2")+"-"+leg+".png",image.EncodeToPNG());
                    }
                    mesh.triangles=allTriangles;
                    body.enabled=true;
                    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest {destination=target});
                    RenderTexture.active=target;
                    image.ReadPixels(new Rect(0,0,Size,Size),0,0);
                    image.Apply();
                    File.WriteAllBytes(Folder+"/Raw/body-"+row+"-"+phase.ToString("D2")+".png",image.EncodeToPNG());
                    if (phase%6==0)
                    {
                        await Task.Yield();
                    }
                }
            }
            File.WriteAllText(Folder+"/poses.json",Newtonsoft.Json.JsonConvert.SerializeObject(poses));
            File.WriteAllText(Folder+"/bake.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {
                phases=Phases,closureRows=Closures,ppu=Ppu,canvas=Size,maxBoneError,lowestKnee,
                source="Same six painted chains; explicit foot lanes and folded key posture; eye closure independent",
                runtimeIk=false,sceneMutations=0
            },Newtonsoft.Json.Formatting.Indented));
            return "432 complete poses and six per-leg alpha masks each; bone error="+maxBoneError;

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
            if (mesh != null)
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }
    }

    private static GameObject New(string name, Scene scene)
    {
        var result = new GameObject(name);
        SceneManager.MoveGameObjectToScene(result, scene);
        return result;
    }
}
