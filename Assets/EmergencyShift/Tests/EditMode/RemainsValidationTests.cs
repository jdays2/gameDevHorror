using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class RemainsValidationTests
    {
        private static readonly string[] InvalidFields =
        {
            "position", "parts.null", "part.null", "part.index", "part.position",
            "body.null", "body.position", "body.scale", "body.rotation", "body.color",
            "shadow.null", "shadow.position", "shadow.rotation", "shadow.color",
            "pool.null", "pool.length", "pool.element", "pool.position", "pool.scale",
            "pool.rotation", "pool.color"
        };

        private const string Root = "Assets/EmergencyShift/Prefabs/Enemies/";
        private Scene scene;
        private GoreBurst2D gore;
        private BloodPool2D pool;
        private Sprite[] fragments;
        private string variantId;

        [SetUp]
        public void Setup()
        {
            scene = EditorSceneManager.NewPreviewScene();
            gore = CreateGore("ShadowStalker");
        }

        [TearDown]
        public void Cleanup()
        {
            if (scene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [TestCaseSource(nameof(InvalidFields))]
        public void GoreValidatorRejectsCorruptSnapshot(string field)
        {
            RemainsSnapshot saved = Snapshot();
            Corrupt(saved, field);
            Assert.Throws<InvalidOperationException>(() => gore.ValidateRemains(saved));
        }

        [TestCaseSource(nameof(InvalidFields))]
        public void DirectRecordRestoreRejectsBeforeChangingExistingRecord(string field)
        {
            var record = CreateObject("Existing remains").AddComponent<RemainsRecord2D>();
            record.Configure(pool, fragments, null, null, null, variantId);
            record.transform.position = new Vector3(3, 4, 5);
            RemainsSnapshot saved = Snapshot();
            Corrupt(saved, field);

            Assert.Throws<InvalidOperationException>(() => record.Restore(saved));
            Assert.That(record.transform.position, Is.EqualTo(new Vector3(3, 4, 5)));
            Assert.That(record.transform.childCount, Is.Zero);
            Assert.That(record.Capture().hasPool, Is.False);
        }

        [Test]
        public void InvalidDirectGoreRestoreKeepsItsExistingRemains()
        {
            gore.RestoreRemains(Snapshot());
            RemainsRecord2D existing = FindRecord();
            string before = JsonUtility.ToJson(gore.CaptureRemains());
            RemainsSnapshot invalid = Snapshot();
            invalid.parts[0].shadow = null;

            Assert.Throws<InvalidOperationException>(() => gore.RestoreRemains(invalid));
            Assert.That(FindRecord(), Is.SameAs(existing));
            Assert.That(JsonUtility.ToJson(gore.CaptureRemains()), Is.EqualTo(before));
        }

        [Test]
        public void InvalidNestedPayloadIsRejectedBeforeAnyLevelEntityIsPrepared()
        {
            var definition = ScriptableObject.CreateInstance<LevelDefinition>();
            var catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            try
            {
                EmergencyShift.Editor.LevelPersistenceAuthoring.ConfigureIdentity(definition, "remains-validation");
                EmergencyShift.Editor.LevelPersistenceAuthoring.ReplaceCatalogEntries(catalog, new[] { definition });
                GameObject owner = CreateObject("Test checkpoint controller");
                var controller = owner.AddComponent<LevelCheckpointController2D>();
                controller.Configure(catalog, definition, null, null);
                var first = CreateObject("Pristine inactive entity").AddComponent<PersistentEntity2D>();
                first.AssignId("first");
                first.transform.position = new Vector3(1, 2, 3);
                first.gameObject.SetActive(false);
                var adapter = gore.gameObject.AddComponent<StalkerCheckpoint2D>();
                var second = adapter.GetComponent<PersistentEntity2D>();
                second.AssignId("second");
                RemainsSnapshot invalid = Snapshot();
                invalid.parts[0].shadow = null;
                var payload = new StalkerCheckpoint2D.State
                {
                    format = 1,
                    initialized = true,
                    visualRotation = Quaternion.identity,
                    actor = new ShadowStalker2D.Snapshot
                    {
                        health = 0,
                        state = ShadowStalker2D.BehaviourState.Dead,
                        melee = new EnemyMeleeAttack2D.Snapshot()
                    },
                    hasRemains = true,
                    remains = invalid
                };
                CheckpointEntityState firstState = first.Capture();
                firstState.position = new Vector3(90, 90, 90);
                firstState.active = true;
                var saved = new CheckpointData
                {
                    levelId = definition.LevelId,
                    levelRevision = definition.SaveRevision,
                    checkpointId = "test",
                    entities = new[]
                    {
                        firstState,
                        new CheckpointEntityState
                        {
                            id = second.Id,
                            active = true,
                            rotation = Quaternion.identity,
                            parts = new[] { new CheckpointPart { key = adapter.SaveKey, json = JsonUtility.ToJson(payload) } }
                        }
                    }
                };

                Assert.Throws<InvalidOperationException>(() => controller.Apply(saved));
                Assert.That(first.gameObject.activeSelf, Is.False);
                Assert.That(first.transform.position, Is.EqualTo(new Vector3(1, 2, 3)));
                Assert.That(gore.CaptureRemains(), Is.Null);
                Assert.That(controller.LastCheckpointId, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(definition);
            }
        }

        [TestCase("ShadowStalker")]
        [TestCase("Sleeper")]
        public void ValidJsonRoundTripRestoresSilentlyWithoutChangingProfiles(string prefab)
        {
            gore = CreateGore(prefab);
            var asset = new SerializedObject(gore).FindProperty("profile").objectReferenceValue;
            string profileBefore = EditorJsonUtility.ToJson(asset);
            int launches = 0;
            gore.FragmentLaunched += fragment => launches++;
            RemainsSnapshot saved = JsonUtility.FromJson<RemainsSnapshot>(JsonUtility.ToJson(Snapshot()));

            Assert.DoesNotThrow(() => gore.ValidateRemains(saved));
            gore.RestoreRemains(saved);

            Assert.That(launches, Is.Zero);
            Assert.That(gore.IsSettled, Is.True);
            Assert.That(JsonUtility.ToJson(gore.CaptureRemains()), Is.EqualTo(JsonUtility.ToJson(saved)));
            Assert.That(EditorJsonUtility.ToJson(asset), Is.EqualTo(profileBefore));
            Assert.That(FindRecord().GetComponentsInChildren<GibMotion2D>(true), Is.Empty);
        }

        private GoreBurst2D CreateGore(string prefab)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + prefab + ".prefab").GetComponent<GoreBurst2D>();
            var serialized = new SerializedObject(source);
            pool = (BloodPool2D)serialized.FindProperty("bloodPoolPrefab").objectReferenceValue;
            SerializedProperty pieces = serialized.FindProperty("fragments");
            fragments = new Sprite[pieces.arraySize];
            for (int i = 0; i < fragments.Length; i++)
            {
                fragments[i] = (Sprite)pieces.GetArrayElementAtIndex(i).objectReferenceValue;
            }

            var profile = (GoreProfile)serialized.FindProperty("profile").objectReferenceValue;
            variantId = profile.variants[0].id;
            var result = CreateObject("Test " + prefab + " gore").AddComponent<GoreBurst2D>();
            result.Configure(pool, fragments, null);
            var target = new SerializedObject(result);
            target.FindProperty("profile").objectReferenceValue = profile;
            target.ApplyModifiedPropertiesWithoutUndo();
            return result;
        }

        private RemainsSnapshot Snapshot()
        {
            var renderers = pool.GetComponentsInChildren<SpriteRenderer>(true);
            var states = new RemainsSpriteState[renderers.Length];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = new RemainsSpriteState();
                states[i].Capture(renderers[i]);
            }

            return new RemainsSnapshot
            {
                variantId = variantId,
                position = new Vector3(7, 8, 0),
                hasPool = true,
                pool = states,
                parts = new[]
                {
                    new RemainsPartState
                    {
                        spriteIndex = 0,
                        position = new Vector3(7, 8, 0),
                        body = SpriteState(),
                        hasShadow = true,
                        shadow = SpriteState()
                    }
                }
            };
        }

        private RemainsRecord2D FindRecord()
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var record = root.GetComponent<RemainsRecord2D>();
                if (record != null)
                {
                    return record;
                }
            }

            return null;
        }

        private GameObject CreateObject(string name)
        {
            var obj = new GameObject(name);
            SceneManager.MoveGameObjectToScene(obj, scene);
            return obj;
        }

        private static RemainsSpriteState SpriteState()
        {
            return new RemainsSpriteState { rotation = Quaternion.identity, scale = Vector3.one, color = Color.white, visible = true };
        }

        private static void Corrupt(RemainsSnapshot saved, string field)
        {
            switch (field)
            {
                case "position":
                    saved.position.z = float.NaN;
                    break;
                case "parts.null":
                    saved.parts = null;
                    break;
                case "part.null":
                    saved.parts[0] = null;
                    break;
                case "part.index":
                    saved.parts[0].spriteIndex = int.MaxValue;
                    break;
                case "part.position":
                    saved.parts[0].position.y = float.PositiveInfinity;
                    break;
                case "body.null":
                    saved.parts[0].body = null;
                    break;
                case "body.position":
                    saved.parts[0].body.position.x = float.NaN;
                    break;
                case "body.scale":
                    saved.parts[0].body.scale.z = float.PositiveInfinity;
                    break;
                case "body.rotation":
                    saved.parts[0].body.rotation = new Quaternion();
                    break;
                case "body.color":
                    saved.parts[0].body.color.r = float.NaN;
                    break;
                case "shadow.null":
                    saved.parts[0].shadow = null;
                    break;
                case "shadow.position":
                    saved.parts[0].shadow.position.z = float.NaN;
                    break;
                case "shadow.rotation":
                    saved.parts[0].shadow.rotation.w = float.PositiveInfinity;
                    break;
                case "shadow.color":
                    saved.parts[0].shadow.color.a = float.NaN;
                    break;
                case "pool.null":
                    saved.pool = null;
                    break;
                case "pool.length":
                    saved.pool = Array.Empty<RemainsSpriteState>();
                    break;
                case "pool.element":
                    saved.pool[0] = null;
                    break;
                case "pool.position":
                    saved.pool[0].position.x = float.NaN;
                    break;
                case "pool.scale":
                    saved.pool[0].scale.y = float.PositiveInfinity;
                    break;
                case "pool.rotation":
                    saved.pool[0].rotation = new Quaternion();
                    break;
                case "pool.color":
                    saved.pool[0].color.b = float.NaN;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(field));
            }
        }
    }
}
