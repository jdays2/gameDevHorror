using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.ClawEnemy.View;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyClawSandbox
{
    const string Folder = "Docs/Validation/ClawSandbox01/";
    public static object Authoring()
    {
        var scene = SceneManager.GetActiveScene();
        var transforms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        var actors = transforms.Select(t => t.GetComponent<ClawEnemy2D>()).Where(a => a != null).ToArray();
        var old = JObject.Parse(File.ReadAllText(Folder + "before.json"));
        var changes = new List<string>();
        foreach (JObject root in old["scenes"][0]["roots"])
            Compare(root, "", transforms, changes);
        int missingScripts = transforms.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        var brokenReferences = new List<string>();
        foreach (var actor in actors)
        {
            foreach (var component in actor.GetComponentsInChildren<Component>(true))
            {
                var iterator = new SerializedObject(component).GetIterator();
                while (iterator.Next(true))
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue == null && !iterator.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId)))
                        brokenReferences.Add(PathOf(component.transform) + ":" + iterator.propertyPath);
            }
        }

        var ids = transforms.SelectMany(t => t.GetComponents<MonoBehaviour>()).Where(c => c != null && c.GetType().Name.Contains("PersistentEntityId")).Select(c => new SerializedObject(c).FindProperty("id")?.stringValue).Where(id => !string.IsNullOrEmpty(id)).ToArray();
        var report = new
        {
        scene = scene.path, scene.isDirty, playMode = Application.isPlaying, startScene = EditorSceneManager.playModeStartScene == null ? null : AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene), originalObjectChanges = changes, missingScripts, brokenReferences, duplicatePersistentIds = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToArray(), actors = actors.Select(a => new
        {
        path = PathOf(a.transform), position = V(a.transform.position), prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(a.gameObject), overrides = PrefabUtility.GetPropertyModifications(a.gameObject)?.Select(p => p.propertyPath).Distinct().ToArray(), a.Stationary, colliderRadius = a.GetComponent<CircleCollider2D>().radius, audioSources = a.GetComponentsInChildren<AudioSource>(true).Length
        }), displays = transforms.Where(t => t.parent != null && t.parent.name == "Claw remains · A B C").Select(t => new
        {
        path = PathOf(t), position = V(t.position), prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject), sprite = AssetDatabase.GetAssetPath(t.GetComponent<SpriteRenderer>().sprite), sort = t.GetComponent<SpriteRenderer>().sortingOrder
        }), cameras = transforms.Count(t => t.GetComponent<Camera>() != null), listeners = transforms.Count(t => t.GetComponent<AudioListener>() != null), contexts = transforms.Count(t => t.GetComponent<Zenject.SceneContext>() != null), allObjects = transforms.Length
        }

        ;
        File.WriteAllText(Folder + "authoring.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        if (changes.Count > 0 || missingScripts > 0 || brokenReferences.Count > 0 || actors.Length != 3)
            throw new InvalidOperationException("Authoring verification failed; see authoring.json");
        return report;
    }

    public static async Task<object> Play()
    {
        if (!Application.isPlaying)
            throw new InvalidOperationException("Enter sandbox Play first.");
        var errors = new List<string>();
        Application.LogCallback capture = (message, stack, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                errors.Add(message);
        };
        Application.logMessageReceived += capture;
        var profileCopies = new List<GoreProfile>();
        try
        {
            var scene = SceneManager.GetActiveScene();
            var actors = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ClawEnemy2D>(true)).ToArray();
            var player = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlayerLife2D>(true)).Single();
            var playerBody = player.GetComponent<Rigidbody2D>();
            player.SetHealthCapacity(100, 100);
            var waiting = actors.Single(a => a.name.Contains("waiting"));
            var patrol = actors.Single(a => a.name.Contains("patrol"));
            Vector2 patrolBefore = patrol.transform.position;
            await Task.Delay(2200);
            bool patrolMoved = Vector2.Distance(patrolBefore, patrol.transform.position) > .02f;
            bool waitingQuiet = !waiting.HasAggro;
            int strikes = 0;
            waiting.GetComponent<EnemyMeleeAttack2D>().Resolved += landed => strikes++;
            playerBody.position = (Vector2)waiting.transform.position + Vector2.up * 1.1f;
            playerBody.linearVelocity = Vector2.zero;
            playerBody.constraints = RigidbodyConstraints2D.FreezeAll;
            Physics2D.SyncTransforms();
            bool sawAttack = false;
            var shownFrames = new HashSet<string>();
            for (int i = 0; i < 28; i++)
            {
                await Task.Delay(100);
                sawAttack |= waiting.IsAttacking;
                shownFrames.Add(waiting.GetComponent<ClawSpriteAnimator2D>().Body.sprite.name);
            }

            bool aggro = waiting.HasAggro;
            int healthAfterAttack = player.Health;
            playerBody.position = new Vector2(8, -3);
            await Task.Delay(150);
            bool persistentAggro = waiting.HasAggro;
            Capture(new Vector2(10, 6.5f), 8.8f, Folder + "room01-play.png");
            var deaths = new List<object>();
            for (int index = 0; index < actors.Length; index++)
            {
                var actor = actors[index];
                var burst = actor.GetComponent<GoreBurst2D>();
                var serialized = new SerializedObject(burst);
                var profile = UnityEngine.Object.Instantiate((GoreProfile)serialized.FindProperty("profile").objectReferenceValue);
                profileCopies.Add(profile);
                var config = new SerializedObject(profile);
                var choices = config.FindProperty("variants");
                for (int i = choices.arraySize - 1; i >= 0; i--)
                    if (i != index)
                        choices.DeleteArrayElementAtIndex(i);
                config.ApplyModifiedPropertiesWithoutUndo();
                profile.Validate();
                serialized.FindProperty("profile").objectReferenceValue = profile;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                float facing = actor.GetComponent<ClawSpriteAnimator2D>().Body.transform.eulerAngles.z;
                int emissions = 0;
                burst.FragmentLaunched += motion => emissions++;
                actor.ReceiveBulletHit(100, actor.transform.position, Vector2.right);
                actor.ReceiveBulletHit(100, actor.transform.position, Vector2.right);
                await Task.Delay(450);
                var snapshot = burst.CaptureRemains();
                float corpseFacing = snapshot.parts[0].body.rotation.eulerAngles.z;
                bool orientationPreserved = Mathf.Abs(Mathf.DeltaAngle(facing, corpseFacing)) < .01f;
                deaths.Add(new
                {
                actor.name, actor.IsAlive, emissions, bodyHidden = !actor.GetComponent<ClawSpriteAnimator2D>().Body.enabled, physicsDisabled = !actor.GetComponent<Rigidbody2D>().simulated, colliderDisabled = !actor.GetComponent<CircleCollider2D>().enabled, facing, corpseFacing, orientationPreserved, variant = snapshot?.variantId, parts = snapshot?.parts?.Length, pool = snapshot?.hasPool
                }

                );
            }

            await Task.Delay(700);
            Capture(new Vector2(45, 11.1f), 5, Folder + "range-remains-play.png");
            var report = new
            {
            patrolMoved, waitingQuiet, sawAttack, aggro, persistentAggro, strikes, healthAfterAttack, frames = shownFrames.ToArray(), deaths, errors, newAudioSources = actors.Sum(a => a.GetComponentsInChildren<AudioSource>(true).Length)}

            ;
            File.WriteAllText(Folder + "play.json", JsonConvert.SerializeObject(report, Formatting.Indented));
            return report;
        }
        finally
        {
            Application.logMessageReceived -= capture;
        // Copies are owned by this probe. Remains still read them until Stop.
        }
    }

    public static async Task<object> LitReview()
    {
        if (!Application.isPlaying)
            throw new InvalidOperationException("Enter Play first.");
        var lightObject = new GameObject("Temporary review fill");
        SceneManager.MoveGameObjectToScene(lightObject, SceneManager.GetActiveScene());
        var light = lightObject.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        lightObject.transform.position = new Vector3(27, 8, 0);
        light.pointLightInnerRadius = 40;
        light.pointLightOuterRadius = 45;
        light.intensity = .75f;
        try
        {
            await Task.Delay(100);
            Capture(new Vector2(47, 12.3f), 3.5f, Folder + "range-remains-lit-review.png");
            Capture(new Vector2(10, 6.5f), 8.8f, Folder + "room01-lit-review.png");
            return new
            {
            temporaryFill = true, savedLightingChanged = false
            }

            ;
        }
        finally
        {
            UnityEngine.Object.Destroy(lightObject);
        }
    }

    static void Capture(Vector2 center, float size, string destination)
    {
        Camera camera = Camera.main;
        Vector3 previous = camera.transform.position;
        float previousSize = camera.orthographicSize;
        RenderTexture oldTarget = camera.targetTexture;
        RenderTexture oldActive = RenderTexture.active;
        var target = new RenderTexture(1280, 900, 24);
        var pixels = new Texture2D(1280, 900, TextureFormat.RGBA32, false);
        try
        {
            camera.transform.position = new Vector3(center.x, center.y, previous.z);
            camera.orthographicSize = size;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1280, 900), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(destination, pixels.EncodeToPNG());
        }
        finally
        {
            camera.transform.position = previous;
            camera.orthographicSize = previousSize;
            camera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(pixels);
        }
    }

    static void Compare(JObject old, string parent, Transform[] all, List<string> changes)
    {
        string path = parent + "/" + old["name"];
        var candidates = all.Where(t => PathOf(t) == path).ToArray();
        Transform current = candidates.FirstOrDefault(t => Vector3.Distance(t.position, Read(old["position"])) < .0001f && Vector3.Distance(t.lossyScale, Read(old["scale"])) < .0001f) ?? candidates.FirstOrDefault();
        if (current == null)
            changes.Add("Missing " + path);
        else
        {
            if (Vector3.Distance(current.position, Read(old["position"])) > .0001f || Vector3.Distance(current.lossyScale, Read(old["scale"])) > .0001f || Quaternion.Angle(current.rotation, Quaternion.Euler(Read(old["rotation"]))) > .001f)
                changes.Add("Transform " + path);
            if (current.gameObject.activeSelf != old["active"].Value<bool>())
                changes.Add("Active " + path);
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(current.gameObject) != old["prefab"].Value<string>())
                changes.Add("Prefab " + path);
            string[] components = current.GetComponents<Component>().Select(c => c == null ? "MISSING" : c.GetType().FullName).ToArray();
            if (!components.SequenceEqual(old["components"].Values<string>()))
                changes.Add("Components " + path);
        }

        foreach (JObject child in old["children"])
            Compare(child, path, all, changes);
    }

    static string PathOf(Transform node) => node.parent == null ? "/" + node.name : PathOf(node.parent) + "/" + node.name;
    static float[] V(Vector3 v) => new[]{v.x, v.y, v.z};
    static Vector3 Read(JToken v) => new Vector3(v[0].Value<float>(), v[1].Value<float>(), v[2].Value<float>());
}
