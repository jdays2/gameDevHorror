using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Saved-scene inspection renders; the bright authoring light is temporary and explicitly labelled.
public static class ReviewEntranceBlood
{
    private const string Output = "work/entrance-traces-20261022/";

    public static async Task<object> PlayCapture()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires a disposable Play session; never saves runtime positions.");
        }
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        var context = roots.SelectMany(root => root.GetComponentsInChildren<LevelContext2D>(true)).Single();
        var enemies = roots.SelectMany(root => root.GetComponentsInChildren<EnemyHealth2D>(true)).ToArray();
        foreach (var enemy in enemies)
        {
            enemy.gameObject.SetActive(false);
        }
        var player = context.Player;
        player.GetComponent<PlayerMotor2D>().enabled = false;
        var body = player.GetComponent<Rigidbody2D>();
        body.constraints = RigidbodyConstraints2D.FreezeAll;
        context.Flashlight.Acquire();
        context.Flashlight.SetPower(true);
        var camera = context.Flashlight.WorldCamera;
        var follow = camera.GetComponent<CameraFollow2D>();
        var positions = new[] { new Vector2(12.0f, 14.3f), new Vector2(19.25f, -2.95f),
            new Vector2(25.8f, -3.0f), new Vector2(45.7f, .2f), new Vector2(33.5f, -12.25f),
            new Vector2(-6.7f, 5.8f), new Vector2(11.5f, 0), new Vector2(40.0f, .5f), new Vector2(33.45f, -7.8f) };
        var targets = new[] { new Vector2(9.65f, 14.3f), new Vector2(19.25f, -5.1f),
            new Vector2(25.8f, -5.15f), new Vector2(45.5f, -1.75f), new Vector2(35.55f, -12.25f),
            new Vector2(-6.9f, 7.6f), new Vector2(13.5f, .2f), new Vector2(38.05f, .5f), new Vector2(33.6f, -5.55f) };
        var names = new[] { "arena", "flashlight-room", "shotgun-room", "east", "green", "arena-west", "hall-west", "hall-east", "hall-return" };
        EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Repaint();
        for (int index = 0; index < positions.Length; index++)
        {
            player.transform.position = positions[index];
            body.position = positions[index];
            Physics2D.SyncTransforms();
            context.Flashlight.AimAt(targets[index]);
            follow.SnapToTarget();
            await Task.Delay(300);
            Canvas.ForceUpdateCanvases();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                if (image == null || image.width < 100)
                {
                    throw new InvalidOperationException("Game view unavailable.");
                }
                File.WriteAllBytes(Output + "game-" + names[index] + ".png", image.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.Destroy(image);
            }
        }
        var result = new { captures = names, flashlightRange = context.Flashlight.Range, flashlightAngle = context.Flashlight.BeamAngle,
            cameraSize = camera.orthographicSize, controlledReview = "Enemies disabled and player teleported only in disposable Play session",
            enemies = enemies.Length, sceneSaved = false };
        File.WriteAllText(Output + "play-review.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    public static object Render(string suffix)
    {
        if (Application.isPlaying)
        {
            throw new InvalidOperationException("Requires EditMode.");
        }
        Scene preview = EditorSceneManager.OpenPreviewScene("Assets/EmergencyShift/Scenes/MorgueRouteTest.unity");
        try
        {
            var lighting = new GameObject("Temporary authoring review light");
            SceneManager.MoveGameObjectToScene(lighting, preview);
            var light = lighting.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = .85f;
            var cameraObject = new GameObject("Temporary authoring review camera");
            SceneManager.MoveGameObjectToScene(cameraObject, preview);
            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = preview;
            camera.enabled = false;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            Capture(camera, "overview-" + suffix, new Vector2(20, 1.5f), 21, 2400, 1600);
            Capture(camera, "arena-" + suffix, new Vector2(9.65f, 14.3f), 3.5f, 1280, 800);
            Capture(camera, "blue-rooms-" + suffix, new Vector2(22.5f, -5.55f), 3.6f, 1600, 800);
            Capture(camera, "east-" + suffix, new Vector2(45.9f, -1.75f), 3.5f, 1280, 800);
            Capture(camera, "green-" + suffix, new Vector2(35.55f, -12.25f), 3.5f, 1280, 800);
            return new { renders = 5, lighting = "Temporary bright authoring light; not gameplay exposure", savedScenes = 0 };
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    private static void Capture(Camera camera, string name, Vector2 position, float size, int width, int height)
    {
        camera.transform.position = new Vector3(position.x, position.y, -10);
        camera.orthographicSize = size;
        var target = new RenderTexture(width, height, 24);
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Output + name + ".png", image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
