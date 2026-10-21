using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Tests
{
    public sealed class CheckpointBooleanTests
    {
        private Scene scene;
        private LevelDefinition definition;
        private LevelCatalog catalog;
        private LevelCheckpointController2D controller;
        private LevelRunController2D run;
        private PersistentEntity2D first;
        private Door2D door;
        private DoorAccess2D access;
        private AccessCardPickup2D card;
        private FlashlightUpgradePickup upgrade;
        private EncounterActivation2D encounter;
        private readonly Dictionary<string, ICheckpointParticipant> participants = new Dictionary<string, ICheckpointParticipant>();

        [SetUp]
        public void Setup()
        {
            scene = EditorSceneManager.NewPreviewScene();
            first = Entity("first");
            first.transform.position = new Vector3(1, 2, 0);
            first.gameObject.SetActive(false);
            GameObject doorObject = Entity("door").gameObject;
            GameObject leaf = Create("leaf");
            leaf.transform.SetParent(doorObject.transform, false);
            door = doorObject.AddComponent<Door2D>();
            door.Configure(leaf.transform);
            door.Construct(new EmergencyShift.Core.Composition.SceneRegistry<Door2D>(),
                new EmergencyShift.Features.Doors.DoorMotionControllerFactory(new EmergencyShift.Core.Time.UnityGameClock()));
            access = doorObject.AddComponent<DoorAccess2D>();
            access.Construct(new EmergencyShift.Features.AccessControl.DoorAccessControllerFactory(new EmergencyShift.Core.Time.UnityGameClock()));
            participants.Add("door", doorObject.AddComponent<DoorCheckpoint2D>());
            card = Entity("card").gameObject.AddComponent<AccessCardPickup2D>();
            card.Construct(new Features.Pickups.PickupControllerFactory());
            participants.Add("card", card.gameObject.AddComponent<CardCheckpoint2D>());
            upgrade = Entity("upgrade").gameObject.AddComponent<FlashlightUpgradePickup>();
            upgrade.Construct(new Features.Pickups.PickupControllerFactory());
            participants.Add("upgrade", upgrade.gameObject.AddComponent<UpgradeCheckpoint2D>());
            encounter = Entity("encounter").gameObject.AddComponent<EncounterActivation2D>();
            participants.Add("encounter", encounter.gameObject.AddComponent<EncounterCheckpoint2D>());
            SetFlags(true);
            definition = ScriptableObject.CreateInstance<LevelDefinition>();
            definition.levelId = "boolean-payload-test";
            catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            catalog.levels = new[] { definition };
            GameObject owner = Create("checkpoint");
            run = owner.AddComponent<LevelRunController2D>();
            run.RestoreElapsed(12);
            var player = Create("player").AddComponent<PlayerLife2D>();
            player.gameObject.AddComponent<PlayerInteraction>().Construct(null, new GameplayInputBlock(),
                new Core.Composition.SceneRegistry<Door2D>(), new Features.Interaction.PlayerInteractionControllerFactory(new Core.Time.UnityGameClock()));
            controller = owner.AddComponent<LevelCheckpointController2D>();
            controller.Configure(catalog, definition, run, player);
        }

        [TearDown]
        public void Cleanup()
        {
            participants.Clear();
            if (scene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
            UnityEngine.Object.DestroyImmediate(catalog);
            UnityEngine.Object.DestroyImmediate(definition);
        }

        [TestCaseSource(nameof(IncompletePayloads))]
        public void MissingBooleansAreRejectedByValidation(string kind, string json)
        {
            Assert.Throws<InvalidOperationException>(() => participants[kind].ValidateCheckpoint(json));
        }

        [TestCaseSource(nameof(IncompletePayloads))]
        public void ApplyRejectsBeforeActivatingMovingOrChangingAnyFlag(string kind, string json)
        {
            CheckpointData snapshot = Snapshot(kind, json);
            string before = Flags();
            Assert.Throws<InvalidOperationException>(() => controller.Apply(snapshot));
            Assert.That(first.gameObject.activeSelf, Is.False);
            Assert.That(first.transform.position, Is.EqualTo(new Vector3(1, 2, 0)));
            Assert.That(Flags(), Is.EqualTo(before));
            Assert.That(run.ElapsedSeconds, Is.EqualTo(12));
            Assert.That(controller.LastCheckpointId, Is.Null);
        }

        [TestCaseSource(nameof(IncompletePayloads))]
        public void DirectRestoreRejectsBeforeChangingExistingState(string kind, string json)
        {
            string before = Flags();
            Assert.Throws<InvalidOperationException>(() => participants[kind].RestoreCheckpoint(json));
            Assert.That(Flags(), Is.EqualTo(before));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void CompleteLegacyDoorRestoresEveryBooleanCombination(bool open, bool unlocked)
        {
            string json = "{\"format\":1,\"open\":" + JsonBool(open) + ",\"unlocked\":" + JsonBool(unlocked) + "}";
            participants["door"].ValidateCheckpoint(json);
            participants["door"].RestoreCheckpoint(json);
            Assert.That(door.IsOpen, Is.EqualTo(open));
            Assert.That(access.IsUnlocked, Is.EqualTo(unlocked));
            participants["door"].ValidateCheckpoint(participants["door"].CaptureCheckpoint());
        }

        [TestCase("card", false)]
        [TestCase("card", true)]
        [TestCase("upgrade", false)]
        [TestCase("upgrade", true)]
        [TestCase("encounter", false)]
        [TestCase("encounter", true)]
        public void CompleteLegacyConsumedRestoresWithOptionalRemainingOmitted(string kind, bool consumed)
        {
            string json = "{\"format\":1,\"consumed\":" + JsonBool(consumed) + "}";
            participants[kind].ValidateCheckpoint(json);
            participants[kind].RestoreCheckpoint(json);
            var read = JsonUtility.FromJson<ConsumedRead>(participants[kind].CaptureCheckpoint());
            Assert.That(read.consumed, Is.EqualTo(consumed));
            Assert.That(read.format, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CompleteCaptureRoundTripAppliesAllFourKinds(bool state)
        {
            SetFlags(state);
            CheckpointData snapshot = Snapshot(null, null);
            SetFlags(!state);
            controller.Apply(JsonUtility.FromJson<CheckpointData>(JsonUtility.ToJson(snapshot)));
            Assert.That(door.IsOpen, Is.EqualTo(state));
            Assert.That(access.IsUnlocked, Is.EqualTo(state));
            Assert.That(card.Collected, Is.EqualTo(state));
            Assert.That(upgrade.Collected, Is.EqualTo(state));
            Assert.That(encounter.Activated, Is.EqualTo(state));
        }

        private static IEnumerable<TestCaseData> IncompletePayloads()
        {
            yield return new TestCaseData("door", "{\"format\":1}");
            yield return new TestCaseData("door", "{\"format\":1,\"open\":false}");
            yield return new TestCaseData("door", "{\"format\":1,\"unlocked\":false}");
            yield return new TestCaseData("door", "{\"format\":1,\"comment\":\"open unlocked\",\"other\":{\"open\":false,\"unlocked\":false}}");
            foreach (string kind in new[] { "card", "upgrade", "encounter" })
            {
                yield return new TestCaseData(kind, "{\"format\":1}");
                yield return new TestCaseData(kind, "{\"format\":1,\"remaining\":0}");
                yield return new TestCaseData(kind, "{\"format\":1,\"other\":{\"consumed\":false}}");
            }
        }

        private CheckpointData Snapshot(string kind, string json)
        {
            var entities = new List<CheckpointEntityState> { first.Capture() };
            entities[0].position = Vector3.one * 90;
            entities[0].active = true;
            foreach (var participant in participants)
            {
                var component = (Component)participant.Value;
                CheckpointEntityState saved = component.GetComponent<PersistentEntity2D>().Capture();
                if (participant.Key == kind)
                {
                    saved.parts[0].json = json;
                }
                entities.Add(saved);
            }
            return new CheckpointData
            {
                levelId = definition.levelId,
                levelRevision = definition.saveRevision,
                checkpointId = "snapshot",
                elapsedSeconds = 4,
                entities = entities.ToArray()
            };
        }

        private void SetFlags(bool value)
        {
            door.RestoreState(value);
            access.RestoreState(value);
            card.RestoreState(value);
            upgrade.RestoreState(value);
            encounter.RestoreState(value);
        }

        private string Flags()
        {
            string result = "";
            foreach (ICheckpointParticipant participant in participants.Values)
            {
                result += participant.CaptureCheckpoint();
            }
            return result;
        }

        private PersistentEntity2D Entity(string id)
        {
            var entity = Create(id).AddComponent<PersistentEntity2D>();
            entity.AssignId(id);
            return entity;
        }

        private GameObject Create(string name)
        {
            var obj = new GameObject(name);
            SceneManager.MoveGameObjectToScene(obj, scene);
            return obj;
        }

        private static string JsonBool(bool value) => value ? "true" : "false";

        [Serializable]
        private sealed class ConsumedRead
        {
            public int format;
            public bool consumed;
        }
    }
}
