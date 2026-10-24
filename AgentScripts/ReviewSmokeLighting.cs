using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using EmergencyShift;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ReviewSmokeLighting
{
    public static async Task<object> Capture(string folder, string baselinePath = null)
    {
        Directory.CreateDirectory(folder);
        const string shaderPath = "Assets/EmergencyShift/Materials/Effects/MuzzleSmoke.shader";
        AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
        if (ShaderUtil.ShaderHasError(shader))
        {
            throw new InvalidOperationException("Smoke shader compilation failed.");
        }
        Scene previous = SceneManager.GetActiveScene();
        bool wasDirty = previous.isDirty;
        Scene scene = EditorSceneManager.NewPreviewScene();
        var target = new RenderTexture(384, 384, 24);
        var pixels = new Texture2D(384, 384, TextureFormat.RGB24, false);
        Material smokeMaterial = null;
        Material litMaterial = null;
        Shader baseline = null;
        RenderTexture oldTarget = RenderTexture.active;
        try
        {
            var root = new GameObject("Temporary smoke lighting review");
            SceneManager.MoveGameObjectToScene(root, scene);
            var camera = Child(root, "Review camera").AddComponent<Camera>();
            camera.scene = scene;
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 1.4f;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.12f, .14f, .14f, 1);
            camera.cullingMask = 1 << 31;
            camera.targetTexture = target;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Combat/MuzzleSmoke.prefab");
            Sprite sprite;
            using (var data = new SerializedObject(prefab.GetComponent<MuzzleSmoke2D>()))
            {
                var template = (SpriteRenderer)data.FindProperty("cloud").objectReferenceValue;
                sprite = template.sprite;
                smokeMaterial = new Material(template.sharedMaterial);
            }
            if (baselinePath != null)
            {
                string source = File.ReadAllText(baselinePath).Replace("Shader \"EmergencyShift/Muzzle Smoke\"", "Shader \"Hidden/EmergencyShift/Smoke lighting baseline\"");
                baseline = ShaderUtil.CreateShaderAsset(source);
                if (ShaderUtil.ShaderHasError(baseline))
                {
                    throw new InvalidOperationException("Baseline smoke shader compilation failed.");
                }
                smokeMaterial.shader = baseline;
            }
            litMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
            var floor = Renderer(root, "Lit reference floor", sprite, litMaterial, -1, new Vector3(4, 4, 1));
            floor.color = new Color(.18f, .20f, .20f, 1);
            var cloud = Renderer(root, "Cloud", sprite, smokeMaterial, 1, new Vector3(2, 2, 1));
            var global = Child(root, "Global reference light").AddComponent<Light2D>();
            global.lightType = Light2D.LightType.Global;
            global.color = Color.white;
            global.intensity = 0;
            var point = Child(root, "Local reference light").AddComponent<Light2D>();
            point.lightType = Light2D.LightType.Point;
            point.transform.position = new Vector3(-1.5f, 0, 0);
            point.color = Color.white;
            point.intensity = 1;
            point.pointLightInnerRadius = 3;
            point.pointLightOuterRadius = 4;
            point.shadowIntensity = 1;
            point.enabled = false;
            var wall = Renderer(root, "Shadow blocker", sprite, litMaterial, 0, new Vector3(.12f, 2.4f, 1));
            wall.transform.position = new Vector3(-.75f, 0, 0);
            var caster = wall.gameObject.AddComponent<ShadowCaster2D>();
            caster.castingOption = ShadowCaster2D.ShadowCastingOptions.CastShadow;
            caster.trimEdge = 0;
            caster.Update();
            wall.gameObject.SetActive(false);
            var records = new List<object>();
            var properties = new MaterialPropertyBlock();
            foreach (string mode in new[] { "forward", "side" })
            {
                properties.SetFloat("_Age", .30f);
                properties.SetFloat("_Seed", 42);
                properties.SetFloat("_SideBurst", mode == "side" ? 1 : 0);
                cloud.SetPropertyBlock(properties);
                cloud.color = new Color(.64f, .66f, .68f, mode == "side" ? .18f : .30f);
                foreach (string lighting in new[] { "dark", "dim", "lit", "local", "shadow", "no-lights" })
                {
                    global.enabled = lighting != "no-lights";
                    global.intensity = lighting == "lit" ? 1 : lighting == "dim" ? .05f : 0;
                    point.enabled = lighting == "local" || lighting == "shadow";
                    if (point.enabled)
                    {
                        // Preview scenes do not run the light's normal LateUpdate mesh/bounds refresh.
                        typeof(Light2D).GetMethod("UpdateMesh", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(point, new object[] { true });
                        typeof(Light2D).GetMethod("UpdateBoundingSphere", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(point, null);
                    }
                    wall.gameObject.SetActive(lighting == "shadow");
                    if (wall.gameObject.activeSelf)
                    {
                        caster.Update();
                    }
                    EditorApplication.QueuePlayerLoopUpdate();
                    await Task.Delay(100);
                    camera.Render();
                    cloud.enabled = false;
                    Color32[] background = Read(camera, target, pixels);
                    cloud.enabled = true;
                    Color32[] foreground = Read(camera, target, pixels);
                    File.WriteAllBytes(Path.Combine(folder, mode + "-" + lighting + ".png"), pixels.EncodeToPNG());
                    int maximum = 0;
                    long sum = 0;
                    for (int y = 120; y < 264; y++)
                    {
                        for (int x = 120; x < 264; x++)
                        {
                            int i = y * 384 + x;
                            int delta = Math.Abs(foreground[i].r - background[i].r) + Math.Abs(foreground[i].g - background[i].g) + Math.Abs(foreground[i].b - background[i].b);
                            maximum = Math.Max(maximum, delta);
                            sum += delta;
                        }
                    }
                    records.Add(new { mode, lighting, maximumRgbDifference = maximum, meanRgbDifference = sum / (144.0 * 144 * 3), referenceCenter = background[192 * 384 + 192].r });
                }
            }
            var result = new { shaderErrors = false, records, gameScene = previous.path, gameScenePreserved = SceneManager.GetActiveScene() == previous && previous.isDirty == wasDirty };
            File.WriteAllText(Path.Combine(folder, "capture.json"), JsonConvert.SerializeObject(result, Formatting.Indented));
            return result;
        }
        finally
        {
            RenderTexture.active = oldTarget;
            EditorSceneManager.ClosePreviewScene(scene);
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(pixels);
            Object.DestroyImmediate(smokeMaterial);
            Object.DestroyImmediate(litMaterial);
            Object.DestroyImmediate(baseline);
        }
    }

    private static GameObject Child(GameObject owner, string name)
    {
        var child = new GameObject(name);
        child.layer = 31;
        child.transform.SetParent(owner.transform, false);
        return child;
    }

    private static SpriteRenderer Renderer(GameObject owner, string name, Sprite sprite, Material material, int order, Vector3 scale)
    {
        var renderer = Child(owner, name).AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = material;
        renderer.sortingOrder = order;
        renderer.transform.localScale = scale;
        return renderer;
    }

    private static Color32[] Read(Camera camera, RenderTexture target, Texture2D pixels)
    {
        camera.Render();
        RenderTexture.active = target;
        pixels.ReadPixels(new Rect(0, 0, 384, 384), 0, 0);
        pixels.Apply();
        return pixels.GetPixels32();
    }
}
