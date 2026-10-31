using System;
using System.IO;
using System.Linq;
using EmergencyShift.Features.Scorpion.Config;
using EmergencyShift.Features.Scorpion.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Native palette review in an isolated preview scene; authored scenes are never saved or modified.</summary>
public static class ReviewScorpionPaletteR11
{
    private const string Root = "Assets/EmergencyShift/";
    private const string Folder = "Docs/Validation/ScorpionPaletteR11/";

    public static object Capture()
    {
        if (Application.isPlaying || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Palette preview requires stopped, compiled Editor.");
        }
        string before = SceneState();
        AssetDatabase.ImportAsset(Root + "Materials/Characters/Scorpion/ScorpionPaletteLit.shader", ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(Root + "Materials/Characters/Scorpion/BurgundyShell.mat", ImportAssetOptions.ForceSynchronousImport);
        var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Characters/Scorpion/BurgundyShell.mat");
        var visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Enemies/ScorpionVisual.prefab");
        var gameplayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Enemies/Scorpion.prefab");
        bool visualUsesPalette = visualPrefab.GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.sharedMaterial == material);
        bool gameplayUsesPalette = gameplayPrefab.transform.Find("Visual/Body").GetComponent<SpriteRenderer>().sharedMaterial == material;
        var gore = new SerializedObject(gameplayPrefab.GetComponent<EmergencyShift.GoreBurst2D>());
        bool remainsUsePalette = gore.FindProperty("litMaterial").objectReferenceValue == material;
        var errors = ShaderUtil.GetShaderMessages(material.shader).Where(message => message.severity.ToString() == "Error").Select(message => message.message).ToArray();
        if (!visualUsesPalette || !gameplayUsesPalette || !remainsUsePalette || errors.Length > 0)
        {
            throw new InvalidOperationException("Palette references or shader are invalid.");
        }
        Directory.CreateDirectory(Folder);
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var actor = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, preview);
            foreach (var animator in actor.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
            }
            var body = actor.GetComponentsInChildren<SpriteRenderer>(true).First(renderer => renderer.sharedMaterial == material);
            var eyes = actor.GetComponentsInChildren<SpriteRenderer>(true).First(renderer => renderer != body);
            var presentation = AssetDatabase.LoadAssetAtPath<ScorpionPresentationSet>(Root + "Data/Enemies/Scorpion/Presentation.asset");
            body.sprite = presentation.Idle;
            eyes.sprite = presentation.IdleEyes;
            actor.transform.position = Vector3.zero;
            var cameraObject = new GameObject("Palette review camera", typeof(Camera), typeof(UniversalAdditionalCameraData));
            SceneManager.MoveGameObjectToScene(cameraObject, preview);
            var camera = cameraObject.GetComponent<Camera>();
            camera.scene = preview;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 1.1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.11f, .13f, .145f);
            var lightObject = new GameObject("Palette review light", typeof(Light2D));
            SceneManager.MoveGameObjectToScene(lightObject, preview);
            var light = lightObject.GetComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1;
            light.color = Color.white;
            Render(camera, "native-lit.png");
            light.intensity = .09f;
            camera.backgroundColor = new Color(.012f, .016f, .02f);
            Render(camera, "native-night.png");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }
        bool scenesPreserved = before == SceneState();
        if (!scenesPreserved)
        {
            throw new InvalidOperationException("Authored scene state changed during preview.");
        }
        var report = new
        {
            task = "TASK-005",
            date = "2026-10-31",
            revision = "wine-copper-r11",
            dark = Values(material.GetColor("_Dark")),
            middle = Values(material.GetColor("_Middle")),
            light = Values(material.GetColor("_Light")),
            visualUsesPalette,
            gameplayUsesPalette,
            remainsUsePalette,
            shaderErrors = errors,
            scenesPreserved,
            sceneSaved = false,
            loadedScenes = before,
            scope = "Actual URP2D material and sprites in isolated native preview; no gameplay or artistic acceptance claimed."
        };
        File.WriteAllText(Folder + "native.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }

    private static float[] Values(Color color) => new[] { color.r, color.g, color.b, color.a };

    private static string SceneState() => string.Join("|", Enumerable.Range(0, SceneManager.sceneCount).Select(index =>
    {
        var scene = SceneManager.GetSceneAt(index);
        return scene.handle + ":" + scene.path + ":" + scene.isDirty + ":" + scene.rootCount;
    }));

    private static void Render(Camera camera, string file)
    {
        var target = new RenderTexture(900, 900, 24);
        var pixels = new Texture2D(900, 900, TextureFormat.RGBA32, false);
        var previousActive = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 900, 900), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Folder + file, pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previousActive;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
}
