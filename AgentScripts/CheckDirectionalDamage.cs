using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.DamageFeedback.Config;
using EmergencyShift.Features.DamageFeedback.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CheckDirectionalDamage
{
    public static async Task<object> Check()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("The damage/UI integration check requires Play; no scene is saved.");
        }

        Scene scene = SceneManager.GetActiveScene();
        LevelContext2D context = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<LevelContext2D>(true)).Single();
        PlayerLife2D life = context.Player;
        Camera camera = context.Flashlight.WorldCamera;
        DirectionalDamageFeedbackView view = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<DirectionalDamageFeedbackView>(true)).Single();
        UnityEngine.UI.RawImage image = view.GetComponent<UnityEngine.UI.RawImage>();
        if (!image.canvas.enabled)
        {
            UnityEngine.UI.Button resume = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                .Single(button => button.name == "Resume" && button.isActiveAndEnabled);
            resume.onClick.Invoke();
            await Task.Delay(100);
        }

        GameplayHudView hud = view.GetComponentInParent<GameplayHudView>();
        var hudData = new SerializedObject(hud);
        var healthFill = (UnityEngine.UI.Image)hudData.FindProperty("healthFill").objectReferenceValue;
        var data = new SerializedObject(view);
        var style = (DirectionalDamageStyle)data.FindProperty("style").objectReferenceValue;
        string styleBefore = EditorJsonUtility.ToJson(style);
        string materialBefore = EditorJsonUtility.ToJson(style.Material);
        Material runtimeMaterial = image.material;
        int originalHealth = life.Health;
        float originalScale = Time.timeScale;
        Quaternion originalRotation = camera.transform.rotation;
        var checks = new List<string>();
        string folder = "Docs/UI/DirectionalDamage01";
        Directory.CreateDirectory(folder);
        try
        {
            Time.timeScale = 0;
            Require(image.canvas.enabled && image.canvas.gameObject.activeInHierarchy,
                "Gameplay Canvas is visible for the native screenshots", checks);
            Require(runtimeMaterial != style.Material, "HUD owns a material instance", checks);
            Require(!image.raycastTarget, "Overlay never intercepts input", checks);
            Require(image.rectTransform.rect.width > 0 && image.rectTransform.rect.height > 0,
                "Overlay has a visible full-screen rectangle", checks);
            Type gameViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            EditorWindow.GetWindow(gameViewType).Repaint();
            await Task.Delay(300);

            Vector2 right = camera.transform.right;
            Vector2 up = camera.transform.up;
            await Hit(life, image, healthFill, right, new Vector2(0, .5f), "left", folder, checks);
            await Hit(life, image, healthFill, -right, new Vector2(1, .5f), "right", null, checks);
            await Hit(life, image, healthFill, up, new Vector2(.5f, 0), "bottom", null, checks);
            await Hit(life, image, healthFill, -up, new Vector2(.5f, 1), "top", null, checks);
            Vector2 fromBottomRight = -(right * camera.aspect - up).normalized;
            await Hit(life, image, healthFill, fromBottomRight, new Vector2(1, 0), "bottom-right", folder, checks);

            camera.transform.rotation = Quaternion.AngleAxis(90, Vector3.forward) * originalRotation;
            await Hit(life, image, healthFill, right, new Vector2(.5f, 1), "rotated-camera-top", null, checks);
            camera.transform.rotation = originalRotation;

            life.RestoreHealth(life.MaximumHealth);
            bool first = life.TryTakeDamage(1, life.transform.position, right);
            int beforeRejected = Active(image).Length;
            bool rejected = !life.TryTakeDamage(1, life.transform.position, -right);
            Require(first && rejected && Active(image).Length == beforeRejected,
                "Protected hit produces no additional pulse", checks);

            life.RestoreHealth(life.MaximumHealth);
            Require(!image.enabled && Active(image).Length == 0, "Restore clears without simulating a hit", checks);
            Require(life.TakeDamage(1) && !image.enabled, "Unknown direction keeps the directional overlay empty", checks);

            life.RestoreHealth(life.MaximumHealth);
            life.TryTakeDamage(1, life.transform.position, right);
            view.gameObject.SetActive(false);
            Require(!image.enabled, "Disabling HUD clears the overlay", checks);
            view.gameObject.SetActive(true);
            Require(!image.enabled && image.material == runtimeMaterial,
                "Re-enable keeps one owned material and no stale pulse", checks);

            life.RestoreHealth(life.MaximumHealth);
            life.TryTakeDamage(1, life.transform.position, right);
            Require(Active(image).Length == 1, "Re-enable does not duplicate subscriptions", checks);
            DateTime deadline = DateTime.UtcNow.AddSeconds(3);
            while (image.enabled && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
            }

            Require(!image.enabled, "Effect fades on unscaled time while the world is paused", checks);
            life.RestoreHealth(life.MaximumHealth);
            life.TryTakeDamage(life.MaximumHealth, life.transform.position, right);
            Require(life.IsDead && image.enabled && Active(image).Length == 1 && healthFill.fillAmount == 0,
                "Fatal hit keeps its direction and the existing zero-health bar", checks);
            life.RestoreHealth(life.MaximumHealth);
            Require(!image.enabled, "Revive/restore clears fatal feedback", checks);
            Require(EditorJsonUtility.ToJson(style) == styleBefore && EditorJsonUtility.ToJson(style.Material) == materialBefore,
                "Shared style and material remain immutable", checks);
            return new { passed = checks.Count, checks, screenshotFolder = folder,
                scene = scene.path, scenesSaved = false };
        }
        finally
        {
            camera.transform.rotation = originalRotation;
            if (!view.gameObject.activeSelf)
            {
                view.gameObject.SetActive(true);
            }

            life.RestoreHealth(Mathf.Max(1, originalHealth));
            Time.timeScale = originalScale;
        }
    }

    private static async Task Hit(PlayerLife2D life, UnityEngine.UI.RawImage image, UnityEngine.UI.Image healthFill,
        Vector2 incoming, Vector2 expected, string name, string folder, List<string> checks)
    {
        life.RestoreHealth(life.MaximumHealth);
        bool accepted = life.TryTakeDamage(1, life.transform.position, incoming);
        Vector4[] active = Active(image);
        Require(accepted && image.enabled && active.Length == 1 &&
            Vector2.Distance(new Vector2(active[0].x, active[0].y), expected) < .005f, name + " edge from actual accepted damage", checks);
        Require(Mathf.Abs(healthFill.fillAmount - (float)life.Health / life.MaximumHealth) < .001f,
            name + " preserves the existing health fill", checks);
        if (folder != null)
        {
            Canvas.ForceUpdateCanvases();
            EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Repaint();
            await Task.Delay(200);
            Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                if (screenshot == null || screenshot.width < 100 || screenshot.height < 100)
                {
                    throw new InvalidOperationException("The native Game view screenshot was not available.");
                }

                File.WriteAllBytes(folder + "/" + name + ".png", screenshot.EncodeToPNG());
            }
            finally
            {
                if (screenshot != null)
                {
                    UnityEngine.Object.Destroy(screenshot);
                }
            }
        }
    }

    private static Vector4[] Active(UnityEngine.UI.RawImage image) =>
        image.material.GetVectorArray("_DamagePulses").Where(pulse => pulse.z > 0).ToArray();

    private static void Require(bool condition, string label, List<string> checks)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Directional damage check failed: " + label);
        }

        checks.Add(label);
    }
}
