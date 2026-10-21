using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Health.Config;
using EmergencyShift.Features.Health.Logic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class CheckpointSaveLifetimeTests
    {
        private Scene scene;
        private string folder;
        private string path;
        private LevelDefinition definition;
        private LevelCatalog catalog;
        private LevelCheckpointController2D controller;
        private LevelRunController2D run;
        private PlayerLife2D player;
        private PlayerAccessCards2D cards;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            scene = SceneManager.CreateScene("Checkpoint lifetime " + Guid.NewGuid().ToString("N"));
            folder = Path.Combine(Path.GetTempPath(), "EmergencyShift-save-lifetime-" + Guid.NewGuid().ToString("N"));
            path = Path.Combine(folder, "slot.json");
            GameObject hero = Create("Inactive player health fixture", false);
            player = hero.AddComponent<PlayerLife2D>();
            Set(player, "model", new HealthModelFactory(new TestClock()).Create(new HealthSettings(2, 0)));
            hero.AddComponent<PlayerInteraction>();
            cards = hero.AddComponent<PlayerAccessCards2D>();
            definition = ScriptableObject.CreateInstance<LevelDefinition>();
            definition.levelId = "save-lifetime-" + Guid.NewGuid().ToString("N");
            definition.exitCardId = "test-exit";
            catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            catalog.levels = new[] { definition };
            GameObject owner = Create("Checkpoint owner");
            run = owner.AddComponent<LevelRunController2D>();
            run.enabled = false;
            run.Configure(player, cards);
            run.SetDefinition(definition, null);
            controller = owner.AddComponent<LevelCheckpointController2D>();
            controller.Configure(catalog, definition, run, player);
            controller.UseStore(path);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (scene.IsValid())
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
            if (definition != null)
            {
                var paths = (Dictionary<string, string>)typeof(LevelCheckpointController2D)
                    .GetField("sessionPaths", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                paths.Remove(definition.levelId);
            }
            UnityEngine.Object.Destroy(catalog);
            UnityEngine.Object.Destroy(definition);
            if (folder != null && Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }

        [UnityTest]
        public IEnumerator ClearImmediatelyCancelsOlderRequestAndDeletesAllSlotFiles()
        {
            Assert.That(controller.TrySaveNow("old-a"), Is.True);
            Assert.That(controller.TrySaveNow("old-b"), Is.True);
            File.WriteAllText(path + ".tmp", "interrupted write");
            Assert.That(File.Exists(path + ".bak"), Is.True);
            Assert.That(controller.RequestSave("pending"), Is.True);

            controller.Clear();

            bool savingImmediatelyAfterClear = controller.IsSaving;
            Assert.That(controller.LastCheckpointId, Is.Null);
            yield return Frames(5);
            AssertEmptySlot();
            Assert.That(savingImmediatelyAfterClear, Is.False);
        }

        [UnityTest]
        public IEnumerator ClearWhileDoorMovesPreventsWriteAfterDoorSettles()
        {
            Door2D door = CreateDoor();
            door.Toggle();
            Assert.That(door.IsMoving, Is.True);
            Assert.That(controller.RequestSave("moving-door"), Is.True);
            yield return Frames(2);
            Assert.That(controller.IsSaving, Is.True);
            AssertEmptySlot();

            controller.Clear();

            Assert.That(controller.IsSaving, Is.False);
            float deadline = Time.realtimeSinceStartup + 3;
            while (door.IsMoving && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(door.IsMoving, Is.False);
            yield return Frames(5);
            AssertEmptySlot();
        }

        [UnityTest]
        public IEnumerator ClearWhileGoreIsUnsettledPreventsWriteAfterItSettles()
        {
            GoreBurst2D gore = Create("Controlled unsettled remains").AddComponent<GoreBurst2D>();
            Set(gore, "spawning", true);
            Assert.That(controller.RequestSave("unsettled"), Is.True);
            yield return Frames(2);
            Assert.That(controller.IsSaving, Is.True);
            AssertEmptySlot();

            controller.Clear();

            Assert.That(controller.IsSaving, Is.False);
            Set(gore, "spawning", false);
            Assert.That(gore.IsSettled, Is.True);
            yield return Frames(5);
            AssertEmptySlot();
        }

        [UnityTest]
        public IEnumerator FreshRequestAfterClearCanSavePreviouslyVisitedCheckpoint()
        {
            Assert.That(controller.TrySaveNow("visited"), Is.True);
            Assert.That(controller.RequestSave("pending"), Is.True);
            controller.Clear();
            Assert.That(controller.RequestSave("visited"), Is.True);
            float deadline = Time.realtimeSinceStartup + 3;
            while (controller.IsSaving && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(controller.IsSaving, Is.False);
            Assert.That(controller.LastError, Is.Null);
            Assert.That(new CheckpointStore(path).Read().checkpointId, Is.EqualTo("visited"));
            Assert.That(controller.LastCheckpointId, Is.EqualTo("visited"));
            Assert.That(controller.RequestSave("visited"), Is.False);
        }

        [UnityTest]
        public IEnumerator FinishingLevelStillCancelsPendingSaveAndClearsSlot()
        {
            Assert.That(controller.TrySaveNow("old"), Is.True);
            Assert.That(controller.RequestSave("pending"), Is.True);
            cards.Add(definition.exitCardId);

            Assert.That(run.TryFinish(player), Is.True);

            Assert.That(controller.IsSaving, Is.False);
            yield return Frames(5);
            AssertEmptySlot();
        }

        private void AssertEmptySlot()
        {
            Assert.That(File.Exists(path), Is.False);
            Assert.That(File.Exists(path + ".bak"), Is.False);
            Assert.That(File.Exists(path + ".tmp"), Is.False);
            Assert.That(controller.HasCheckpoint, Is.False);
        }

        private Door2D CreateDoor()
        {
            GameObject obj = Create("Moving door", false);
            GameObject leaf = Create("Door leaf");
            leaf.transform.SetParent(obj.transform, false);
            var door = obj.AddComponent<Door2D>();
            door.Configure(leaf.transform);
            obj.SetActive(true);
            return door;
        }

        private GameObject Create(string name, bool active = true)
        {
            var obj = new GameObject(name);
            obj.SetActive(active);
            SceneManager.MoveGameObjectToScene(obj, scene);
            return obj;
        }

        private static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        private static void Set(object owner, string field, object value) =>
            owner.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(owner, value);

        private sealed class TestClock : IGameClock
        {
            public double Time => 0;
        }
    }
}
