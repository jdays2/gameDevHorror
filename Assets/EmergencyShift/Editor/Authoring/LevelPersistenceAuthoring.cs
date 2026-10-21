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
            ConfigureIdentity(data, sceneName == "FirstShift" ? "first-shift" : sceneName.ToLowerInvariant(),
                "Assets/EmergencyShift/Scenes/" + sceneName + ".unity");
            if (sceneName == "FirstShift")
            {
                LevelObjectiveRule[] rules =
                {
                    new LevelObjectiveRule(ObjectiveCondition.HasCard, "evacuation", 3, "ЦЕЛЬ: восточный шлюз → зелёный выход"),
                    new LevelObjectiveRule(ObjectiveCondition.MissingCard, "service", 0, "ЦЕЛЬ: найти служебную карту · северная комната ↑"),
                    new LevelObjectiveRule(ObjectiveCondition.WeaponMissing, null, 1, "ЦЕЛЬ: найти пистолет · распределитель →"),
                    new LevelObjectiveRule(ObjectiveCondition.Always, null, 2, "ЦЕЛЬ: найти эвакуационную карту · узел управления ↑")
                };
                var serialized = new SerializedObject(data);
                SerializedProperty entries = serialized.FindProperty("objectives");
                entries.arraySize = rules.Length;
                for (int index = 0; index < rules.Length; index++)
                {
                    SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                    entry.FindPropertyRelative("condition").intValue = (int)rules[index].Condition;
                    entry.FindPropertyRelative("cardId").stringValue = rules[index].CardId;
                    entry.FindPropertyRelative("phase").intValue = rules[index].Phase;
                    entry.FindPropertyRelative("text").stringValue = rules[index].Text;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(data);
            return data;
        }

        public static void ConfigureIdentity(LevelDefinition data, string levelId, string scenePath = null, string exitCardId = null)
        {
            if (data == null || string.IsNullOrWhiteSpace(levelId))
            {
                throw new ArgumentException("A level definition and stable ID are required.");
            }

            LevelDefinition preview = UnityEngine.Object.Instantiate(data);
            try
            {
                ApplyIdentity(preview, levelId, scenePath, exitCardId);
                preview.Validate();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }

            ApplyIdentity(data, levelId, scenePath, exitCardId);
        }

        private static void ApplyIdentity(LevelDefinition data, string levelId, string scenePath, string exitCardId)
        {
            var serialized = new SerializedObject(data);
            serialized.FindProperty("levelId").stringValue = levelId;
            if (scenePath != null)
            {
                serialized.FindProperty("scenePath").stringValue = scenePath;
            }

            if (exitCardId != null)
            {
                serialized.FindProperty("exitCardId").stringValue = exitCardId;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ReplaceCatalogEntries(LevelCatalog catalog, LevelDefinition[] levels)
        {
            if (catalog == null || levels == null)
            {
                throw new ArgumentException("A catalog and its entries are required.");
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (LevelDefinition level in levels)
            {
                if (level == null)
                {
                    throw new InvalidOperationException("Missing level definition.");
                }

                level.Validate();
                if (!ids.Add(level.LevelId))
                {
                    throw new InvalidOperationException("Duplicate level ID: " + level.LevelId);
                }
            }

            var serialized = new SerializedObject(catalog);
            SerializedProperty entries = serialized.FindProperty("levels");
            entries.arraySize = levels.Length;
            for (int index = 0; index < levels.Length; index++)
            {
                entries.GetArrayElementAtIndex(index).objectReferenceValue = levels[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
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
                    if (catalog.Find(definition.LevelId) == null)
                    {
                        var entries = new List<LevelDefinition>();
                        for (int index = 0; index < catalog.DefinitionCount; index++)
                        {
                            entries.Add(catalog.DefinitionAt(index));
                        }

                        entries.Add(definition);
                        ReplaceCatalogEntries(catalog, entries.ToArray());
                        EditorUtility.SetDirty(catalog);
                    }

                    var checkpoints = Ensure<LevelCheckpointController2D>(flow.gameObject);
                    checkpoints.Configure(catalog, definition, flow, flow.Player);

                }
            }
        }

    }
}
