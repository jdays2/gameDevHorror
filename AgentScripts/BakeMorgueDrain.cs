using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit BUG-007 asset bake. Does not open, rebuild or save authored scenes.
public static class BakeMorgueDrain
{
    private const string PrefabPath = "Assets/EmergencyShift/Prefabs/Environment/RoomFinish01/MorgueFloorDrain.prefab";
    private const string TexturePath = "Assets/EmergencyShift/Art/Environment/RoomFinish01/MorgueFloorDrain.png";
    private const string Evidence = "work/creature-restore-20261022/";
    private const int Resolution = 256;
    private const float CanvasUnits = 0.64f;

    public static object Bake()
    {
        if (Application.isPlaying || File.Exists(TexturePath))
        {
            throw new InvalidOperationException("Run once in EditMode, before the drain texture exists.");
        }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var original = prefab.GetComponentsInChildren<SpriteRenderer>(true);
        if (original.Length != 13)
        {
            throw new InvalidOperationException("Expected the original 13-renderer drain assembly.");
        }
        foreach (var scene in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt))
        {
            foreach (var transform in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)))
            {
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform.gameObject) != PrefabPath
                    || !PrefabUtility.IsAnyPrefabInstanceRoot(transform.gameObject))
                {
                    continue;
                }
                var modifications = PrefabUtility.GetPropertyModifications(transform.gameObject);
                if (modifications.Any(modification => modification.target is SpriteRenderer))
                {
                    throw new InvalidOperationException("Review the scene-specific drain renderer overrides before baking.");
                }
            }
        }
        var source = original.Select(renderer => new
        {
            renderer.name,
            position = new[] { renderer.transform.localPosition.x, renderer.transform.localPosition.y },
            scale = new[] { renderer.transform.localScale.x, renderer.transform.localScale.y },
            color = new[] { renderer.color.r, renderer.color.g, renderer.color.b, renderer.color.a },
            renderer.sortingOrder
        }).ToArray();
        File.WriteAllText(Evidence + "drain-source.json", JsonConvert.SerializeObject(source, Formatting.Indented));
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
            foreach (var renderer in instance.GetComponentsInChildren<SpriteRenderer>())
            {
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/EmergencyShift/Materials/Shared/FlatUnlit.mat");
            }
            var cameraObject = new GameObject("Drain bake camera");
            SceneManager.MoveGameObjectToScene(cameraObject, preview);
            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = preview;
            camera.orthographic = true;
            camera.orthographicSize = CanvasUnits * 0.5f;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.enabled = false;
            byte[] png = Render(camera);
            File.WriteAllBytes(TexturePath, png);
            File.WriteAllBytes(Evidence + "drain-baked.png", png);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }
        AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = Resolution / CanvasUnits;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = Resolution;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();

        var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            while (contents.transform.childCount > 0)
            {
                UnityEngine.Object.DestroyImmediate(contents.transform.GetChild(0).gameObject);
            }
            var renderer = contents.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath);
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/EmergencyShift/Materials/Shared/FlatLit.mat");
            renderer.sortingOrder = -5;
            PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
        var result = new
        {
            prefab = PrefabPath,
            guid = AssetDatabase.AssetPathToGUID(PrefabPath),
            texture = TexturePath,
            widthUnits = CanvasUnits,
            opaqueWidthUnits = 0.58f,
            resolution = Resolution,
            beforeRenderers = original.Length,
            afterRenderers = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponentsInChildren<SpriteRenderer>(true).Length,
            scenesSaved = 0
        };
        File.WriteAllText(Evidence + "drain-result.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    private static byte[] Render(Camera camera)
    {
        var target = new RenderTexture(Resolution, Resolution, 24, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        var pixels = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0);
            pixels.Apply();
            return pixels.EncodeToPNG();
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
}
