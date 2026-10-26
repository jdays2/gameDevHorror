using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Offline composition only. The resulting body sprites need no runtime skeleton.</summary>
public static class BakeStalkerSprites
{
    private const string Folder = "Concepts/Used/Stalker/Locomotion03Sprites";
    private const int Phases = 48;
    private const int Closures = 9;
    private const int Size = 512;
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
            int corrections = 0;
            for (int closure = 0; closure < Closures; closure++)
            {
                float compact = closure / (float)(Closures - 1);
                var gait = new StalkerGaitModel(profile.Limbs, 1, profile.CycleDistance, profile.SupportFraction,
                    profile.LiftHeight, profile.BodyHeight, 0);
                // Three complete cycles remove the startup pose from the loop.
                for (int tick = 0; tick < Phases * 4; tick++)
                {
                    Vector2 root = Vector2.up * (tick / (float)Phases);
                    actor.transform.position = root;
                    gait.Step(root, 0, 1f / (Phases * 1.2f), true, compact);
                    if (tick < Phases * 3)
                    {
                        continue;
                    }
                    // The hips stay attached to one torso. Feet tuck toward them
                    // gradually; the same two segment lengths solve every pose.
                    var originalFeet = (Vector3[])gait.Feet.Clone();
                    var originalKnees = (Vector3[])gait.Knees.Clone();
                    for (int leg = 0; leg < 6; leg++)
                    {
                        StalkerLimb limb = profile.Limbs[leg];
                        Vector3 hip = gait.Hips[leg];
                        Vector3 foot = gait.Feet[leg];
                        Vector2 tucked = Vector2.Lerp(foot, hip, compact * .24f);
                        foot.x = tucked.x;
                        foot.y = tucked.y;
                        Vector2 sourceA = limb.Tip - limb.Hip;
                        Vector2 sourceB = limb.Knee - limb.Hip;
                        float side = Mathf.Sign(sourceA.x * sourceB.y - sourceA.y * sourceB.x);
                        Vector2 axis = (Vector2)(foot - hip);
                        Vector3 pole = new Vector3(-axis.y, axis.x) * side;
                        gait.Knees[leg] = StalkerGaitModel.SolveKnee(hip, ref foot, pole, limb.UpperLength, limb.LowerLength);
                        gait.Feet[leg] = foot;
                    }
                    updateSkin.Invoke(skin,new object[] {gait,visual,Color.white});
                    camera.transform.position = (Vector3)root + Vector3.back * 10;
                    RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                    image.Apply();
                    File.WriteAllBytes(Folder + "/Raw/body-" + closure + "-" + (tick % Phases).ToString("D2") + ".png", image.EncodeToPNG());
                    // Restore the unmodified pose before advancing the source model.
                    // A new closure run gets a separate model and contact history.
                    Array.Copy(originalFeet, gait.Feet, 6);
                    Array.Copy(originalKnees, gait.Knees, 6);
                    if (tick % 8 == 0)
                    {
                        await Task.Yield();
                    }
                }
                corrections += gait.ReachCorrections;
            }
            File.WriteAllText(Folder + "/bake.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                phases = Phases, closureRows = Closures, ppu = Ppu, canvas = Size, cycleDistance = profile.CycleDistance,
                source = "Locomotion02 connected paint; offline assembly only; fixed hips and bone lengths; 24% foot tuck",
                reachCorrections = corrections, runtimeIk = false, sceneMutations = 0
            }, Newtonsoft.Json.Formatting.Indented));
            return (Phases * Closures) + " complete transparent bodies baked; authored scenes untouched.";
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
