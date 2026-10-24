using System.Linq;
using EmergencyShift;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InspectStalkerBurn
{
    public static string ModelSample()
    {
        var settings = new StalkerSettings(1.5f, 3.8f, 1.05f, .2f, 1.05f, 3, 3.2f, .45f, .8f, .09f, 1.6f, 1.9f, .3f, 10, .35f);
        var results = new System.Collections.Generic.List<object>();
        foreach (StalkerState state in new[] { StalkerState.Recoiling, StalkerState.Retreating, StalkerState.Waiting })
        {
            var model = new StalkerModel(settings, new Clock(), Vector2.zero);
            new RestoreStalkerCommand(model, new StalkerStateSnapshot(state, false, .3f, 0, 0, 0, 1, Vector2.right, Vector2.up, 0, Vector2.zero)).Execute();
            int cues = 0;
            model.EyeBurnStarted += () => cues++;
            model.Step(Frame(false));
            results.Add(new { source = "restored " + state, light = false, damageBefore = .3f, damageAfter = model.EyeDamageProgress, cues });
        }

        foreach (bool scripted in new[] { false, true })
        {
            var model = new StalkerModel(settings, new Clock(), Vector2.zero);
            if (scripted)
            {
                new SetStalkerScriptedControlCommand(model, true).Execute();
            }

            int cues = 0;
            model.EyeBurnStarted += () => cues++;
            model.Step(Frame(true));
            results.Add(new { source = scripted ? "scripted Step" : "ordinary Step", light = true, damageBefore = 0, damageAfter = model.EyeDamageProgress, cues });
        }

        string json = Newtonsoft.Json.JsonConvert.SerializeObject(results, Newtonsoft.Json.Formatting.Indented);
        return json;
    }

    private static StalkerObservation Frame(bool exposed) => new StalkerObservation(.2f, Vector2.zero, Vector2.zero,
        Vector2.right, Vector2.right, Vector2.left, true, true, true, exposed, false, false, 3.2f, .3f, 256);

    private sealed class Clock : IGameClock
    {
        public double Time => 0;
    }

    public static string Read()
    {
        var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(index => SceneManager.GetSceneAt(index)).ToArray();
        return Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            playing = EditorApplication.isPlaying,
            frame = Time.frameCount,
            scenes = scenes.Select(scene => new { scene.path, scene.isDirty }).ToArray(),
            actors = scenes.SelectMany(scene => scene.GetRootGameObjects()).SelectMany(root => root.GetComponentsInChildren<ShadowStalker2D>(true)).Select(actor => new
            {
                actor.name,
                active = actor.isActiveAndEnabled,
                position = new[] { actor.transform.position.x, actor.transform.position.y },
                state = actor.State.ToString(),
                actor.Exposed,
                actor.Closure,
                actor.EyeDamageProgress,
                actor.HasAggro,
                lightOn = actor.LightSource != null && actor.LightSource.IsOn,
                renderers = actor.GetComponentsInChildren<SpriteRenderer>(true).Where(renderer => renderer.sharedMaterial != null && renderer.sharedMaterial.shader.name.Contains("Eye Burn")).Select(renderer => Eye(renderer)).ToArray(),
                sounds = actor.GetComponents<AudioSource>().Select(source => new { source.isPlaying, clip = source.clip != null ? source.clip.name : null }).ToArray()
            }).ToArray()
        }, Newtonsoft.Json.Formatting.Indented);
    }

    private static object Eye(SpriteRenderer renderer)
    {
        var block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        return new { renderer.name, renderer.enabled, sprite = renderer.sprite != null ? renderer.sprite.name : null, burn = block.GetFloat("_BurnAmount"), frame = block.GetFloat("_EyeFrame"), pupil = block.GetFloat("_PupilLayer") };
    }
}
