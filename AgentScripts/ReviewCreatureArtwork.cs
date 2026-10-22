using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class ReviewCreatureArtwork
{
    private const string Evidence = "work/creature-restore-20261022/";
    private const string DrainPath = "Assets/EmergencyShift/Prefabs/Environment/RoomFinish01/MorgueFloorDrain.prefab";

    public static object VerifyAndRender()
    {
        if (Application.isPlaying)
        {
            throw new InvalidOperationException("Asset review requires EditMode.");
        }
        Directory.CreateDirectory(Evidence);
        var rows = JArray.Parse(File.ReadAllText("Concepts/Used/Visual/CreatureRestoration01/native-sprites.json"));
        foreach (var row in rows)
        {
            string path = (string)row["path"];
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Require(sprite != null && sprite.texture != null, path + ": missing sprite/texture");
            Require(AssetDatabase.AssetPathToGUID(path) == (string)row["guid"], path + ": GUID");
            Require(Mathf.Abs(sprite.pixelsPerUnit - (float)row["ppu"]) < 0.01f, path + ": PPU");
            var expectedRect = new Rect((float)row["rect"][0], (float)row["rect"][1],
                (float)row["rect"][2], (float)row["rect"][3]);
            Require(sprite.rect == expectedRect, path + ": original crop");
            Require(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long localId)
                && guid == (string)row["guid"] && localId == 21300000, path + ": persistent Sprite identity");
            var pivot = new Vector2((float)row["pivot"][0], (float)row["pivot"][1]);
            Require(Vector2.Distance(sprite.pivot / sprite.rect.size, pivot) < 0.0001f, path + ": pivot");
            Require(sprite.rect.xMin >= 0 && sprite.rect.yMin >= 0
                && sprite.rect.xMax <= sprite.texture.width && sprite.rect.yMax <= sprite.texture.height, path + ": crop");
        }
        var drain = AssetDatabase.LoadAssetAtPath<GameObject>(DrainPath);
        Require(drain.transform.childCount == 0 && drain.GetComponents<SpriteRenderer>().Length == 1,
            "Drain must have a single root SpriteRenderer and no visual child objects.");
        Require(drain.GetComponents<Component>().Length == 2, "Drain must have only Transform and SpriteRenderer.");
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var lightObject = new GameObject("Review light - not saved");
            SceneManager.MoveGameObjectToScene(lightObject, preview);
            var light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1;
            for (int species = 0; species < 2; species++)
            {
                string name = species == 0 ? "ShadowStalker" : "Sleeper";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/" + name + ".prefab");
                Require(prefab != null, name + ": prefab");
                for (int pose = 0; pose < 4; pose++)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
                    instance.transform.position = new Vector3(pose * 2.3f, -species * 2.5f, 0);
                    SetPose(instance, pose);
                    Require(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance) == AssetDatabase.GetAssetPath(prefab),
                        name + ": reusable connected instance");
                    foreach (var component in instance.GetComponentsInChildren<Component>(true))
                    {
                        Require(component != null, name + ": missing script");
                        using (var serialized = new SerializedObject(component))
                        {
                            var property = serialized.GetIterator();
                            while (property.Next(true))
                            {
                                if (property.propertyType == SerializedPropertyType.ObjectReference)
                                {
                                    Require(property.objectReferenceValue != null || property.objectReferenceEntityIdValue == default,
                                        name + ": unresolved " + property.propertyPath);
                                }
                            }
                        }
                    }
                }
            }
            var cameraObject = new GameObject("Review camera - not saved");
            SceneManager.MoveGameObjectToScene(cameraObject, preview);
            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = preview;
            camera.orthographic = true;
            camera.orthographicSize = 2.7f;
            camera.transform.position = new Vector3(3.45f, -1.25f, -10);
            camera.backgroundColor = new Color(0.14f, 0.17f, 0.17f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.enabled = false;
            Render(camera, Evidence + "creatures-native.png", 1600, 900);
            camera.orthographicSize = 5.4f;
            Render(camera, Evidence + "creatures-scale.png", 1600, 900);
            foreach (var root in preview.GetRootGameObjects().Where(root => root.GetComponent<CreatureSpriteAnimator2D>() != null))
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
            for (int index = 0; index < 2; index++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(drain, preview);
                instance.transform.position = new Vector3(index * 0.9f, 0, 0);
                Require(instance.GetComponentsInChildren<SpriteRenderer>().Length == 1, "Repeated drain must stay one renderer.");
            }
            camera.transform.position = new Vector3(0.45f, 0, -10);
            camera.orthographicSize = 0.65f;
            Render(camera, Evidence + "drain-reuse-native.png", 960, 540);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }
        var result = new { nativeSprites = rows.Count, cropsAndPivotsValid = true, creatureInstances = 8,
            drainInstances = 2, drainRenderersPerInstance = 1, previewScenesClosed = true, scenesSaved = 0 };
        File.WriteAllText(Evidence + "native-validation.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    private static void SetPose(GameObject instance, int pose)
    {
        var animator = instance.GetComponent<CreatureSpriteAnimator2D>();
        using (var serialized = new SerializedObject(animator))
        {
            string field = "walkFrames";
            int frame = 0;
            int eye = 0;
            if (pose == 1)
            {
                field = "recoilFrames";
                frame = 3;
                eye = 3;
            }
            else if (pose == 2)
            {
                field = "attackFrames";
                frame = 3;
            }
            else if (pose == 3)
            {
                field = "deathFrames";
            }
            var body = (SpriteRenderer)serialized.FindProperty("bodyRenderer").objectReferenceValue;
            body.sprite = (Sprite)serialized.FindProperty(field).GetArrayElementAtIndex(frame).objectReferenceValue;
            var iris = (SpriteRenderer)serialized.FindProperty("iris").objectReferenceValue;
            iris.sprite = (Sprite)serialized.FindProperty("eyeFrames").GetArrayElementAtIndex(eye).objectReferenceValue;
            var offset = (Vector3)serialized.FindProperty("eyeFrameOffsets").GetArrayElementAtIndex(eye).vector2Value;
            iris.transform.localPosition += offset;
            var underlay = (SpriteRenderer)serialized.FindProperty("eyeUnderlay").objectReferenceValue;
            underlay.transform.localPosition += offset;
            var pupil = (SpriteRenderer)serialized.FindProperty("pupil").objectReferenceValue;
            pupil.transform.localPosition += offset;
            pupil.enabled = pose != 1 && pose != 3;
            iris.enabled = pose != 3;
            underlay.enabled = pose != 3;
            foreach (var renderer in instance.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer != body && renderer != iris && renderer != pupil && renderer != underlay)
                {
                    renderer.enabled = false;
                }
            }
        }
    }

    public static async Task<object> CaptureAuthoredDrain()
    {
        if (Application.isPlaying)
        {
            throw new InvalidOperationException("Use EditMode for the temporary authoring camera.");
        }
        Scene scene = SceneManager.GetActiveScene();
        bool dirtyBefore = scene.isDirty;
        var cameraObject = new GameObject("Drain verification camera - temporary");
        var beamObject = new GameObject("Drain verification flashlight - temporary");
        try
        {
            var camera = cameraObject.AddComponent<Camera>();
            camera.CopyFrom(Camera.main);
            camera.enabled = false;
            camera.transform.position = new Vector3(-1.25f, 1.25f, -10);
            camera.orthographicSize = 1.3f;
            Render(camera, Evidence + "drain-room-lamp.png", 960, 540);
            beamObject.transform.position = new Vector3(-2.3f, 1.25f, 0);
            beamObject.transform.rotation = Quaternion.Euler(0, 0, -90);
            var beam = beamObject.AddComponent<Light2D>();
            beam.lightType = Light2D.LightType.Point;
            beam.pointLightOuterRadius = 2.5f;
            beam.pointLightInnerRadius = 0.1f;
            beam.pointLightOuterAngle = 65;
            beam.pointLightInnerAngle = 45;
            beam.intensity = 1.3f;
            beam.targetSortingLayers = SortingLayer.layers.Select(layer => layer.id).ToArray();
            beam.SendMessage("LateUpdate", SendMessageOptions.RequireReceiver);
            await Task.Delay(150);
            Render(camera, Evidence + "drain-room-beam.png", 960, 540);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(beamObject);
            Require(scene.isDirty == dirtyBefore, "Temporary review changed the scene dirty state; do not save it.");
        }
        return new { scene = scene.path, temporaryCameraAndBeamRemoved = true, sceneSaved = false };
    }

    private static void Render(Camera camera, string path, int width, int height)
    {
        var target = new RenderTexture(width, height, 24);
        var previous = RenderTexture.active;
        var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToPNG());
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

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
