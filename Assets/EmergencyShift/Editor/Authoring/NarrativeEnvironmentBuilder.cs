using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmergencyShift.Editor
{
    /// <summary>Initial scene authoring only. Never rebuilds an existing or user-tuned scene.</summary>
    public static class NarrativeEnvironmentBuilder
    {
        private const string Root = "Assets/EmergencyShift";
        private const string Sources = "Concepts/Used/Environment/PracticalLights01";
        public const string ScenePath = Root + "/Scenes/ArtReview/EnvironmentNarrativeStressReview.unity";

        [Serializable]
        private sealed class Plan
        {
            public Placement[] placements;
            public Barrier[] walls;
            public Marking[] markings;
        }

        [Serializable]
        private sealed class Placement
        {
            public string name;
            public string prefab;
            public string group;
            public string parent;
            public float x;
            public float y;
            public float angle;
            public float scale;
            public int zone;
        }

        [Serializable]
        private sealed class Barrier
        {
            public float x;
            public float y;
            public float width;
            public float height;
        }

        [Serializable]
        private sealed class Marking
        {
            public string id;
            public float x;
            public float y;
            public float angle;
            public float scale;
            public float r;
            public float g;
            public float b;
            public float a;
        }

        [Serializable]
        private sealed class SourceLibrary
        {
            public Source[] outputs;
        }

        [Serializable]
        private sealed class Source
        {
            public string id;
            public float pixelsPerUnit;
        }

        [MenuItem("Emergency Shift/Art/Create narrative environment stress scene (once)")]
        public static void Create()
        {
            if (Application.isPlaying || EditorApplication.isCompiling || File.Exists(ScenePath))
            {
                throw new InvalidOperationException("Stop Play/compilation. Edit saved scenes explicitly.");
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                {
                    throw new InvalidOperationException("Save manual edits before opening another scene.");
                }
            }

            string project = Directory.GetParent(Application.dataPath).FullName;
            var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(Path.Combine(project,
                "Concepts/Used/Environment/NarrativeBlock01/layout.json")));
            PrepareLighting(project);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var root = new GameObject("Service block · receiving / analysis / medical evacuation").transform;
            string[] names = { "01 · Receiving and dispatch", "02 · Laboratory night shift", "03 · Medical and staff" };
            var zones = new Transform[3];
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = new GameObject(names[i]).transform;
                zones[i].SetParent(root, false);
                string texture = i == 0 ? "warehouse-concrete-base-01" : "lab-tile-base-01";
                EnvironmentLibraryReviewBuilder.Floor(zones[i], "Floor", new Vector2(i * 14, 0), new Vector2(14, 14), texture);
            }

            EnvironmentLibraryReviewBuilder.Wall(root, new Vector2(14, 7), new Vector2(42.3f, .3f));
            EnvironmentLibraryReviewBuilder.Wall(root, new Vector2(14, -7), new Vector2(42.3f, .3f));
            EnvironmentLibraryReviewBuilder.Wall(root, new Vector2(-7, 0), new Vector2(.3f, 14));
            EnvironmentLibraryReviewBuilder.Wall(root, new Vector2(35, 0), new Vector2(.3f, 14));
            foreach (float x in new[] { 7f, 21f })
            {
                EnvironmentLibraryReviewBuilder.Wall(root, new Vector2(x, 3), new Vector2(.3f, 8));
                EnvironmentLibraryReviewBuilder.Wall(root, new Vector2(x, -5), new Vector2(.3f, 4));
            }

            foreach (var wall in plan.walls)
            {
                EnvironmentLibraryReviewBuilder.Wall(root, new Vector2(wall.x, wall.y), new Vector2(wall.width, wall.height));
            }

            var groups = new Dictionary<string, Transform>();
            var placed = new Dictionary<string, Transform>();
            foreach (var prop in plan.placements)
            {
                if (!groups.TryGetValue(prop.group, out var group))
                {
                    group = new GameObject(prop.group).transform;
                    group.SetParent(zones[prop.zone], false);
                    groups.Add(prop.group, group);
                }

                var obj = EnvironmentLibraryReviewBuilder.Instance(prop.prefab, group, new Vector2(prop.x, prop.y));
                obj.name = prop.name;
                obj.transform.rotation = Quaternion.Euler(0, 0, prop.angle);
                obj.transform.localScale = Vector3.one * prop.scale;
                if (!string.IsNullOrEmpty(prop.parent))
                {
                    obj.transform.SetParent(placed[prop.parent], true);
                }

                placed.Add(prop.name, obj.transform);
            }

            foreach (var marking in plan.markings)
            {
                var obj = EnvironmentLibraryReviewBuilder.Instance(Root + "/Prefabs/Environment/FloorMarkings/" + marking.id + ".prefab",
                    root, new Vector2(marking.x, marking.y));
                obj.transform.rotation = Quaternion.Euler(0, 0, marking.angle);
                obj.transform.localScale = Vector3.one * marking.scale;
                obj.GetComponent<SpriteRenderer>().color = new Color(marking.r, marking.g, marking.b, marking.a);
            }

            var rig = EnvironmentLibraryReviewBuilder.Instance(Root + "/Prefabs/Levels/LevelRig.prefab", null, Vector2.zero);
            var player = rig.GetComponentInChildren<PlayerLife2D>();
            player.transform.position = new Vector3(1.9f, -2, 0);
            var follow = rig.GetComponentInChildren<CameraFollow2D>();
            follow.Configure(player.transform);
            follow.SnapToTarget();
            foreach (var light in rig.GetComponentsInChildren<Light2D>(true))
            {
                if (light.lightType == Light2D.LightType.Global)
                {
                    light.color = Color.black;
                    light.intensity = 0;
                }
            }

            foreach (var label in rig.GetComponentInChildren<PrototypeHud>().GetComponentsInChildren<Text>(true))
            {
                if (label.name == "Title")
                {
                    label.text = "СЛУЖЕБНЫЙ БЛОК · ТЕСТ";
                }
            }

            // Optional local optics test; normal progression/pickup code owns collection.
            EnvironmentLibraryReviewBuilder.Instance(Root + "/Prefabs/Loot/FlashlightRangeUpgrade.prefab", root, new Vector2(.2f, -1));
            EnvironmentLibraryReviewBuilder.RecordOverrides(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException("Could not save service block scene.");
            }

            AssetDatabase.SaveAssets();
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var previous = SceneManager.GetSceneAt(i);
                if (previous != scene && !previous.isDirty)
                {
                    EditorSceneManager.CloseScene(previous, true);
                }
            }

            Selection.activeGameObject = root.gameObject;
        }

        private static void PrepareLighting(string project)
        {
            Directory.CreateDirectory(Root + "/Settings/EnvironmentLights");
            var framing = LoadOrCreate<CameraFramingProfile>(Root + "/Settings/CameraFraming.asset");
            framing.Validate();
            var rig = PrefabUtility.LoadPrefabContents(Root + "/Prefabs/Levels/LevelRig.prefab");
            try
            {
                var follow = rig.GetComponentInChildren<CameraFollow2D>();
                follow.SetFraming(framing);
                follow.Configure(rig.GetComponentInChildren<PlayerLife2D>().transform);
                follow.SnapToTarget();
                PrefabUtility.SaveAsPrefabAsset(rig, Root + "/Prefabs/Levels/LevelRig.prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(rig);
            }

            var library = JsonUtility.FromJson<SourceLibrary>(File.ReadAllText(Path.Combine(project, Sources, "provenance.json")));
            for (int i = 0; i < library.outputs.Length; i++)
            {
                var prop = library.outputs[i];
                var profile = LoadOrCreate<LocalLightProfile>(Root + "/Settings/EnvironmentLights/" + prop.id + ".asset");
                Color key;
                Vector2 outlet = new Vector2(0, -.5f);
                if (prop.id == "working-laptop")
                {
                    key = new Color(0, .8f, 1);
                    profile.color = new Color(.5f, .8f, 1);
                    profile.radius = 1.55f;
                    profile.intensity = .9f;
                    profile.cycleSeconds = 5.4f;
                    profile.minimumFactor = .72f;
                    profile.darkIntervals = new[] { new Vector2(.62f, .67f), new Vector2(.7f, .725f) };
                }
                else if (prop.id == "lab-desk-lamp")
                {
                    key = new Color(1, .65f, .1f);
                    profile.color = new Color(1, .78f, .43f);
                    profile.radius = 2.15f;
                    profile.intensity = 1;
                    profile.minimumFactor = .97f;
                }
                else
                {
                    key = new Color(1, .05f, .03f);
                    profile.color = new Color(1, .15f, .065f);
                    profile.radius = 1.8f;
                    profile.intensity = 1.2f;
                    profile.cycleSeconds = 5.8f;
                    profile.minimumFactor = .85f;
                    profile.darkIntervals = new[] { new Vector2(.12f, .17f), new Vector2(.21f, .27f), new Vector2(.39f, 1) };
                    outlet = new Vector2(0, -.18f);
                }

                profile.Validate();
                EditorUtility.SetDirty(profile);
                CreateLightPrefab(prop, profile, key, outlet);
            }
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        private static void CreateLightPrefab(Source prop, LocalLightProfile profile, Color key, Vector2 outlet)
        {
            string art = Root + "/Art/Environment/PracticalLights01/" + prop.id + ".png";
            EnvironmentSpriteAuthoring.Import(art, prop.pixelsPerUnit);
            string directory = Root + "/Prefabs/Environment/PracticalLights01";
            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
            var obj = new GameObject(prop.id);
            try
            {
                var body = obj.AddComponent<SpriteRenderer>();
                body.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(art);
                body.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Shared/FlatLit.mat");
                body.sortingOrder = 2;
                var aperture = new GameObject("Emissive aperture", typeof(SpriteRenderer));
                aperture.transform.SetParent(obj.transform, false);
                var glow = aperture.GetComponent<SpriteRenderer>();
                glow.sprite = body.sprite;
                glow.sortingOrder = 3;
                var material = new Material(Shader.Find("EmergencyShift/Prop Emission"));
                material.SetColor("_EmissionKey", key);
                string materialPath = Root + "/Materials/Environment/Lighting/" + prop.id + "-emission.mat";
                AssetDatabase.CreateAsset(material, materialPath);
                glow.sharedMaterial = material;

                // 2D has no table height: spill begins just beyond the worktop caster rim.
                var spill = new GameObject("Practical spill", typeof(Light2D), typeof(LocalLamp2D));
                spill.transform.SetParent(obj.transform, false);
                spill.transform.localPosition = outlet;
                var lamp = spill.GetComponent<LocalLamp2D>();
                lamp.Configure(spill.GetComponent<Light2D>(), glow, profile.color, false);
                lamp.SetProfile(profile);
                PrefabUtility.SaveAsPrefabAsset(obj, directory + "/" + prop.id + ".prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }
    }
}
