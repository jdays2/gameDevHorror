using System;
using System.IO;
using EmergencyShift;
using EmergencyShift.Features.DamageFeedback.Config;
using EmergencyShift.Features.DamageFeedback.Logic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Controlled native rendering evidence, not a saved scene or a simulation of natural AI.
public static class ReviewDamageFeedbackVisuals
{
    private const string Output = "work/damage-remains-20261022/";

    public static object RemainsComparison()
    {
        if (Application.isPlaying)
        {
            throw new InvalidOperationException("Review requires EditMode.");
        }
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var light = New("Controlled review light", preview).AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1;
            for (int species = 0; species < 2; species++)
            {
                string name = species == 0 ? "ShadowStalker" : "Sleeper";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/" + name + ".prefab");
                using (var data = new SerializedObject(prefab.GetComponent<GoreBurst2D>()))
                {
                    var fragments = data.FindProperty("fragments");
                    var profile = (GoreProfile)data.FindProperty("profile").objectReferenceValue;
                    var material = (Material)data.FindProperty("litMaterial").objectReferenceValue;
                    for (int side = 0; side < 2; side++)
                    {
                        for (int index = 0; index < fragments.arraySize; index++)
                        {
                            var renderer = New(name + " fragment " + index, preview).AddComponent<SpriteRenderer>();
                            renderer.sprite = (Sprite)fragments.GetArrayElementAtIndex(index).objectReferenceValue;
                            renderer.sharedMaterial = material;
                            renderer.sortingOrder = RemainsVisualStyle.DeadOrder;
                            renderer.color = side == 0 ? RemainsVisualStyle.Tint : profile.SettledTint;
                            renderer.transform.position = new Vector3((side == 0 ? -4.1f : .45f) + index % 7 * .59f,
                                1.4f - species * 2.2f - index / 7 * .8f, 0);
                        }
                    }
                }
            }
            var camera = New("Review camera", preview).AddComponent<Camera>();
            camera.scene = preview;
            camera.enabled = false;
            camera.transform.position = new Vector3(0, -.1f, -10);
            camera.orthographic = true;
            camera.orthographicSize = 2.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.13f, .15f, .15f, 1);
            Render(camera, Output + "remains-tint-comparison.png", 1920, 1080);
            return new { species = 2, fragmentsPerSpecies = 14, left = "Original .58 tint", right = "Configured .78 tint",
                sourceSpritesChanged = 0, scenesSaved = 0 };
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    public static object BloodSamples()
    {
        var style = AssetDatabase.LoadAssetAtPath<DirectionalDamageStyle>("Assets/EmergencyShift/Data/UI/Feedback/DirectionalDamage01.asset");
        Scene preview = EditorSceneManager.NewPreviewScene();
        var material = new Material(style.Material);
        try
        {
            var camera = New("Damage review camera", preview).AddComponent<Camera>();
            camera.scene = preview;
            camera.enabled = false;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.19f, .21f, .22f, 1);
            var canvas = New("Damage review Canvas", preview).AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            var imageObject = new GameObject("Damage shader sample", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
            imageObject.transform.SetParent(canvas.transform, false);
            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            imageObject.GetComponent<UnityEngine.UI.RawImage>().material = material;
            for (int pass = 0; pass < 2; pass++)
            {
                bool original = pass == 0;
                var settings = original ? new DamageFeedbackSettings(.09f, .95f, .67f) : style.CreateSettings();
                material.SetColor("_BloodColor", original ? new Color(.58f, .018f, .035f, 1) : style.BloodColor);
                material.SetFloat("_Depth", original ? .13f : style.Depth);
                material.SetFloat("_Spread", original ? .46f : style.Spread);
                foreach (float age in new[] { .12f, 1.1f })
                {
                    var model = new DirectionalDamageModel(settings);
                    new ShowDamagePulseCommand(model, 1, Vector2.left).Execute();
                    model.Advance(age);
                    var pulses = new Vector4[DirectionalDamageModel.Capacity];
                    for (int index = 0; index < pulses.Length; index++)
                    {
                        DamagePulse pulse = model.Pulses[index];
                        pulses[index] = new Vector4(pulse.EdgePoint.x, pulse.EdgePoint.y, pulse.Opacity, pulse.Seed);
                    }
                    material.SetVectorArray("_DamagePulses", pulses);
                    Canvas.ForceUpdateCanvases();
                    Render(camera, Output + (original ? "blood-before-" : "blood-after-")
                        + (age < 1 ? "peak" : "1s") + ".png", 1280, 720);
                }
            }
            return new { nativeShader = style.Material.shader.name, sampleAges = new[] { .12f, 1.1f }, scenesSaved = 0 };
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
            UnityEngine.Object.DestroyImmediate(material);
        }
    }

    private static GameObject New(string name, Scene scene)
    {
        var owner = new GameObject(name);
        SceneManager.MoveGameObjectToScene(owner, scene);
        return owner;
    }

    private static void Render(Camera camera, string path, int width, int height)
    {
        Directory.CreateDirectory(Output);
        var target = new RenderTexture(width, height, 24);
        var previous = RenderTexture.active;
        var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
