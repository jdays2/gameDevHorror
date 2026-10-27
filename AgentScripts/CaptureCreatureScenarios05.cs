using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.ScriptedEncounters.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Disposable Play Mode visual diagnostics. Validated actor snapshots drive the real species
/// presentation source and the real sprite animator. This does not duplicate frame selection,
/// replace gameplay tests, save scenes, or modify shared profiles.
/// </summary>
public static class CaptureCreatureScenarios05
{
    private const string Output = "Concepts/Used/Stalker/Postures05/Scenarios";
    private const float Delta = 1f / 60;
    private const int Size = 640;
    private const int ReviewLayer = 31;
    private static int profileFrameLimit;
    private static bool onlyGore;
    private static bool onlyInterruptions;
    private static bool onlyEyeTransitions;
    private static bool onlyEyeFollowup;
    private static string speciesFilter = "";
    private static readonly MethodInfo LateTick = typeof(CreatureSpriteAnimator2D).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo WalkPhase = typeof(CreatureSpriteAnimator2D).GetField("walkPhase", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BodyRenderer = typeof(CreatureSpriteAnimator2D).GetField("bodyRenderer", BindingFlags.Instance | BindingFlags.NonPublic);

    public static Task<string> LightingSmoke()
    {
        ResetMode();
        profileFrameLimit = 2;
        return CaptureCore("lighting-smoke");
    }

    public static Task<string> GoreProbe()
    {
        ResetMode();
        onlyGore = true;
        return CaptureCore("gore-current");
    }

    public static Task<string> GoreFinalProbe()
    {
        ResetMode();
        onlyGore = true;
        return CaptureCore("gore-final");
    }

    public static Task<string> StalkerReview()
    {
        ResetMode();
        speciesFilter = "stalker";
        return CaptureCore("stalker-dense-final");
    }

    public static Task<string> SleeperReview()
    {
        ResetMode();
        speciesFilter = "sleeper";
        return CaptureCore("sleeper-final");
    }

    public static Task<string> InterruptionReview()
    {
        ResetMode();
        speciesFilter = "stalker";
        onlyInterruptions = true;
        return CaptureCore("stalker-dense-edges");
    }

    public static Task<string> SleeperEdgeReview()
    {
        ResetMode();
        speciesFilter = "sleeper";
        onlyInterruptions = true;
        return CaptureCore("sleeper-edges");
    }

    public static Task<string> EyeThresholdReview()
    {
        ResetMode();
        onlyEyeTransitions = true;
        return CaptureCore("eye-threshold-final");
    }

    public static Task<string> EyeThresholdFollowup()
    {
        ResetMode();
        onlyEyeTransitions = true;
        onlyEyeFollowup = true;
        return CaptureCore("eye-threshold-followup");
    }

    private static void ResetMode()
    {
        profileFrameLimit = 0;
        onlyGore = false;
        onlyInterruptions = false;
        onlyEyeTransitions = false;
        onlyEyeFollowup = false;
        speciesFilter = "";
    }

    public static Task<string> Capture(string runLabel = "final")
    {
        ResetMode();
        return CaptureCore(runLabel);
    }

    private static async Task<string> CaptureCore(string runLabel)
    {
        if (!EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Requires Play Mode; caller owns Play/Stop. No scenes are saved.");
        }
        if (runLabel.Any(c => !char.IsLetterOrDigit(c) && c != '-' && c != '_'))
        {
            throw new ArgumentException("Run label must contain letters, digits, dash or underscore.");
        }

        string runFolder = Path.Combine(Output, runLabel);
        Directory.CreateDirectory(runFolder);
        Scene previous = SceneManager.GetActiveScene();
        float oldCaptureDelta = Time.captureDeltaTime;
        float oldScale = Time.timeScale;
        var completed = new List<object>();
        var previouslyActiveRoots = new List<GameObject>();
        var captureErrors = new List<object>();
        bool completedAll = false;
        Application.LogCallback captureLog = (message, stack, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                captureErrors.Add(new { type = type.ToString(), message, stack });
            }
        };
        Application.logMessageReceived += captureLog;
        try
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = Delta;
            // Additive Physics2D casts see other loaded scenes. Isolate this diagnostic from
            // the original Play copy so its walls cannot invalidate the cloned route Awake.
            // Scene objects are restored in finally; nothing is saved to the authored maps.
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                foreach (GameObject root in SceneManager.GetSceneAt(index).GetRootGameObjects())
                {
                    if (root.activeSelf)
                    {
                        previouslyActiveRoots.Add(root);
                        root.SetActive(false);
                    }
                }
            }
            foreach (string level in new[] { "MechanicsSandbox", "MorgueRouteTest", "BasementPrototype" })
            {
                await CaptureLevel(level, runFolder, completed);
            }
            completedAll = true;
        }
        finally
        {
            foreach (GameObject root in previouslyActiveRoots)
            {
                if (root != null)
                {
                    root.SetActive(true);
                }
            }
            Time.captureDeltaTime = oldCaptureDelta;
            Time.timeScale = oldScale;
            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
            }
            Application.logMessageReceived -= captureLog;
            Write(Path.Combine(runFolder, "run.json"), new
            {
                completedAll,
                sampleHz = 60,
                squarePixels = Size,
                background = "#00FF00",
                realAnimator = typeof(CreatureSpriteAnimator2D).FullName,
                selectionMethod = "Actual LateUpdate invoked once per diagnostic sample; no duplicate selection logic.",
                stateMethod = "Actual scene-authored actor clone, LevelContext2D factory, validated RestoreState snapshots.",
                scope = "Presentation diagnostics; authored AI behavior is independently covered by Play Mode regressions.",
                sceneSaves = 0,
                profileWrites = 0,
                captureErrors,
                completed
            });
        }
        return Path.GetFullPath(runFolder);
    }

    private static async Task CaptureLevel(string level, string runFolder, List<object> completed)
    {
        string path = "Assets/EmergencyShift/Scenes/" + level + ".unity";
        AsyncOperation load = EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
        while (!load.isDone)
        {
            await Task.Delay(1);
        }
        Scene scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
        RenderTexture target = null;
        Texture2D pixels = null;
        var previousGlobalLights = new List<Light2D>();
        try
        {
            SceneManager.SetActiveScene(scene);
            GameObject[] roots = scene.GetRootGameObjects();
            LevelContext2D context = roots.SelectMany(r => r.GetComponentsInChildren<LevelContext2D>(true)).First();
            ShadowStalker2D[] stalkers = roots.SelectMany(r => r.GetComponentsInChildren<ShadowStalker2D>(true)).ToArray();
            Sleeper2D[] sleepers = roots.SelectMany(r => r.GetComponentsInChildren<Sleeper2D>(true)).ToArray();
            List<CaptureSource> sources = SelectSources(stalkers, sleepers);
            if (sources.Select(source => source.Key).Distinct(StringComparer.Ordinal).Count() != sources.Count)
            {
                throw new InvalidOperationException("Source folder key collision in " + level);
            }
            Write(Path.Combine(runFolder, level + "-sources.json"), new
            {
                level,
                allStalkers = stalkers.Select(a => new { a.name, definition = AssetDatabase.GetAssetPath(a.Definition), speed = a.Definition.PursuitSpeed, scale = Vec(a.transform.lossyScale), authored = AuthoredOverrides(a.gameObject) }),
                allSleepers = sleepers.Select(a => new { a.name, definition = AssetDatabase.GetAssetPath(a.Definition), speed = a.Definition.PursuitSpeed, authored = AuthoredOverrides(a.gameObject) }),
                selectionMethod = "One actual Stalker per immutable presentation configuration; every actual Sleeper remains separate. Scale/tint grouping rounds to five decimal places; raw authored metadata stays exact.",
                selected = sources.Select(source => source.Actor.name),
                selectedVariants = sources.Select(source => new
                {
                    key = level + "-" + source.Key,
                    source.Species,
                    source = source.Actor.name,
                    hierarchy = Hierarchy(source.Actor.transform),
                    globalId = GlobalObjectId.GetGlobalObjectIdSlow(source.Actor).ToString(),
                    configuration = JsonConvert.DeserializeObject(source.Signature),
                    members = source.Members.Select(member => new { member.name, hierarchy = Hierarchy(member.transform) })
                })
            });
            foreach (GameObject root in roots)
            {
                root.SetActive(false);
            }
            // URP resolves global lights by sorting layer across loaded scenes. Temporarily
            // isolate this diagnostic light from the already running level's dark ambient.
            foreach (Light2D existing in UnityEngine.Object.FindObjectsByType<Light2D>())
            {
                if (existing.enabled && existing.lightType == Light2D.LightType.Global)
                {
                    previousGlobalLights.Add(existing);
                    existing.enabled = false;
                }
            }
            var cameraObject = New("Temporary isolated green review camera", scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene;
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 1.65f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.green;
            camera.cullingMask = 1 << ReviewLayer;
            camera.gameObject.AddComponent<UniversalAdditionalCameraData>().SetRenderer(0);
            var light = New("Temporary neutral global review light", scene).AddComponent<Light2D>();
            light.gameObject.layer = ReviewLayer;
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1;
            target = new RenderTexture(Size, Size, 24);
            target.Create();
            camera.targetTexture = target;
            pixels = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            for (int index = 0; index < sources.Count; index++)
            {
                CaptureSource selected = sources[index];
                GameObject source = selected.Actor;
                string species = selected.Species;
                string key = level + "-" + selected.Key;
                string folder = Path.Combine(runFolder, key);
                Directory.CreateDirectory(folder);
                var stageRoots = new HashSet<GameObject>(scene.GetRootGameObjects());
                bool authoredRoutePresent = source.GetComponent<ScriptedStalkerRoute2D>() != null;
                GameObject probe = context.CreateInstance(source, new Vector3(1000, 1000, 0), Quaternion.identity);
                probe.name = "Disposable visual review " + key;
                var fixtureRoute = probe.GetComponent<ScriptedStalkerRoute2D>();
                bool fixtureRouteRemoved = fixtureRoute != null;
                if (fixtureRoute != null)
                {
                    // Scripted routes intentionally reject checkpoint snapshots. This
                    // disposable presentation fixture excludes the route and its mechanics;
                    // the authored source and its runtime checkpoint policy remain intact.
                    UnityEngine.Object.DestroyImmediate(fixtureRoute);
                    if (probe.GetComponent<ScriptedStalkerRoute2D>() != null || source.GetComponent<ScriptedStalkerRoute2D>() == null)
                    {
                        throw new InvalidOperationException("Scripted route isolation failed for " + key);
                    }
                }
                ValidateCloneReferences(probe);
                probe.SetActive(true);
                SetLayer(probe.transform);
                await NextFrame();
                DisableBehaviour(probe);
                var animator = probe.GetComponent<CreatureSpriteAnimator2D>();
                WalkPhase.SetValue(animator, .25f);
                animator.RestoreVisualRotation(Quaternion.identity);
                var body = (SpriteRenderer)BodyRenderer.GetValue(animator);
                var presentation = probe.GetComponent<EnemyPresentationSource2D>();
                var stalker = probe.GetComponent<ShadowStalker2D>();
                var sleeper = probe.GetComponent<Sleeper2D>();
                EnemyDefinition definition = stalker != null ? stalker.Definition : sleeper.Definition;
                var health = probe.GetComponent<EnemyHealth2D>();
                int deathEvents = 0;
                int killCalls = 0;
                Action countDeath = () => deathEvents++;
                health.Died += countDeath;
                var metadata = new List<object>();
                var spans = new List<object>();
                try
                {
                    int frame = 0;
                    foreach (Clip clip in Clips(sleeper != null, definition))
                    {
                        if (onlyEyeFollowup && clip.Name != "30-eye-threshold-open-close")
                        {
                            continue;
                        }
                        if (onlyEyeTransitions && clip.Name != "01-idle" && clip.Name != "02-waking-entire-transition" &&
                            clip.Name != "07-recoil-open-eye-to-shut" && clip.Name != "10-recover-open-and-walk" &&
                            clip.Name != "12-recover-before-combat" && clip.Name != "17-restore-waking-half" &&
                            clip.Name != "21-sleep-before-near-attack" && clip.Name != "22-wake-to-near-attack-boundary" &&
                            clip.Name != "30-eye-threshold-open-close")
                        {
                            continue;
                        }
                        if (onlyInterruptions && clip.Name != "01-idle" && !clip.Name.StartsWith("27-", StringComparison.Ordinal) && !clip.Name.StartsWith("28-", StringComparison.Ordinal) && !clip.Name.StartsWith("29-", StringComparison.Ordinal))
                        {
                            continue;
                        }
                        if (onlyGore && clip.Name != "09-wait-fully-folded" && clip.Name != "01-idle" && clip.Name != "20-death-actual-kill-gore")
                        {
                            continue;
                        }
                        if (profileFrameLimit > 0 && frame >= profileFrameLimit)
                        {
                            break;
                        }
                        int start = frame;
                        for (int local = 0; local < clip.Frames; local++)
                        {
                            if (profileFrameLimit > 0 && frame >= profileFrameLimit)
                            {
                                break;
                            }
                            await NextFrame();
                            Sample sample = clip.Sample(local * Delta, local, definition.PursuitSpeed);
                            if (sample.TriggerKill)
                            {
                                probe.GetComponent<GoreBurst2D>().enabled = true;
                                health.Kill();
                                killCalls++;
                                // Let the real dead model advance its age. Restoring a Dead
                                // checkpoint calls RestoreDead and deliberately chooses a
                                // settled legacy frame, which is a different visual path.
                                if (stalker != null)
                                {
                                    stalker.enabled = true;
                                }
                                if (sleeper != null)
                                {
                                    sleeper.enabled = true;
                                }
                            }
                            if (sample.State != "Dead" || killCalls == 0)
                            {
                                ApplyState(probe, sample, definition);
                            }
                            if (sample.State == "Dead")
                            {
                                foreach (GameObject sceneRoot in scene.GetRootGameObjects())
                                {
                                    if (sceneRoot.activeSelf)
                                    {
                                        SetLayer(sceneRoot.transform);
                                    }
                                }
                            }
                            Vector3 position = probe.transform.position;
                            if (sample.Teleport)
                            {
                                position += new Vector3(20, -15, 0);
                            }
                            position += (Vector3)(sample.Velocity * Delta);
                            probe.transform.position = position;
                            var physics = probe.GetComponent<Rigidbody2D>();
                            physics.position = position;
                            physics.linearVelocity = sample.Velocity;
                            if (sample.RestoreRotation)
                            {
                                animator.RestoreVisualRotation(Quaternion.Euler(0, 0, sample.Rotation));
                            }
                            if (presentation.Target != null)
                            {
                                presentation.Target.position = position + new Vector3(sample.TargetOffset.x, sample.TargetOffset.y, 0);
                            }
                            LateTick.Invoke(animator, null);
                            camera.orthographicSize = sample.State == "Dead" ? 4.5f : 1.65f;
                            camera.transform.SetPositionAndRotation(position + new Vector3(0, 0, -10), Quaternion.identity);
                            Readback(camera, target, pixels, Path.Combine(folder, "frame-" + frame.ToString("D5") + ".png"));
                            metadata.Add(new
                            {
                                frame,
                                seconds = frame * Delta,
                                scenario = clip.Name,
                                local,
                                state = stalker != null ? stalker.State.ToString() : sleeper.State.ToString(),
                                body = body.sprite != null ? body.sprite.name : null,
                                bodyAsset = body.sprite != null ? AssetDatabase.GetAssetPath(body.sprite) : null,
                                spritePivotPixels = body.sprite != null ? new[] { body.sprite.pivot.x, body.sprite.pivot.y } : null,
                                spriteRect = body.sprite != null ? new[] { body.sprite.rect.x, body.sprite.rect.y, body.sprite.rect.width, body.sprite.rect.height } : null,
                                spritePixelsPerUnit = body.sprite != null ? body.sprite.pixelsPerUnit : 0,
                                bodyLocalPosition = Vec(body.transform.localPosition),
                                bodyLocalScale = Vec(body.transform.localScale),
                                bodyLocalRotation = body.transform.localEulerAngles.z,
                                frameIndex = animator.CurrentFrame,
                                fold = DisplayedFold(animator),
                                phase = (float)WalkPhase.GetValue(animator),
                                closure = presentation.Closure,
                                eyeOpen = animator.EyeOpen,
                                pupilOffset = new[] { animator.PupilOffset.x, animator.PupilOffset.y },
                                eye = EyeSprite(probe),
                                posture = presentation.BodyPosture.ToString(),
                                attack = presentation.AttackProgress,
                                death = presentation.DeathTime,
                                staggered = presentation.IsStaggered,
                                position = Vec(position),
                                velocity = new[] { sample.Velocity.x, sample.Velocity.y },
                                visualRotation = animator.VisualRotation.eulerAngles.z,
                                scale = Vec(probe.transform.lossyScale),
                                bounds = new[] { body.bounds.center.x - position.x, body.bounds.center.y - position.y, body.bounds.size.x, body.bounds.size.y },
                                bodyEnabled = body.enabled,
                                orthographicSize = camera.orthographicSize,
                                animatorEnabled = animator.enabled,
                                deathEvents,
                                killCalls,
                                remainsRoots = scene.GetRootGameObjects().Count(r => r.name.StartsWith("Persistent remains", StringComparison.Ordinal)),
                                sample.Teleport,
                                sample.RestoreRotation
                            });
                            frame++;
                        }
                        spans.Add(new { scenario = clip.Name, first = start, last = frame - 1, count = frame - start });
                    }
                    Write(Path.Combine(folder, "manifest.json"), new
                    {
                        source = source.name,
                        sourceHierarchy = Hierarchy(source.transform),
                        sourceGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(source).ToString(),
                        sourceVariantKey = selected.Key,
                        sourceVariantConfiguration = JsonConvert.DeserializeObject(selected.Signature),
                        sourceVariantMembers = selected.Members.Select(member => new { member.name, hierarchy = Hierarchy(member.transform) }),
                        authoredRoutePresent,
                        fixtureRouteRemoved,
                        routeScope = "Disposable presentation fixture excludes scripted route/checkpoint mechanics; authored source remains unchanged.",
                        level,
                        species,
                        definition = AssetDatabase.GetAssetPath(definition),
                        speed = definition.PursuitSpeed,
                        definition.AttackDuration,
                        definition.LungeStart,
                        definition.LungeEnd,
                        definition.StrikeAt,
                        sampleHz = 60,
                        orthographicSize = camera.orthographicSize,
                        pixelSize = Size,
                        authoredOverrides = AuthoredOverrides(source),
                        note = "No hand-selected body/eye frames. Ground shadows disabled only for silhouette review. Live snapshots hold state; actual Kill/dead model advances naturally without invoking settled RestoreDead. Actual animator selects all frames.",
                        spans,
                        frames = metadata
                    });
                    completed.Add(new { key, species, source = source.name, variant = selected.Key, frames = metadata.Count, scenarios = spans.Count });
                }
                finally
                {
                    health.Died -= countDeath;
                    UnityEngine.Object.DestroyImmediate(probe);
                    foreach (GameObject generatedRoot in scene.GetRootGameObjects())
                    {
                        if (!stageRoots.Contains(generatedRoot))
                        {
                            UnityEngine.Object.DestroyImmediate(generatedRoot);
                        }
                    }
                }
            }
        }
        finally
        {
            RenderTexture.active = null;
            if (pixels != null)
            {
                UnityEngine.Object.DestroyImmediate(pixels);
            }
            if (target != null)
            {
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
            if (scene.IsValid() && scene.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
                while (unload != null && !unload.isDone)
                {
                    await Task.Delay(1);
                }
            }
            foreach (Light2D existing in previousGlobalLights)
            {
                if (existing != null)
                {
                    existing.enabled = true;
                }
            }
        }
    }

    private static List<CaptureSource> SelectSources(ShadowStalker2D[] stalkers, Sleeper2D[] sleepers)
    {
        var result = new List<CaptureSource>();
        if (speciesFilter != "sleeper")
        {
            foreach (IGrouping<string, ShadowStalker2D> group in stalkers.GroupBy(actor => PresentationSignature(actor.gameObject))
                .OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                GameObject[] members = group.Select(actor => actor.gameObject).OrderBy(actor => Hierarchy(actor.transform), StringComparer.Ordinal).ToArray();
                GameObject source = members[0];
                var stalker = source.GetComponent<ShadowStalker2D>();
                string definition = AssetLabel(stalker.Definition, "Stalker-", "SandboxTarget-");
                string profile = AssetLabel(stalker.BehaviourProfile, "Stalker-Light-");
                string key = "stalker-" + definition + "-" + profile + "-" + MobilityLabel(source) + "-" + ShortHash(group.Key);
                result.Add(new CaptureSource(source, "stalker", key, group.Key, members));
            }
        }
        if (speciesFilter != "stalker")
        {
            foreach (Sleeper2D sleeper in sleepers.OrderBy(actor => Hierarchy(actor.transform), StringComparer.Ordinal))
            {
                string signature = PresentationSignature(sleeper.gameObject);
                string identity = GlobalObjectId.GetGlobalObjectIdSlow(sleeper.gameObject) + "|" + Hierarchy(sleeper.transform);
                string key = "sleeper-" + MobilityLabel(sleeper.gameObject) + "-" + ShortHash(signature + "|" + identity);
                result.Add(new CaptureSource(sleeper.gameObject, "sleeper", key, signature, new[] { sleeper.gameObject }));
            }
        }
        return result;
    }

    private static string PresentationSignature(GameObject source)
    {
        var stalker = source.GetComponent<ShadowStalker2D>();
        var sleeper = source.GetComponent<Sleeper2D>();
        EnemyDefinition definition = stalker != null ? stalker.Definition : sleeper.Definition;
        UnityEngine.Object profile = stalker != null ? (UnityEngine.Object)stalker.BehaviourProfile : sleeper.BehaviourProfile;
        var animator = source.GetComponent<CreatureSpriteAnimator2D>();
        var serialized = new SerializedObject(animator);
        var body = (SpriteRenderer)BodyRenderer.GetValue(animator);
        var physics = source.GetComponent<Rigidbody2D>();
        var crowd = source.GetComponent<CrowdAgent2D>();
        return JsonConvert.SerializeObject(new
        {
            definition = AssetDatabase.GetAssetPath(definition),
            definitionGuid = AssetGuid(definition),
            behaviourProfile = AssetDatabase.GetAssetPath(profile),
            behaviourProfileGuid = AssetGuid(profile),
            attacksEnabled = stalker != null ? stalker.AttacksEnabled : sleeper.AttacksEnabled,
            constraints = physics.constraints.ToString(),
            physics.simulated,
            crowdEnabled = crowd != null && crowd.enabled,
            worldScale = GroupedScale(source.transform.lossyScale),
            visualWorldScale = GroupedScale(source.transform.Find("Visual").lossyScale),
            bodyWorldScale = GroupedScale(body.transform.lossyScale),
            tint = new[] { Math.Round(body.color.r, 5), Math.Round(body.color.g, 5), Math.Round(body.color.b, 5), Math.Round(body.color.a, 5) },
            animator = animator.GetType().FullName,
            bodySetGuid = AssetGuid(serialized.FindProperty("spriteSet")?.objectReferenceValue),
            attackSetGuid = AssetGuid(serialized.FindProperty("attackSpriteSet")?.objectReferenceValue),
            locomotionProfileGuid = AssetGuid(serialized.FindProperty("locomotionProfile")?.objectReferenceValue),
            bodyMaterialGuid = AssetGuid(body.sharedMaterial)
        });
    }

    private static string MobilityLabel(GameObject source)
    {
        var physics = source.GetComponent<Rigidbody2D>();
        bool frozenX = (physics.constraints & RigidbodyConstraints2D.FreezePositionX) != 0;
        bool frozenY = (physics.constraints & RigidbodyConstraints2D.FreezePositionY) != 0;
        if (frozenX && frozenY)
        {
            return "stationary";
        }
        return "live";
    }

    private static string AssetLabel(UnityEngine.Object asset, params string[] prefixes)
    {
        string label = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(asset));
        foreach (string prefix in prefixes)
        {
            if (label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                label = label.Substring(prefix.Length);
                break;
            }
        }
        label = new string(label.ToLowerInvariant().Where(character => char.IsLetterOrDigit(character) || character == '-').ToArray());
        return label.Length > 18 ? label.Substring(0, 18) : label;
    }

    private static string AssetGuid(UnityEngine.Object asset) => asset != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)) : "";
    private static double[] GroupedScale(Vector3 scale) => new[] { Math.Round(scale.x, 5), Math.Round(scale.y, 5), Math.Round(scale.z, 5) };
    private static string Hierarchy(Transform transform) => transform.parent == null ? transform.name : Hierarchy(transform.parent) + "/" + transform.name;

    private static string ShortHash(string value)
    {
        using (SHA256 hash = SHA256.Create())
        {
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").Substring(0, 8).ToLowerInvariant();
        }
    }

    private static void ValidateCloneReferences(GameObject clone)
    {
        foreach (Transform child in clone.GetComponentsInChildren<Transform>(true))
        {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
            {
                throw new InvalidOperationException("Missing script in presentation fixture: " + Hierarchy(child));
            }
            foreach (Component component in child.GetComponents<Component>())
            {
                using (var serialized = new SerializedObject(component))
                {
                    SerializedProperty property = serialized.GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null &&
                            property.objectReferenceEntityIdValue != default && !property.propertyPath.StartsWith("m_Prefab", StringComparison.Ordinal) &&
                            !property.propertyPath.StartsWith("m_Corresponding", StringComparison.Ordinal))
                        {
                            throw new InvalidOperationException("Missing reference in presentation fixture: " + Hierarchy(child) + " | " + property.propertyPath);
                        }
                    }
                }
            }
        }
    }

    private static IEnumerable<Clip> Clips(bool sleeper, EnemyDefinition definition)
    {
        string walk = sleeper ? "Hunting" : "Sneaking";
        yield return new Clip("01-idle", 45, (t, n, speed) => new Sample(sleeper ? "Sleeping" : "Dormant"));
        if (sleeper)
        {
            yield return new Clip("02-waking-entire-transition", 60, (t, n, speed) => new Sample("Waking") { Age = Mathf.Min(.9f, t) });
        }
        yield return new Clip("03-walk-up", 90, (t, n, speed) => new Sample(walk) { Velocity = Vector2.up * speed });
        yield return new Clip("04-walk-down", 90, (t, n, speed) => new Sample(walk) { Velocity = Vector2.down * speed });
        yield return new Clip("05-turn-90-and-180", 120, (t, n, speed) => new Sample(walk) { Velocity = TurnDirection(n) * speed });
        yield return new Clip("06-start-stop-teleport", 120, (t, n, speed) => new Sample(walk) { Velocity = n < 30 || n >= 60 && n < 90 ? Vector2.right * speed : Vector2.zero, Teleport = n == 65 });
        if (!sleeper)
        {
            yield return new Clip("07-recoil-open-eye-to-shut", 12, (t, n, speed) => new Sample("Recoiling") { Age = t, Closure = Mathf.InverseLerp(.7f, 1, t / .2f) });
            yield return new Clip("08-retreat-compact", 63, (t, n, speed) => new Sample("Retreating") { Age = t, Closure = 1, Velocity = Vector2.down * 3.8f });
            yield return new Clip("09-wait-fully-folded", 90, (t, n, speed) => new Sample("Waiting") { Age = t, Closure = 1 });
            yield return new Clip("10-recover-open-and-walk", 60, (t, n, speed) => new Sample(walk) { Closure = Mathf.Max(0, 1 - t * 3), Velocity = Vector2.up * speed });
            yield return new Clip("11-restore-compact-turn", 90, (t, n, speed) => new Sample("Retreating") { Age = t, Closure = 1, Velocity = n < 45 ? Vector2.right * 3.8f : Vector2.up * 3.8f, RestoreRotation = n == 0, Rotation = 90 });
            yield return new Clip("12-recover-before-combat", 45, (t, n, speed) => new Sample(walk) { Closure = Mathf.Max(0, 1 - t * 3), Velocity = Vector2.up * speed });
        }
        yield return new Clip("13-attack-windup-strike-recovery", Mathf.CeilToInt(definition.AttackDuration * 60), (t, n, speed) =>
        {
            float progress = t / definition.AttackDuration;
            return new Sample("Attacking") { Age = t, Velocity = progress >= definition.LungeStart && progress <= definition.LungeEnd ? Vector2.up * (sleeper ? 6 : 2.7f) : Vector2.zero };
        });
        if (sleeper)
        {
            yield return new Clip("14-post-lunge-recovery", 39, (t, n, speed) => new Sample("Recovering") { Age = t });
        }
        yield return new Clip("15-attack-to-walk", 60, (t, n, speed) => new Sample(walk) { Velocity = Vector2.up * speed });
        yield return new Clip("16-stagger-hit-stop-and-resume", 60, (t, n, speed) => new Sample(walk) { Stagger = n < 6 ? .1f : 0, Velocity = n < 6 ? Vector2.left * 1.6f : Vector2.up * speed });
        if (sleeper)
        {
            yield return new Clip("17-restore-waking-half", 45, (t, n, speed) => new Sample("Waking") { Age = .45f + t * .5f, RestoreRotation = n == 0, Rotation = 180 });
            yield return new Clip("18-restore-hunting", 45, (t, n, speed) => new Sample(walk) { Velocity = Vector2.down * speed });
        }
        yield return new Clip("19-target-dead-stop", 45, (t, n, speed) => new Sample("TargetDead") { Age = t });
        if (!sleeper)
        {
            yield return new Clip("21a-waiting-before-close-attack", 45, (t, n, speed) => new Sample("Waiting") { Age = t, Closure = 1 });
            yield return new Clip("21b-light-off-close-unfold", 20, (t, n, speed) => new Sample(walk) { Closure = Mathf.Max(0, 1 - t * 3), TargetOffset = Vector2.up });
            yield return new Clip("21c-natural-close-attack", Mathf.CeilToInt(definition.AttackDuration * 60), (t, n, speed) => new Sample("Attacking") { Age = t, TargetOffset = Vector2.up });
            yield return new Clip("21d-close-attack-resume", 45, (t, n, speed) => new Sample(walk) { Velocity = Vector2.up * speed });
            yield return new Clip("22a-folded-before-checkpoint-cut", 30, (t, n, speed) => new Sample("Waiting") { Age = t, Closure = 1 });
            yield return new Clip("22b-checkpoint-restored-attack", Mathf.CeilToInt(definition.AttackDuration * 60), (t, n, speed) => new Sample("Attacking") { Age = t, RestoreRotation = n == 0, Rotation = 0 });
            yield return new Clip("22c-checkpoint-attack-resume", 45, (t, n, speed) => new Sample(walk) { Velocity = Vector2.up * speed });
        }
        else
        {
            yield return new Clip("21-sleep-before-near-attack", 30, (t, n, speed) => new Sample("Sleeping"));
            yield return new Clip("22-wake-to-near-attack-boundary", 54, (t, n, speed) => new Sample("Waking") { Age = t, TargetOffset = Vector2.up * 1.5f });
            yield return new Clip("23-hunt-near-one-frame", 1, (t, n, speed) => new Sample(walk) { TargetOffset = Vector2.up * 1.5f });
            yield return new Clip("24-near-attack-after-wake", Mathf.CeilToInt(definition.AttackDuration * 60), (t, n, speed) => new Sample("Attacking") { Age = t, TargetOffset = Vector2.up * 1.5f });
            yield return new Clip("25-near-attack-recovery", 39, (t, n, speed) => new Sample("Recovering") { Age = t });
            yield return new Clip("26-near-attack-resume", 45, (t, n, speed) => new Sample(walk) { Velocity = Vector2.up * speed });
        }
        int interruptAt = Mathf.CeilToInt(definition.AttackDuration * (definition.StrikeAt + .03f) * 60);
        yield return new Clip("27-attack-to-hit-state-cut", interruptAt + 45, (t, n, speed) =>
        {
            if (n < interruptAt)
            {
                return new Sample("Attacking") { Age = t };
            }
            int elapsed = n - interruptAt;
            string interruptedState = walk;
            if (sleeper)
            {
                interruptedState = "Recovering";
            }
            return new Sample(interruptedState)
            {
                Age = elapsed * Delta,
                Stagger = elapsed < 6 ? .1f : 0,
                Velocity = elapsed < 6 ? Vector2.left * 1.6f : Vector2.up * speed
            };
        });
        if (!sleeper)
        {
            yield return new Clip("28-attack-to-light-fear-state-cut", interruptAt + 42, (t, n, speed) =>
            {
                if (n < interruptAt)
                {
                    return new Sample("Attacking") { Age = t };
                }
                int elapsed = n - interruptAt;
                if (elapsed < 12)
                {
                    return new Sample("Recoiling")
                    {
                        Age = elapsed * Delta,
                        Closure = Mathf.InverseLerp(.7f, 1, elapsed * Delta / .2f)
                    };
                }
                return new Sample("Retreating")
                {
                    Age = (elapsed - 12) * Delta,
                    Closure = 1,
                    Velocity = Vector2.down * 3.8f
                };
            });
        }
        if (sleeper)
        {
            yield return new Clip("29-waking-to-target-death", 75, (t, n, speed) =>
            {
                if (n < 30)
                {
                    return new Sample("Waking") { Age = t, RestoreRotation = n == 0, Rotation = 0 };
                }
                return new Sample("TargetDead") { Age = (n - 30) * Delta };
            });
        }
        if (onlyEyeTransitions)
        {
            float[] closures = { .168f, .167f, 1f / 6, .16666663f, .1665f, .166f, .165f, .165f, .166f, .1665f, .16666663f, 1f / 6, .167f, .168f };
            if (onlyEyeFollowup)
            {
                closures = closures.Concat(Enumerable.Repeat(.165f, 36)).ToArray();
            }
            yield return new Clip("30-eye-threshold-open-close", closures.Length, (t, n, speed) => new Sample(sleeper ? "Waking" : "Recoiling")
            {
                Age = sleeper ? (1 - closures[n]) * .9f : t,
                Closure = closures[n],
                TargetOffset = onlyEyeFollowup ? new Vector2(n < closures.Length - 12 ? 1.5f : -1.5f, 0) : Vector2.up * 1.5f
            });
        }
        yield return new Clip("20-death-actual-kill-gore", 90, (t, n, speed) => new Sample("Dead") { Age = t, TriggerKill = n == 0 || n == 15 });
    }

    private static void ApplyState(GameObject probe, Sample sample, EnemyDefinition definition)
    {
        var stalker = probe.GetComponent<ShadowStalker2D>();
        if (stalker != null)
        {
            ShadowStalker2D.Snapshot saved = stalker.CaptureState();
            saved.state = (ShadowStalker2D.BehaviourState)Enum.Parse(typeof(ShadowStalker2D.BehaviourState), sample.State);
            saved.health = sample.State == "Dead" ? 0 : definition.MaximumHealth;
            saved.aggro = sample.State != "Dormant";
            saved.age = sample.Age;
            saved.closure = sample.Closure;
            saved.staggerLeft = sample.Stagger;
            saved.exposure = 0;
            saved.eyeBurned = false;
            saved.eyeDamage = 0;
            saved.escape = Vector2.down;
            saved.approach = Vector2.up;
            saved.push = Vector2.left;
            stalker.RestoreState(saved);
        }
        else
        {
            var sleeper = probe.GetComponent<Sleeper2D>();
            Sleeper2D.Snapshot saved = sleeper.CaptureState();
            saved.state = (Sleeper2D.BehaviourState)Enum.Parse(typeof(Sleeper2D.BehaviourState), sample.State);
            saved.health = sample.State == "Dead" ? 0 : definition.MaximumHealth;
            saved.aggro = sample.State != "Sleeping";
            saved.age = sample.Age;
            saved.staggerLeft = sample.Stagger;
            saved.heading = Vector2.up;
            saved.push = Vector2.left;
            sleeper.RestoreState(saved);
        }
        DisableBehaviour(probe);
        probe.GetComponent<Rigidbody2D>().simulated = false;
    }

    private static Vector2 TurnDirection(int frame)
    {
        if (frame < 40)
        {
            return Vector2.up;
        }
        if (frame < 80)
        {
            return Vector2.left;
        }
        return Vector2.right;
    }

    private static void DisableBehaviour(GameObject probe)
    {
        foreach (Behaviour behaviour in probe.GetComponentsInChildren<Behaviour>(true))
        {
            behaviour.enabled = false;
        }
        Transform shadows = probe.transform.Find("Ground shadows");
        if (shadows != null)
        {
            foreach (Renderer shadow in shadows.GetComponentsInChildren<Renderer>(true))
            {
                shadow.enabled = false;
            }
        }
    }

    private static string EyeSprite(GameObject probe)
    {
        Transform iris = probe.transform.Find("Visual/Eye iris");
        Sprite sprite = iris != null ? iris.GetComponent<SpriteRenderer>().sprite : null;
        return sprite != null ? sprite.name : null;
    }

    private static float? DisplayedFold(CreatureSpriteAnimator2D animator)
    {
        PropertyInfo property = animator.GetType().GetProperty("DisplayedBodyFold");
        return property != null ? (float)property.GetValue(animator) : (float?)null;
    }

    private static object AuthoredOverrides(GameObject source)
    {
        var animator = source.GetComponent<CreatureSpriteAnimator2D>();
        if (animator == null)
        {
            return new { source.name, animator = "MISSING" };
        }
        var serialized = new SerializedObject(animator);
        SerializedProperty bodySet = serialized.FindProperty("spriteSet");
        SerializedProperty attackSet = serialized.FindProperty("attackSpriteSet");
        var body = (SpriteRenderer)BodyRenderer.GetValue(animator);
        var physics = source.GetComponent<Rigidbody2D>();
        var sleeper = source.GetComponent<Sleeper2D>();
        var stalker = source.GetComponent<ShadowStalker2D>();
        var crowd = source.GetComponent<CrowdAgent2D>();
        return new
        {
            source.name,
            hierarchy = Hierarchy(source.transform),
            globalId = GlobalObjectId.GetGlobalObjectIdSlow(source).ToString(),
            authoredScriptedRoute = source.GetComponent<ScriptedStalkerRoute2D>() != null,
            animator = animator.GetType().FullName,
            bodySet = bodySet != null ? AssetDatabase.GetAssetPath(bodySet.objectReferenceValue) : null,
            attackSet = attackSet != null ? AssetDatabase.GetAssetPath(attackSet.objectReferenceValue) : null,
            attacksEnabled = sleeper != null ? sleeper.AttacksEnabled : stalker.AttacksEnabled,
            constraints = physics.constraints.ToString(),
            physics.simulated,
            crowdEnabled = crowd != null && crowd.enabled,
            tint = new[] { body.color.r, body.color.g, body.color.b, body.color.a },
            scale = Vec(source.transform.lossyScale),
            visualScale = Vec(source.transform.Find("Visual").lossyScale),
            bodyScale = Vec(body.transform.lossyScale),
            definitionGuid = AssetGuid(stalker != null ? stalker.Definition : sleeper.Definition),
            behaviourProfile = AssetDatabase.GetAssetPath(stalker != null ? (UnityEngine.Object)stalker.BehaviourProfile : sleeper.BehaviourProfile),
            behaviourProfileGuid = AssetGuid(stalker != null ? (UnityEngine.Object)stalker.BehaviourProfile : sleeper.BehaviourProfile),
            position = Vec(source.transform.position),
            prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(source),
            note = "Controlled pose diagnostics may move/attack a stationary actor; these authored flags remain unchanged in the source and saved map."
        };
    }

    private static async Task NextFrame()
    {
        int frame = Time.frameCount;
        while (Time.frameCount == frame && EditorApplication.isPlaying)
        {
            await Task.Delay(1);
        }
        if (!EditorApplication.isPlaying)
        {
            throw new OperationCanceledException("Play Mode stopped during isolated visual capture.");
        }
    }

    private static void Readback(Camera camera, RenderTexture target, Texture2D pixels, string path)
    {
        camera.Render();
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        pixels.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
        pixels.Apply(false);
        File.WriteAllBytes(path, pixels.EncodeToPNG());
        RenderTexture.active = previous;
    }

    private static void SetLayer(Transform root)
    {
        root.gameObject.layer = ReviewLayer;
        foreach (Transform child in root)
        {
            SetLayer(child);
        }
    }

    private static GameObject New(string name, Scene scene)
    {
        var result = new GameObject(name);
        SceneManager.MoveGameObjectToScene(result, scene);
        return result;
    }

    private static float[] Vec(Vector3 value) => new[] { value.x, value.y, value.z };
    private static void Write(string path, object value) => File.WriteAllText(path, JsonConvert.SerializeObject(value, Formatting.Indented));

    private sealed class Clip
    {
        public readonly string Name;
        public readonly int Frames;
        public readonly Func<float, int, float, Sample> Sample;
        public Clip(string name, int frames, Func<float, int, float, Sample> sample)
        {
            Name = name;
            Frames = frames;
            Sample = sample;
        }
    }

    private sealed class CaptureSource
    {
        public readonly GameObject Actor;
        public readonly string Species;
        public readonly string Key;
        public readonly string Signature;
        public readonly GameObject[] Members;

        public CaptureSource(GameObject actor, string species, string key, string signature, GameObject[] members)
        {
            Actor = actor;
            Species = species;
            Key = key;
            Signature = signature;
            Members = members;
        }
    }

    private sealed class Sample
    {
        public readonly string State;
        public float Age;
        public float Closure;
        public float Stagger;
        public Vector2 Velocity;
        public Vector2 TargetOffset = Vector2.up * 1.5f;
        public bool Teleport;
        public bool RestoreRotation;
        public bool TriggerKill;
        public float Rotation;
        public Sample(string state) => State = state;
    }
}
