using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift.Features.Scorpion.View;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Isolate the actual emissive renderer; no body texture, occluder or player can mask the comparison.</summary>
public static class VerifyScorpionEyesR9
{
    public static async Task<object> Play()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires short native Play.");
        }
        var nodes = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        Scorpion2D actor = nodes.Select(node => node.GetComponent<Scorpion2D>()).First(value => value != null);
        var animator = actor.GetComponent<ScorpionSpriteAnimator2D>();
        var eyes = actor.transform.Find("Visual/Eyes").GetComponent<SpriteRenderer>();
        var renderers = nodes.Select(node => node.GetComponent<Renderer>()).Where(value => value != null).ToArray();
        bool[] enabled = renderers.Select(renderer => renderer.enabled).ToArray();
        float scale = Time.timeScale;
        var block = new MaterialPropertyBlock();
        eyes.GetPropertyBlock(block);
        Color color = eyes.color;
        bool animated = animator.enabled;
        int oldPixels;
        int dimPixels;
        float oldEnergy;
        float dimEnergy;
        try
        {
            Time.timeScale = 0;
            animator.enabled = false;
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = renderer == eyes;
            }
            eyes.enabled = true;
            eyes.color = Color.white;
            var before = new MaterialPropertyBlock();
            before.SetColor("_Color", new Color(1.8f, .025f, .012f, 1));
            eyes.SetPropertyBlock(before);
            await Task.Delay(30);
            oldEnergy = Capture(nodes, actor, "emission-before.png", out oldPixels);
            eyes.SetPropertyBlock(block);
            eyes.color = color;
            await Task.Delay(30);
            dimEnergy = Capture(nodes, actor, "emission-dim.png", out dimPixels);
        }
        finally
        {
            eyes.color = color;
            eyes.SetPropertyBlock(block);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = enabled[i];
            }
            animator.enabled = animated;
            Time.timeScale = scale;
        }
        var report = new {beforeRedEnergy=oldEnergy, dimRedEnergy=dimEnergy, ratio=dimEnergy/Math.Max(.001f,oldEnergy), oldPixels, dimPixels,
            rendererTint=color.ToString(),
            scope="Actual native emissive renderer isolated from base-body texture and player. Old white SpriteRenderer versus current serialized tint .6. Shared assets never mutated; pause/restoration in finally."};
        File.WriteAllText("Docs/Validation/ScorpionR9/eyes.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }
    private static float Capture(Transform[] nodes, Scorpion2D actor, string file, out int redPixels)
    {
        var camera = nodes.Select(node => node.GetComponent<Camera>()).First(value => value != null);
        var position = camera.transform.position;
        float size = camera.orthographicSize;
        var oldTarget = camera.targetTexture;
        var active = RenderTexture.active;
        var target = new RenderTexture(600, 600, 24);
        var pixels = new Texture2D(600, 600, TextureFormat.RGBA32, false);
        try
        {
            camera.transform.position = new Vector3(actor.transform.position.x, actor.transform.position.y, position.z);
            camera.orthographicSize = 1.4f;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 600, 600), 0, 0);
            pixels.Apply();
            File.WriteAllBytes("Docs/Validation/ScorpionR9/" + file, pixels.EncodeToPNG());
            float energy = 0;
            redPixels = 0;
            foreach (Color32 pixel in pixels.GetPixels32())
            {
                if (pixel.r > pixel.g * 1.5f && pixel.r > pixel.b * 1.5f && pixel.r > 8)
                {
                    energy += pixel.r;
                    redPixels++;
                }
            }
            return energy;
        }
        finally
        {
            camera.transform.position = position;
            camera.orthographicSize = size;
            camera.targetTexture = oldTarget;
            RenderTexture.active = active;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
}
