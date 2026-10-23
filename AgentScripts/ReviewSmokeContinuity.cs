using System;
using System.IO;
using EmergencyShift;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ReviewSmokeContinuity
{
    public static object Capture(string folder)
    {
        return CaptureCore(folder, null);
    }

    public static object Baseline(string folder, string sourcePath)
    {
        return CaptureCore(folder, sourcePath);
    }

    public static object Variants(string folder)
    {
        return CaptureCore(folder, null, new[] { 0, 7, 42, 19, 63, 91 });
    }

    private static object CaptureCore(string folder, string sourcePath, int[] seeds = null)
    {
        seeds = seeds ?? new[] { 0, 7, 42 };
        Directory.CreateDirectory(folder);
        const string shaderPath = "Assets/EmergencyShift/Materials/Effects/MuzzleSmoke.shader";
        AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
        if (ShaderUtil.ShaderHasError(shader))
        {
            throw new InvalidOperationException("Smoke shader has compilation errors.");
        }
        Scene previous = SceneManager.GetActiveScene();
        Scene preview = EditorSceneManager.NewPreviewScene();
        var target = new RenderTexture(384, 384, 24);
        var pixels = new Texture2D(384, 384, TextureFormat.RGB24, false);
        Material previewMaterial = null;
        Shader baselineShader = null;
        RenderTexture oldActive = RenderTexture.active;
        try
        {
            var owner = new GameObject("Temporary smoke shader preview");
            SceneManager.MoveGameObjectToScene(owner, preview);
            var cameraObject = new GameObject("Preview camera", typeof(Camera));
            cameraObject.transform.SetParent(owner.transform, false);
            var camera = cameraObject.GetComponent<Camera>();
            camera.scene = preview;
            camera.enabled = false;
            camera.cameraType = CameraType.Preview;
            camera.orthographic = true;
            camera.orthographicSize = 1.12f;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.12f, .14f, .14f, 1);
            camera.cullingMask = 1 << 31;
            camera.targetTexture = target;
            var smokeObject = new GameObject("Isolated cloud", typeof(SpriteRenderer));
            smokeObject.transform.SetParent(owner.transform, false);
            smokeObject.layer = 31;
            smokeObject.transform.localScale = new Vector3(2, 2, 1);
            var renderer = smokeObject.GetComponent<SpriteRenderer>();
            var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Combat/MuzzleSmoke.prefab");
            using (var data = new SerializedObject(template.GetComponent<MuzzleSmoke2D>()))
            {
                var forward = (SpriteRenderer)data.FindProperty("cloud").objectReferenceValue;
                renderer.sprite = forward.sprite;
                previewMaterial = new Material(forward.sharedMaterial);
                if (sourcePath != null)
                {
                    string source = File.ReadAllText(sourcePath).Replace("Shader \"EmergencyShift/Muzzle Smoke\"", "Shader \"Hidden/EmergencyShift/Smoke continuity baseline\"");
                    baselineShader = ShaderUtil.CreateShaderAsset(source);
                    if (ShaderUtil.ShaderHasError(baselineShader))
                    {
                        throw new InvalidOperationException("Baseline smoke shader has compilation errors.");
                    }
                    previewMaterial.shader = baselineShader;
                }
                renderer.sharedMaterial = previewMaterial;
            }
            renderer.color = new Color(.64f, .66f, .68f, .30f);
            var properties = new MaterialPropertyBlock();
            foreach (int seed in seeds)
            {
                foreach (int age in new[] { 15, 40, 65 })
                {
                    properties.SetFloat("_Age", age / 100f);
                    properties.SetFloat("_Seed", seed);
                    properties.SetFloat("_SideBurst", 0);
                    renderer.SetPropertyBlock(properties);
                    Save(camera, target, pixels, Path.Combine(folder, "forward-" + seed + "-" + age + ".png"));
                }
                properties.SetFloat("_Age", .3f);
                properties.SetFloat("_Seed", seed);
                properties.SetFloat("_SideBurst", 1);
                renderer.SetPropertyBlock(properties);
                Save(camera, target, pixels, Path.Combine(folder, "side-" + seed + ".png"));
            }
            var result = new { passed = true, shaderErrors = false, forwardSamples = seeds.Length * 3, sideSamples = seeds.Length, gameScene = previous.path, gameScenePreserved = SceneManager.GetActiveScene() == previous };
            File.WriteAllText(Path.Combine(folder, "capture.json"), JsonConvert.SerializeObject(result, Formatting.Indented));
            return result;
        }
        finally
        {
            RenderTexture.active = oldActive;
            EditorSceneManager.ClosePreviewScene(preview);
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(pixels);
            if (previewMaterial != null)
            {
                Object.DestroyImmediate(previewMaterial);
            }
            if (baselineShader != null)
            {
                Object.DestroyImmediate(baselineShader);
            }
        }
    }

    private static void Save(Camera camera, RenderTexture target, Texture2D pixels, string path)
    {
        camera.Render();
        RenderTexture.active = target;
        pixels.ReadPixels(new Rect(0, 0, 384, 384), 0, 0);
        pixels.Apply();
        byte darkest = 255;
        byte lightest = 0;
        foreach (Color32 pixel in pixels.GetPixels32())
        {
            darkest = Math.Min(darkest, pixel.r);
            lightest = Math.Max(lightest, pixel.r);
        }
        if (lightest - darkest < 3)
        {
            throw new InvalidOperationException("Blank smoke capture: " + path);
        }
        File.WriteAllBytes(path, pixels.EncodeToPNG());
    }
}
