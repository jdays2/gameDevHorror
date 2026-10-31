using System;
using System.IO;
using System.Linq;
using EmergencyShift;
using EmergencyShift.Features.Scorpion.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class InstallScorpionFeedbackR14
{
    private const string Root = "Assets/EmergencyShift/";
    private const string Folder = "Docs/Validation/ScorpionFeedbackR14/";
    public const float EyeStrength = .32f;

    public static object Install()
    {
        if (Application.isPlaying || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Requires stopped compiled Editor.");
        }
        string before = SceneState();
        foreach (string name in new[] { "Scorpion", "ScorpionVisual" })
        {
            string path = Root + "Prefabs/Enemies/" + name + ".prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
                var eyes = renderers.Single(renderer => renderer.name == "Eyes");
                eyes.color = new Color(EyeStrength, EyeStrength, EyeStrength, 1);
                if (name == "Scorpion")
                {
                    var body = renderers.Single(renderer => renderer.name == "Body");
                    var hit = prefab.GetComponent<EnemyHitResponse2D>();
                    if (hit == null)
                    {
                        hit = prefab.AddComponent<EnemyHitResponse2D>();
                    }
                    var gore = new SerializedObject(prefab.GetComponent<GoreBurst2D>());
                    hit.Configure(body.transform.parent, body, (Sprite)gore.FindProperty("bloodSpraySprite").objectReferenceValue,
                        AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Characters/Scorpion/ToxicBlood.mat"));
                    var feedback = new SerializedObject(hit);
                    feedback.FindProperty("bloodTint").colorValue = Color.white;
                    feedback.FindProperty("deformVisual").boolValue = false;
                    feedback.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }
        var presentation = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Root + "Data/Enemies/Scorpion/Presentation.asset");
        var serialized = new SerializedObject(presentation);
        serialized.FindProperty("hit").objectReferenceValue = serialized.FindProperty("alarm").objectReferenceValue;
        serialized.FindProperty("hitVolume").floatValue = .75f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(presentation);
        if (before != SceneState())
        {
            throw new InvalidOperationException("Authored scene state changed.");
        }
        var report = new { task = "TASK-005", eyeStrength = EyeStrength, bloodTint = "white through ToxicBlood material",
            hitClip = "approved Detection", hitVolume = .75f, scenesPreserved = true, sceneSaved = false };
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "install.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }

    public static object EyeComparison()
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Enemies/ScorpionVisual.prefab");
            float[] strengths = { .12f, .24f, .32f };
            for (int index = 0; index < strengths.Length; index++)
            {
                var actor = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                actor.transform.position = new Vector3((index - 1) * 1.4f, 0, 0);
                foreach (var animator in actor.GetComponentsInChildren<Animator>(true))
                {
                    animator.enabled = false;
                }
                var eyes = actor.GetComponentsInChildren<SpriteRenderer>(true).Single(renderer => renderer.name == "Eyes");
                eyes.color = new Color(strengths[index], strengths[index], strengths[index], 1);
            }
            var cameraObject = new GameObject("R14 preview camera", typeof(Camera), typeof(UniversalAdditionalCameraData));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>();
            camera.scene = scene;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 1.65f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            var lightObject = new GameObject("R14 preview light", typeof(Light2D));
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            var light = lightObject.GetComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1;
            camera.backgroundColor = new Color(.11f, .13f, .145f);
            Render(camera, "eyes-day.png");
            light.intensity = .025f;
            camera.backgroundColor = new Color(.012f, .016f, .02f);
            Render(camera, "eyes-night.png");
            return new { strengths, order = "left to right", shaderUnchanged = true };
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    public static object InspectRoom()
    {
        var actors = SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Scorpion2D>(true))
            .Select(actor =>
            {
                var response = new SerializedObject(actor.GetComponent<EnemyHitResponse2D>());
                var body = actor.transform.Find("Visual/Body").GetComponent<SpriteRenderer>();
                var eyes = actor.transform.Find("Visual/Eyes").GetComponent<SpriteRenderer>();
                return new
                {
                    actor.name,
                    position = actor.transform.position.ToString(),
                    bodyMaterial = body.sharedMaterial.name,
                    bloodMaterial = response.FindProperty("bloodMaterial").objectReferenceValue.name,
                    tint = response.FindProperty("bloodTint").colorValue.ToString(),
                    deformVisual = response.FindProperty("deformVisual").boolValue,
                    eyeTint = eyes.color.r,
                    bindsBody = response.FindProperty("body").objectReferenceValue == body,
                    bindsVisual = response.FindProperty("visual").objectReferenceValue == body.transform.parent
                };
            }).ToArray();
        if (actors.Length != 4 || actors.Any(actor => actor.deformVisual || !actor.bindsBody || !actor.bindsVisual
            || actor.bodyMaterial != "BlushIvory" || actor.bloodMaterial != "ToxicBlood" || Mathf.Abs(actor.eyeTint - EyeStrength) > .0001f))
        {
            throw new InvalidOperationException("Authored room feedback references are incomplete.");
        }
        var report = new { actors, scenes = SceneState(), sceneSaved = false, authoringReview = "Not applicable: no scene objects added/moved." };
        File.WriteAllText(Folder + "room.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }

    private static void Render(Camera camera, string name)
    {
        Directory.CreateDirectory(Folder);
        var target = new RenderTexture(1500, 1000, 24);
        var pixels = new Texture2D(1500, 1000, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1500, 1000), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Folder + name, pixels.EncodeToPNG());
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
