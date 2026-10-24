using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class ReviewSmokeLightingScene
{
    public static async Task<object> Capture(string folder)
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Use a temporary Play session for the authored-scene review.");
        }
        Directory.CreateDirectory(folder);
        var context = Object.FindAnyObjectByType<LevelContext2D>();
        var player = context.Player;
        var motor = player.GetComponent<PlayerMotor2D>();
        bool motorEnabled = motor.enabled;
        var flashlight = context.Flashlight;
        bool owned = flashlight.IsOwned;
        bool powered = flashlight.IsOn;
        var lights = Object.FindObjectsByType<Light2D>().Where(light => light.gameObject.scene == context.gameObject.scene).ToArray();
        float[] intensities = lights.Select(light => light.intensity).ToArray();
        var camera = Camera.main;
        var target = new RenderTexture(960, 540, 24);
        var pixels = new Texture2D(960, 540, TextureFormat.RGB24, false);
        var previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        GameObject smoke = null;
        try
        {
            motor.enabled = false;
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            player.GetComponent<PlayerAim2D>().SetScriptedTarget(player.transform.position + Vector3.right * 3);
            if (!owned)
            {
                flashlight.Acquire();
            }
            flashlight.SetPower(true);
            flashlight.AimAt(player.transform.position + Vector3.right * 3);
            await Task.Delay(200);
            var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/PistolDefinition.asset");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Combat/MuzzleSmoke.prefab");
            smoke = context.CreateInstance(prefab, player.transform.position + Vector3.right * .4f, Quaternion.Euler(0, 0, -90));
            smoke.transform.localScale = Vector3.one * definition.Visuals.MuzzleSize;
            var view = smoke.GetComponent<MuzzleSmoke2D>();
            view.Begin(definition.Visuals);
            view.Tick(.12f);
            var renderers = smoke.GetComponentsInChildren<SpriteRenderer>().Where(renderer => renderer.enabled).ToArray();
            camera.targetTexture = target;
            var records = new List<object>();
            foreach (string mode in new[] { "flashlight", "ambient", "blackout" })
            {
                if (mode == "ambient")
                {
                    flashlight.SetPower(false);
                    await Task.Delay(100);
                }
                if (mode == "blackout")
                {
                    foreach (var light in lights)
                    {
                        light.intensity = 0;
                    }
                }
                foreach (var renderer in renderers)
                {
                    renderer.enabled = false;
                }
                Color32[] background = Read(camera, target, pixels);
                foreach (var renderer in renderers)
                {
                    renderer.enabled = true;
                }
                Color32[] foreground = Read(camera, target, pixels);
                File.WriteAllBytes(Path.Combine(folder, mode + ".png"), pixels.EncodeToPNG());
                int maximum = 0;
                int changed = 0;
                for (int i = 0; i < foreground.Length; i++)
                {
                    int delta = Math.Abs(foreground[i].r - background[i].r) + Math.Abs(foreground[i].g - background[i].g) + Math.Abs(foreground[i].b - background[i].b);
                    maximum = Math.Max(maximum, delta);
                    if (delta > 0)
                    {
                        changed++;
                    }
                }
                records.Add(new { mode, flashlightOn = flashlight.IsOn, maximumRgbDifference = maximum, changedPixels = changed });
            }
            var result = new { scene = context.gameObject.scene.path, cameraSize = camera.orthographicSize, smokeAgeSeconds = .12f, records };
            File.WriteAllText(Path.Combine(folder, "capture.json"), JsonConvert.SerializeObject(result, Formatting.Indented));
            return result;
        }
        finally
        {
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].intensity = intensities[i];
            }
            flashlight.RestoreOwnership(owned);
            flashlight.SetPower(powered);
            motor.enabled = motorEnabled;
            Object.Destroy(smoke);
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            target.Release();
            Object.Destroy(target);
            Object.Destroy(pixels);
        }
    }

    private static Color32[] Read(Camera camera, RenderTexture target, Texture2D pixels)
    {
        camera.Render();
        RenderTexture.active = target;
        pixels.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
        pixels.Apply();
        return pixels.GetPixels32();
    }
}
