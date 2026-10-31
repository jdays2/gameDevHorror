using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmergencyShift;
using EmergencyShift.Features.Scorpion.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Palette-only variation and native comparison; source textures and authored scenes stay intact.</summary>
public static class ScorpionPalettesR12
{
    private const string Root = "Assets/EmergencyShift/";
    private const string MaterialFolder = Root + "Materials/Characters/Scorpion/";
    private const string Folder = "Docs/Validation/ScorpionPalettesR12/";

    public static object InstallAndCapture()
    {
        if (Application.isPlaying || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Requires stopped compiled Editor.");
        }
        string before = SceneState();
        var importQuality = new List<object>();
        foreach (string name in new[] { "Run", "Attack" })
        {
            string path = Root + "Art/Enemies/Scorpion/" + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            string formatBefore = texture.format.ToString();
            long[] idsBefore = SpriteIds(path);
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.compressionQuality = 100;
            importer.SaveAndReimport();
            if (!idsBefore.SequenceEqual(SpriteIds(path)))
            {
                throw new InvalidOperationException("Sprite identities changed during quality adjustment.");
            }
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            importQuality.Add(new { path, formatBefore, formatAfter = texture.format.ToString(), texture.width, texture.height, spriteIdsPreserved = true });
        }
        var spec = JObject.Parse(File.ReadAllText("Concepts/Unused/Visual/Scorpion01/palette-variants-r12.json"));
        var template = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "BurgundyShell.mat");
        var materials = new Dictionary<string, Material>();
        foreach (JObject palette in spec["palettes"])
        {
            string key = (string)palette["key"];
            string name = (string)palette["material"];
            string path = MaterialFolder + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (key != "wine")
            {
                if (material == null)
                {
                    material = UnityEngine.Object.Instantiate(template);
                    material.name = name;
                    AssetDatabase.CreateAsset(material, path);
                }
                material.SetColor("_Dark", ReadColor(palette["dark"]));
                material.SetColor("_Middle", ReadColor(palette["middle"]));
                material.SetColor("_Light", ReadColor(palette["light"]));
                material.SetFloat("_UseMaxChannel", 0);
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssetIfDirty(material);
            }
            if (material == null || ShaderUtil.GetShaderMessages(material.shader).Any(message => message.severity.ToString() == "Error"))
            {
                throw new InvalidOperationException("Missing palette or shader error: " + key);
            }
            materials.Add(key, material);
        }
        Material selected = materials[(string)spec["default"]];
        string gameplayPath = Root + "Prefabs/Enemies/Scorpion.prefab";
        var gameplay = PrefabUtility.LoadPrefabContents(gameplayPath);
        try
        {
            gameplay.transform.Find("Visual/Body").GetComponent<SpriteRenderer>().sharedMaterial = selected;
            var gore = new SerializedObject(gameplay.GetComponent<GoreBurst2D>());
            gore.FindProperty("litMaterial").objectReferenceValue = selected;
            gore.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(gameplay, gameplayPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(gameplay);
        }
        string visualPath = Root + "Prefabs/Enemies/ScorpionVisual.prefab";
        var visual = PrefabUtility.LoadPrefabContents(visualPath);
        try
        {
            visual.GetComponentsInChildren<SpriteRenderer>(true).First(renderer => renderer.sharedMaterial.shader == template.shader).sharedMaterial = selected;
            PrefabUtility.SaveAsPrefabAsset(visual, visualPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(visual);
        }
        Directory.CreateDirectory(Folder);
        Capture(spec, materials, visualPath);
        bool scenesPreserved = before == SceneState();
        if (!scenesPreserved)
        {
            throw new InvalidOperationException("Authored scene state changed during palette review.");
        }
        var savedGameplay = AssetDatabase.LoadAssetAtPath<GameObject>(gameplayPath);
        var savedGore = new SerializedObject(savedGameplay.GetComponent<GoreBurst2D>());
        var report = new
        {
            task = "TASK-005",
            date = "2026-10-31",
            revision = "pale-variants-r12",
            defaultKey = (string)spec["default"],
            defaultMaterial = AssetDatabase.GetAssetPath(selected),
            bodyAndRemainsMatch = savedGore.FindProperty("litMaterial").objectReferenceValue == selected
                && savedGameplay.transform.Find("Visual/Body").GetComponent<SpriteRenderer>().sharedMaterial == selected,
            materials = materials.Select(pair => new { key = pair.Key, path = AssetDatabase.GetAssetPath(pair.Value),
                guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(pair.Value)) }).ToArray(),
            comparisonOrder = spec["comparison"].Values<string>().ToArray(),
            importQuality,
            scenesPreserved,
            sceneSaved = false,
            scope = "Four native URP2D palette variants of unchanged sprites; no gameplay, sound, eye or blood changes."
        };
        File.WriteAllText(Folder + "native.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }

    private static Color ReadColor(JToken values)
    {
        if (values == null || values.Count() != 3 || values.Values<float>().Any(value => !float.IsFinite(value) || value < 0 || value > 1))
        {
            throw new InvalidOperationException("Invalid palette pigment.");
        }
        return new Color((float)values[0], (float)values[1], (float)values[2], 1);
    }

    private static long[] SpriteIds(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Select(sprite =>
    {
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long id);
        return id;
    }).OrderBy(id => id).ToArray();

    private static void Capture(JObject spec, Dictionary<string, Material> materials, string visualPath)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(visualPath);
            var presentation = AssetDatabase.LoadAssetAtPath<ScorpionPresentationSet>(Root + "Data/Enemies/Scorpion/Presentation.asset");
            int index = 0;
            foreach (string key in spec["comparison"].Values<string>())
            {
                var actor = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                actor.transform.position = new Vector3(-2.1f + index * 1.4f, 0, 0);
                foreach (var animator in actor.GetComponentsInChildren<Animator>(true))
                {
                    animator.enabled = false;
                }
                foreach (var renderer in actor.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (renderer.sharedMaterial.shader.name == "EmergencyShift/Scorpion Palette Lit")
                    {
                        renderer.sharedMaterial = materials[key];
                        renderer.sprite = presentation.Idle;
                    }
                    else
                    {
                        renderer.sprite = presentation.IdleEyes;
                    }
                }
                index++;
            }
            var cameraObject = new GameObject("R12 palette camera", typeof(Camera), typeof(UniversalAdditionalCameraData));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>();
            camera.scene = scene;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 1.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.11f, .13f, .145f);
            var lightObject = new GameObject("R12 review light", typeof(Light2D));
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            var light = lightObject.GetComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1;
            Render(camera, "native-comparison.png");
            light.intensity = .09f;
            camera.backgroundColor = new Color(.012f, .016f, .02f);
            Render(camera, "native-night.png");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static void Render(Camera camera, string file)
    {
        var target = new RenderTexture(1800, 1000, 24);
        var pixels = new Texture2D(1800, 1000, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1800, 1000), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Folder + file, pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(pixels);
        }
    }

    private static string SceneState() => string.Join("|", Enumerable.Range(0, SceneManager.sceneCount).Select(index =>
    {
        var scene = SceneManager.GetSceneAt(index);
        return scene.handle + ":" + scene.path + ":" + scene.isDirty + ":" + scene.rootCount;
    }));
}
