using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.DamageFeedback.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CaptureDamageReview
{
    public static async Task<object> Capture()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires a temporary Play session; never saves a scene.");
        }
        Scene scene = SceneManager.GetActiveScene();
        var objects = scene.GetRootGameObjects();
        var context = objects.SelectMany(root => root.GetComponentsInChildren<LevelContext2D>(true)).Single();
        foreach (var enemy in objects.SelectMany(root => root.GetComponentsInChildren<EnemyHealth2D>(true)))
        {
            enemy.gameObject.SetActive(false);
        }
        var player = context.Player;
        player.GetComponent<PlayerMotor2D>().enabled = false;
        player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        player.SetHealthCapacity(100, 100);
        var camera = context.Flashlight.WorldCamera;
        var follow = camera.GetComponent<CameraFollow2D>();
        var feedback = objects.SelectMany(root => root.GetComponentsInChildren<DirectionalDamageFeedbackView>(true)).Single();
        var image = feedback.GetComponent<UnityEngine.UI.RawImage>();
        if (!image.canvas.isActiveAndEnabled)
        {
            throw new InvalidOperationException("HUD is hidden; do not invent a capture.");
        }
        EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Repaint();
        await Task.Delay(400);
        follow.SnapToTarget();
        Vector3 baseline = camera.transform.position;
        Quaternion rotation = camera.transform.rotation;
        float zoom = camera.orthographicSize;
        bool accepted = player.TryTakeDamage(1, player.transform.position, camera.transform.right);
        float maximumOffset = 0;
        double start = EditorApplication.timeSinceStartup;
        while (EditorApplication.timeSinceStartup - start < .11)
        {
            await Task.Delay(8);
            maximumOffset = Mathf.Max(maximumOffset, Vector3.Distance(camera.transform.position, baseline));
        }
        Save("damage-game-peak.png");
        while (EditorApplication.timeSinceStartup - start < 1.1)
        {
            await Task.Delay(16);
        }
        Save("damage-game-1s.png");
        var result = new { scene = scene.path, accepted, maximumOffset, returnError = Vector3.Distance(camera.transform.position, baseline),
            bloodVisibleAt1Second = image.enabled, rotationUnchanged = camera.transform.rotation == rotation,
            zoomUnchanged = Mathf.Abs(camera.orthographicSize - zoom) < .0001f, scenesSaved = 0 };
        File.WriteAllText("work/damage-remains-20261022/live-feedback.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        player.RestoreHealth(100);
        return result;
    }

    private static void Save(string name)
    {
        Canvas.ForceUpdateCanvases();
        var image = ScreenCapture.CaptureScreenshotAsTexture();
        try
        {
            if (image == null || image.width < 100)
            {
                throw new InvalidOperationException("Game view capture was unavailable.");
            }
            File.WriteAllBytes("work/damage-remains-20261022/" + name, image.EncodeToPNG());
        }
        finally
        {
            if (image != null)
            {
                UnityEngine.Object.Destroy(image);
            }
        }
    }
}
