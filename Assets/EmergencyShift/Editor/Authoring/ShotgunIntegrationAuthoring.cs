using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmergencyShift.Editor
{
    /// <summary>Adds weapon fixtures to the existing range; never generates or replaces room geometry.</summary>
    public static class ShotgunIntegrationAuthoring
    {
        private const string Root = "Assets/EmergencyShift";
        private const string DefinitionPath = Root + "/Data/Weapons/PumpShotgun.asset";
        private const string PickupPath = Root + "/Prefabs/Loot/ShotgunPickup.prefab";
        private const string AmmoPath = Root + "/Prefabs/Loot/ShotgunAmmoPickup.prefab";

        [MenuItem("Emergency Shift/Sandbox/Add pump shotgun to existing range")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != SandboxSceneBuilder.ScenePath || scene.isDirty)
            {
                throw new InvalidOperationException("Stop Play, save edits, and open MechanicsSandbox first.");
            }

            ApplyTo(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        public static void ApplyTo(Scene scene)
        {
            var definition = PrepareDefinition();
            PreparePickup(PickupPath, Root + "/Prefabs/Loot/PistolPickup.prefab", definition, WeaponLootPickup.LootKind.Weapon, 16);
            PreparePickup(AmmoPath, Root + "/Prefabs/Loot/AmmoPickup.prefab", definition, WeaponLootPickup.LootKind.Ammo, 12);
            SandboxHealthZone2D zone = null;
            PrototypeHud hud = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (zone == null)
                {
                    zone = root.GetComponentInChildren<SandboxHealthZone2D>(true);
                }

                if (hud == null)
                {
                    hud = root.GetComponentInChildren<PrototypeHud>(true);
                }
            }

            if (zone == null || hud == null)
            {
                throw new InvalidOperationException("The authored range/rig is missing.");
            }

            var range = zone.transform.parent;
            Place(scene, range, PickupPath, "Range shotgun", new Vector2(46, 2));
            Place(scene, range, AmmoPath, "Range shotgun shells", new Vector2(54, 2));
            var font = hud.GetComponentInChildren<Text>(true).font;
            Sign(range, font, "Shotgun pickup label", "ДРОБОВИК · [2]", new Vector2(46, 3.1f));
            Sign(range, font, "Shotgun shells label", "ПАТРОНЫ ДРОБОВИКА", new Vector2(54, 3.1f));
            if (hud.transform.Find("Weapon test controls") == null)
            {
                var obj = new GameObject("Weapon test controls", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                obj.transform.SetParent(hud.transform, false);
                var text = obj.GetComponent<Text>();
                text.font = font;
                text.fontSize = 15;
                text.color = new Color(.7f, .8f, .76f);
                text.raycastTarget = false;
                text.text = "[1] Пистолет   [2] Дробовик   [X] Перезарядить выбранное оружие";
                var rect = text.rectTransform;
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(24, -96);
                rect.sizeDelta = new Vector2(1050, 28);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalog>(Root + "/Data/Levels/Weapons.asset");
            var entries = new List<WeaponDefinition>(catalog.weapons ?? Array.Empty<WeaponDefinition>());
            if (!entries.Contains(definition))
            {
                entries.Add(definition);
                catalog.weapons = entries.ToArray();
                EditorUtility.SetDirty(catalog);
            }
        }

        private static PumpShotgunDefinition PrepareDefinition()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PumpShotgunDefinition>(DefinitionPath);
            if (existing != null)
            {
                return existing;
            }

            var audio = ScriptableObject.CreateInstance<WeaponAudioProfile>();
            audio.shots = Clips("shotgun", 3, Root + "/Audio/FirearmMix02");
            audio.pump = Clips("pump", 3);
            audio.shellInsert = Clips("shell_insert", 3);
            audio.casings = Clips("hull_concrete", 1);
            audio.emptyTrigger = Clips("empty", 1, Root + "/Audio/FirearmMix02")[0];
            audio.shotGain = .92f;
            audio.emptyGain = .3f;
            AssetDatabase.CreateAsset(audio, Root + "/Data/Audio/ShotgunPump.asset");
            var visual = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<WeaponDefinition>(Root + "/Data/Weapons/PistolDefinition.asset").Visuals);
            visual.displayName = "Дробовик · короткая вспышка";
            visual.plumeWidth = 1.1f;
            visual.plumeLength = .9f;
            AssetDatabase.CreateAsset(visual, Root + "/Data/Weapons/ShotgunVisual.asset");
            var definition = ScriptableObject.CreateInstance<PumpShotgunDefinition>();
            var data = new SerializedObject(definition);
            data.FindProperty("contentId").stringValue = "pump-shotgun";
            data.FindProperty("displayName").stringValue = "Дробовик";
            data.FindProperty("selectionSlot").intValue = 1;
            data.FindProperty("damage").intValue = 1;
            data.FindProperty("maximumAmmo").intValue = 36;
            data.FindProperty("secondsBetweenShots").floatValue = .9f;
            data.FindProperty("range").floatValue = 8;
            data.FindProperty("visuals").objectReferenceValue = visual;
            data.FindProperty("audioProfile").objectReferenceValue = audio;
            data.ApplyModifiedPropertiesWithoutUndo();
            definition.Validate();
            AssetDatabase.CreateAsset(definition, DefinitionPath);
            return definition;
        }

        private static AudioClip[] Clips(string prefix, int count, string folder = null)
        {
            if (folder == null)
            {
                folder = Root + "/Audio/ShotgunPump";
            }

            var clips = new AudioClip[count];
            for (int i = 0; i < count; i++)
            {
                string path = folder + "/" + prefix + "_" + (i + 1).ToString("00") + ".wav";
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clips[i] == null)
                {
                    throw new InvalidOperationException("Missing recorded sound: " + path);
                }
            }

            return clips;
        }

        private static void PreparePickup(string path, string source, WeaponDefinition definition, WeaponLootPickup.LootKind kind, int amount)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                return;
            }

            var obj = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(source));
            try
            {
                obj.name = kind == WeaponLootPickup.LootKind.Weapon ? "ShotgunPickup" : "ShotgunAmmoPickup";
                obj.GetComponent<WeaponLootPickup>().Configure(kind, amount, definition);
                foreach (var renderer in obj.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    renderer.color *= new Color(.9f, .65f, .5f);
                    if (kind == WeaponLootPickup.LootKind.Weapon)
                    {
                        renderer.transform.localScale = Vector3.Scale(renderer.transform.localScale, new Vector3(1.15f, 1.8f, 1));
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(obj, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        private static void Place(Scene scene, Transform parent, string path, string name, Vector2 position)
        {
            if (parent.Find(name) != null)
            {
                return;
            }

            var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), scene);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
        }

        private static void Sign(Transform parent, Font font, string name, string message, Vector2 position)
        {
            if (parent.Find(name) != null)
            {
                return;
            }

            var obj = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            obj.transform.localScale = Vector3.one * .01f;
            var canvas = obj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 6;
            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(obj.transform, false);
            var text = label.GetComponent<Text>();
            text.font = font;
            text.fontSize = 20;
            text.color = new Color(.82f, .68f, .5f);
            text.raycastTarget = false;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = message;
            text.rectTransform.sizeDelta = new Vector2(650, 60);
        }
    }
}
