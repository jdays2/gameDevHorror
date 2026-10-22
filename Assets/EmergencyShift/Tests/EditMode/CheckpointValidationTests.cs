using System;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Tests
{
    public sealed class CheckpointValidationTests
    {
        private static readonly string[] InvalidNumbers =
        {
            "position.x", "position.y", "position.z", "position.infinity",
            "rotation.x", "rotation.y", "rotation.z", "rotation.w",
            "rotation.zero", "rotation.non-unit", "rotation.overflow",
            "elapsed.nan", "elapsed.infinity", "elapsed.negative"
        };

        private Scene scene;
        private string folder;
        private string path;
        private CheckpointStore store;
        private LevelCatalog catalog;
        private LevelDefinition definition;
        private LevelCheckpointController2D controller;
        private LevelRunController2D run;
        private PersistentEntity2D first;
        private PersistentEntity2D second;

        [SetUp]
        public void Setup()
        {
            scene = EditorSceneManager.NewPreviewScene();
            folder = Path.Combine(Path.GetTempPath(), "EmergencyShift-validation-" + Guid.NewGuid().ToString("N"));
            path = Path.Combine(folder, "slot.json");
            store = new CheckpointStore(path);
            definition = ScriptableObject.CreateInstance<LevelDefinition>();
            EmergencyShift.Editor.LevelPersistenceAuthoring.ConfigureIdentity(definition, "validation-test");
            catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            EmergencyShift.Editor.LevelPersistenceAuthoring.ReplaceCatalogEntries(catalog, new[] { definition });
            GameObject owner = CreateObject("Checkpoint test controller");
            run = owner.AddComponent<LevelRunController2D>();
            run.Construct(null, new Features.LevelProgress.LevelProgressFactory(new Core.Time.UnityGameClock()));
            run.RestoreElapsed(12);
            controller = owner.AddComponent<LevelCheckpointController2D>();
            controller.Construct(new Features.CheckpointSession.CheckpointSessionFactory(), new Features.CheckpointSession.Logic.CheckpointPathRegistry());
            controller.Configure(catalog, definition, run, null);
            first = CreateEntity("first", new Vector3(1, 2, 3));
            second = CreateEntity("second", new Vector3(4, 5, 6));
            first.gameObject.SetActive(false);
        }

        [TearDown]
        public void Cleanup()
        {
            if (scene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
            UnityEngine.Object.DestroyImmediate(catalog);
            UnityEngine.Object.DestroyImmediate(definition);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }

        [TestCaseSource(nameof(InvalidNumbers))]
        public void StoreRejectsInvalidNumbersAndPreservesLastGoodSlot(string field)
        {
            store.Write(Snapshot());
            byte[] before = File.ReadAllBytes(path);
            CheckpointData invalid = Snapshot();
            Corrupt(invalid, field);

            Assert.Throws<InvalidDataException>(() => store.Write(invalid));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
            Assert.That(File.Exists(path + ".tmp"), Is.False);
            Assert.That(File.Exists(path + ".bak"), Is.False);
        }

        [TestCaseSource(nameof(InvalidNumbers))]
        public void ApplyRejectsInvalidNumbersBeforeActivatingOrMovingAnyEntity(string field)
        {
            CheckpointData invalid = Snapshot();
            invalid.entities[0].position = new Vector3(90, 90, 90);
            invalid.entities[0].active = true;
            Corrupt(invalid, field);
            float elapsedBefore = run.ElapsedSeconds;

            Assert.Throws<InvalidDataException>(() => controller.Apply(invalid));

            Assert.That(first.gameObject.activeSelf, Is.False);
            Assert.That(first.transform.position, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(second.transform.position, Is.EqualTo(new Vector3(4, 5, 6)));
            Assert.That(first.transform.rotation, Is.EqualTo(Quaternion.identity));
            Assert.That(second.transform.rotation, Is.EqualTo(Quaternion.identity));
            Assert.That(run.ElapsedSeconds, Is.EqualTo(elapsedBefore));
            Assert.That(controller.LastCheckpointId, Is.Null);
        }

        [Test]
        public void DirectPrepareRejectsOtherInstanceIdBeforePoseOrActivation()
        {
            CheckpointEntityState saved = Snapshot().entities[1];
            saved.id = "another-instance";
            saved.position = Vector3.one * 90;
            Assert.Throws<InvalidDataException>(() => second.PrepareRestore(saved));
            Assert.That(second.transform.position, Is.EqualTo(new Vector3(4, 5, 6)));
        }

        [Test]
        public void DirectRestoreRejectsMissingParticipantBeforeActivation()
        {
            var pickup = second.gameObject.AddComponent<AccessCardPickup2D>();
            pickup.Construct(new Features.Pickups.PickupControllerFactory());
            second.gameObject.AddComponent<CardCheckpoint2D>();
            CheckpointEntityState saved = second.Capture();
            saved.parts[0] = null;
            saved.active = false;
            Assert.Throws<InvalidOperationException>(() => second.Restore(saved));
            Assert.That(second.gameObject.activeSelf, Is.True);
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase(null)]
        public void InvalidVisitHistoryCannotReplaceLastGoodSlot(string id)
        {
            store.Write(Snapshot());
            byte[] before = File.ReadAllBytes(path);
            var invalid = Snapshot();
            invalid.visitedCheckpoints = new[] { id };
            Assert.Throws<InvalidDataException>(() => store.Write(invalid));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
            Assert.That(File.Exists(path + ".tmp"), Is.False);
        }

        [TestCase("position.z")]
        [TestCase("rotation.w")]
        [TestCase("rotation.zero")]
        [TestCase("rotation.non-unit")]
        public void DirectEntityValidationAlsoRejectsInvalidPose(string field)
        {
            CheckpointData invalid = Snapshot();
            Corrupt(invalid, field);
            Assert.Throws<InvalidDataException>(() => second.Validate(invalid.entities[1]));
        }

        [TestCase("rotation.zero")]
        [TestCase("rotation.non-unit")]
        [TestCase("elapsed.negative")]
        public void MalformedNumbersSurviveJsonAndAreRejectedWhenReadingDisk(string field)
        {
            Directory.CreateDirectory(folder);
            CheckpointData invalid = Snapshot();
            Corrupt(invalid, field);
            File.WriteAllText(path, JsonUtility.ToJson(invalid));
            Assert.Throws<InvalidDataException>(() => store.Read());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ValidVersionOneDiskRoundTripPreservesQuaternionAndElapsed(bool negativeQuaternion)
        {
            CheckpointData saved = Snapshot();
            Quaternion rotation = Quaternion.Euler(17, 31, 48);
            if (negativeQuaternion)
            {
                rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
            }

            saved.entities[1].rotation = rotation;
            saved.elapsedSeconds = 123.5f;
            store.Write(saved);
            CheckpointData read = store.Read();
            Assert.That(read.version, Is.EqualTo(1));
            Assert.That(read.entities[1].rotation, Is.EqualTo(rotation));
            Assert.That(read.elapsedSeconds, Is.EqualTo(123.5f));
        }

        private GameObject CreateObject(string name)
        {
            var obj = new GameObject(name);
            SceneManager.MoveGameObjectToScene(obj, scene);
            return obj;
        }

        private PersistentEntity2D CreateEntity(string id, Vector3 position)
        {
            var entity = CreateObject(id).AddComponent<PersistentEntity2D>();
            entity.AssignId(id);
            entity.transform.position = position;
            return entity;
        }

        private CheckpointData Snapshot()
        {
            return new CheckpointData
            {
                levelId = definition.LevelId,
                levelRevision = definition.SaveRevision,
                checkpointId = "checkpoint-a",
                elapsedSeconds = 8,
                entities = new[] { first.Capture(), second.Capture() }
            };
        }

        private static void Corrupt(CheckpointData saved, string field)
        {
            CheckpointEntityState entity = saved.entities[1];
            switch (field)
            {
                case "position.x":
                    entity.position.x = float.NaN;
                    break;
                case "position.y":
                    entity.position.y = float.NaN;
                    break;
                case "position.z":
                    entity.position.z = float.NaN;
                    break;
                case "position.infinity":
                    entity.position.z = float.PositiveInfinity;
                    break;
                case "rotation.x":
                    entity.rotation.x = float.NaN;
                    break;
                case "rotation.y":
                    entity.rotation.y = float.NaN;
                    break;
                case "rotation.z":
                    entity.rotation.z = float.NaN;
                    break;
                case "rotation.w":
                    entity.rotation.w = float.PositiveInfinity;
                    break;
                case "rotation.zero":
                    entity.rotation = new Quaternion();
                    break;
                case "rotation.non-unit":
                    entity.rotation = new Quaternion(0, 0, 0, 2);
                    break;
                case "rotation.overflow":
                    entity.rotation = new Quaternion(float.MaxValue, 0, 0, 1);
                    break;
                case "elapsed.nan":
                    saved.elapsedSeconds = float.NaN;
                    break;
                case "elapsed.infinity":
                    saved.elapsedSeconds = float.PositiveInfinity;
                    break;
                case "elapsed.negative":
                    saved.elapsedSeconds = -1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(field));
            }
        }
    }
}
