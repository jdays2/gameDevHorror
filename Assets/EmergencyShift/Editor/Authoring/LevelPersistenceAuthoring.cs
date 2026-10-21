using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmergencyShift.Editor
{
    /// <summary>Explicit authoring command. Adds metadata/components; never rebuilds room geometry.</summary>
    public static class LevelPersistenceAuthoring
    {
        private const string Data = "Assets/EmergencyShift/Data/Levels";
        public static T LoadOrCreate<T>(string path)
            where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static LevelDefinition Definition(string sceneName)
        {
            System.IO.Directory.CreateDirectory(Data);
            AssetDatabase.Refresh();
            var existing = AssetDatabase.LoadAssetAtPath<LevelDefinition>(Data + "/" + sceneName + ".asset");
            if (existing != null)
            {
                return existing;
            }

            var data = LoadOrCreate<LevelDefinition>(Data + "/" + sceneName + ".asset");
            data.levelId = sceneName == "FirstShift" ? "first-shift" : sceneName.ToLowerInvariant();
            data.scenePath = "Assets/EmergencyShift/Scenes/" + sceneName + ".unity";
            if (sceneName == "FirstShift")
            {
                data.objectives = new[]
                {
                    new LevelObjectiveRule
                    {
                        condition = ObjectiveCondition.HasCard,
                        cardId = "evacuation",
                        phase = 3,
                        text = "ЦЕЛЬ: восточный шлюз → зелёный выход"
                    },
                    new LevelObjectiveRule
                    {
                        condition = ObjectiveCondition.MissingCard,
                        cardId = "service",
                        phase = 0,
                        text = "ЦЕЛЬ: найти служебную карту · северная комната ↑"
                    },
                    new LevelObjectiveRule
                    {
                        condition = ObjectiveCondition.WeaponMissing,
                        phase = 1,
                        text = "ЦЕЛЬ: найти пистолет · распределитель →"
                    },
                    new LevelObjectiveRule
                    {
                        condition = ObjectiveCondition.Always,
                        phase = 2,
                        text = "ЦЕЛЬ: найти эвакуационную карту · узел управления ↑"
                    }
                };
            }

            EditorUtility.SetDirty(data);
            return data;
        }

        private static T Ensure<T>(GameObject root)
            where T : Component
        {
            var component = root.GetComponent<T>();
            return component != null ? component : root.AddComponent<T>();
        }

        private static void AddIdentity(GameObject root, HashSet<string> ids)
        {
            var identity = Ensure<PersistentEntity2D>(root);
            if (string.IsNullOrWhiteSpace(identity.Id) || !ids.Add(identity.Id))
            {
                identity.AssignId(Guid.NewGuid().ToString("N"));
                ids.Add(identity.Id);
            }

            EditorUtility.SetDirty(identity);
        }

        [MenuItem("Emergency Shift/Levels/Prepare active level persistence")]
        public static void PrepareActive()
        {
            Prepare(SceneManager.GetActiveScene());
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        public static void Prepare(Scene scene)
        {
            var ids = new HashSet<string>();
            System.IO.Directory.CreateDirectory(Data);
            AssetDatabase.Refresh();
            var weaponCatalog = LoadOrCreate<WeaponCatalog>(Data + "/Weapons.asset");
            if (weaponCatalog.DefinitionCount == 0)
            {
                var data = new SerializedObject(weaponCatalog);
                SerializedProperty definitions = data.FindProperty("weapons");
                definitions.arraySize = 1;
                definitions.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/PistolDefinition.asset");
                data.ApplyModifiedPropertiesWithoutUndo();
            }

            weaponCatalog.Validate();

            EditorUtility.SetDirty(weaponCatalog);
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var player in root.GetComponentsInChildren<PlayerLife2D>(true))
                {
                    AddIdentity(player.gameObject, ids);
                    if (player.GetComponent<PlayerCheckpoint2D>() == null)
                        player.gameObject.AddComponent<PlayerCheckpoint2D>().Configure(weaponCatalog);
                    Ensure<LevelParticipant2D>(player.gameObject).Configure(new Behaviour[] { player.GetComponent<PlayerMotor2D>(), player.GetComponent<PlayerInteraction>(), player.GetComponent<PlayerWeapon2D>() }, player.GetComponent<Rigidbody2D>());
                }

                foreach (var actor in root.GetComponentsInChildren<ShadowStalker2D>(true))
                {
                    AddIdentity(actor.gameObject, ids);
                    Ensure<StalkerCheckpoint2D>(actor.gameObject);
                    Ensure<LevelParticipant2D>(actor.gameObject).Configure(new Behaviour[] { actor, actor.GetComponent<StalkerAudio2D>(), actor.GetComponent<StalkerAudioThreat2D>() }, actor.GetComponent<Rigidbody2D>());
                }

                foreach (var door in root.GetComponentsInChildren<Door2D>(true))
                {
                    AddIdentity(door.gameObject, ids);
                    Ensure<DoorCheckpoint2D>(door.gameObject);
                }

                foreach (var loot in root.GetComponentsInChildren<WeaponLootPickup>(true))
                {
                    AddIdentity(loot.gameObject, ids);
                    Ensure<WeaponLootCheckpoint2D>(loot.gameObject);
                }

                foreach (var loot in root.GetComponentsInChildren<FlashlightUpgradePickup>(true))
                {
                    AddIdentity(loot.gameObject, ids);
                    Ensure<UpgradeCheckpoint2D>(loot.gameObject);
                }

                foreach (var loot in root.GetComponentsInChildren<AccessCardPickup2D>(true))
                {
                    AddIdentity(loot.gameObject, ids);
                    Ensure<CardCheckpoint2D>(loot.gameObject);
                }

                foreach (var encounter in root.GetComponentsInChildren<EncounterActivation2D>(true))
                {
                    AddIdentity(encounter.gameObject, ids);
                    Ensure<EncounterCheckpoint2D>(encounter.gameObject);
                    Ensure<LevelParticipant2D>(encounter.gameObject).Configure(new Behaviour[] { encounter }, null);
                }
            }

            // Prefabs of new species already carry their own save adapter and lifetime component.
            // Assign/deduplicate their placed IDs without adding species cases to this tool.
            var authoredIds = new HashSet<string>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var identity in root.GetComponentsInChildren<PersistentEntity2D>(true))
                {
                    AddIdentity(identity.gameObject, authoredIds);
                }
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var flow in root.GetComponentsInChildren<LevelRunController2D>(true))
                {
                    var definition = flow.Definition != null ? flow.Definition : Definition(scene.name);
                    var objectives = Ensure<LevelObjectiveController2D>(flow.gameObject);
                    objectives.Configure(definition, flow.Player.GetComponent<PlayerAccessCards2D>(), flow.Player.GetComponent<PlayerWeapon2D>());
                    flow.SetDefinition(definition, objectives);
                    var catalog = LoadOrCreate<LevelCatalog>(Data + "/Campaign.asset");
                    if (catalog.Find(definition.levelId) == null)
                    {
                        var entries = new List<LevelDefinition>(catalog.levels ?? Array.Empty<LevelDefinition>());
                        entries.Add(definition);
                        catalog.levels = entries.ToArray();
                        EditorUtility.SetDirty(catalog);
                    }

                    var checkpoints = Ensure<LevelCheckpointController2D>(flow.gameObject);
                    checkpoints.Configure(catalog, definition, flow, flow.Player);

                }
            }
        }

    }
}
