using System;
using System.IO;
using System.Reflection;
using EmergencyShift.Core.Time;
using EmergencyShift.Core.Composition;
using EmergencyShift.Core.Randomness;
using EmergencyShift.Features.AccessControl;
using EmergencyShift.Features.Health.Logic;
using EmergencyShift.Features.PlayerInput.Logic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class CheckpointPayloadTests
    {
        private static readonly string[] InvalidModules =
        {
            "duplicate", "null-entry", "null-id", "empty-id", "whitespace-id",
            "negative-range", "negative-angle", "nan-range", "nan-angle",
            "infinite-range", "infinite-angle", "zero-bonus", "negative-count",
            "count-below-unique", "empty-with-count", "null-with-count"
        };

        private Scene scene;
        private GameObject player;
        private PlayerCheckpoint2D checkpoint;
        private FlashlightUpgrades upgrades;
        private Flashlight2D flashlight;
        private PlayerLife2D life;
        private PlayerWeapon2D gun;
        private PlayerAccessCards2D cards;
        private PersistentEntity2D first;
        private PersistentEntity2D hero;
        private LevelCheckpointController2D controller;
        private LevelRunController2D run;
        private LevelDefinition definition;
        private LevelCatalog levels;
        private WeaponCatalog weapons;

        [SetUp]
        public void Setup()
        {
            scene = EditorSceneManager.NewPreviewScene();
            player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Player/Player.prefab"));
            SceneManager.MoveGameObjectToScene(player, scene);
            life = player.GetComponent<PlayerLife2D>();
            player.GetComponent<PlayerInteraction>().Construct(null, new GameplayInputBlock(), new SceneRegistry<Door2D>(),
                new Features.Interaction.PlayerInteractionControllerFactory(new TestClock()),
                new LocalIlluminationQuery2D(new SceneRegistry<LocalLamp2D>(), new SceneRegistry<ShotEffect2D>()));
            Set(life, "models", new HealthModelFactory(new TestClock()));
            Awake(life);
            flashlight = player.GetComponent<Flashlight2D>();
            flashlight.Construct(null, new GameplayInputBlock(), new EmergencyShift.Features.Flashlight.FlashlightControllerFactory());
            Awake(flashlight);
            upgrades = player.GetComponent<FlashlightUpgrades>();
            upgrades.Construct(new EmergencyShift.Features.Flashlight.FlashlightControllerFactory());
            Awake(upgrades);
            gun = player.GetComponent<PlayerWeapon2D>();
            typeof(PlayerWeapon2D).GetMethod("Construct", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(gun, new object[] { null, new GameplayInputBlock(),
                    new WeaponControllerFactory(new TestClock(), new RandomSourceFactory(new System.Random(117))),
                    new ScenePrefabFactory(new Zenject.DiContainer(), player.transform) });
            Awake(gun);
            gun.RestoreState(true, 6, gun.Definition);
            cards = player.GetComponent<PlayerAccessCards2D>();
            if (cards == null)
            {
                cards = player.AddComponent<PlayerAccessCards2D>();
            }
            cards.Construct(new AccessControlFactory());
            cards.Add("live-card");
            Assert.That(upgrades.TryInstall("live-lens", "Test", .2f, 2, out _), Is.True);
            weapons = ScriptableObject.CreateInstance<WeaponCatalog>();
            var catalogData = new SerializedObject(weapons);
            SerializedProperty definitions = catalogData.FindProperty("weapons");
            definitions.arraySize = 1;
            definitions.GetArrayElementAtIndex(0).objectReferenceValue = gun.Definition;
            catalogData.ApplyModifiedPropertiesWithoutUndo();
            checkpoint = player.GetComponent<PlayerCheckpoint2D>();
            if (checkpoint == null)
            {
                checkpoint = player.AddComponent<PlayerCheckpoint2D>();
            }
            checkpoint.Configure(weapons);
            hero = player.GetComponent<PersistentEntity2D>();
            hero.AssignId("hero");
            hero.transform.position = new Vector3(4, 5, 0);

            first = Create("First inactive entity").AddComponent<PersistentEntity2D>();
            first.AssignId("first");
            first.transform.position = new Vector3(1, 2, 0);
            first.gameObject.SetActive(false);
            definition = ScriptableObject.CreateInstance<LevelDefinition>();
            EmergencyShift.Editor.LevelPersistenceAuthoring.ConfigureIdentity(definition, "payload-tests");
            levels = ScriptableObject.CreateInstance<LevelCatalog>();
            EmergencyShift.Editor.LevelPersistenceAuthoring.ReplaceCatalogEntries(levels, new[] { definition });
            GameObject owner = Create("Checkpoint owner");
            run = owner.AddComponent<LevelRunController2D>();
            run.RestoreElapsed(12);
            controller = owner.AddComponent<LevelCheckpointController2D>();
            controller.Configure(levels, definition, run, life);
        }

        [TearDown]
        public void Cleanup()
        {
            if (scene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
            Object.DestroyImmediate(weapons);
            Object.DestroyImmediate(levels);
            Object.DestroyImmediate(definition);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("live-card")]
        public void InvalidCardsAreRejectedBeforeRestoringAnyPlayerState(string invalid)
        {
            var state = JsonUtility.FromJson<PlayerCheckpoint2D.State>(checkpoint.CaptureCheckpoint());
            state.health = 1;
            state.cards = new[] { "live-card", invalid };
            string json = JsonUtility.ToJson(state);
            int healthBefore = life.Health;
            string[] cardsBefore = cards.Capture();
            Assert.Throws<InvalidOperationException>(() => checkpoint.ValidateCheckpoint(json));
            Assert.Throws<InvalidOperationException>(() => checkpoint.RestoreCheckpoint(json));
            Assert.That(life.Health, Is.EqualTo(healthBefore));
            Assert.That(cards.Capture(), Is.EqualTo(cardsBefore));
        }

        [TestCaseSource(nameof(InvalidModules))]
        public void InvalidModulesAreRejectedByPlayerValidationAfterJsonRoundTrip(string fault)
        {
            PlayerCheckpoint2D.State saved = SavedPlayer();
            Corrupt(saved, fault);
            Assert.Throws<InvalidOperationException>(() => checkpoint.ValidateCheckpoint(JsonUtility.ToJson(saved)));
        }

        [TestCaseSource(nameof(InvalidModules))]
        public void ApplyRejectsModulesBeforeChangingAnyEntityOrPlayerState(string fault)
        {
            string before = checkpoint.CaptureCheckpoint();
            PlayerCheckpoint2D.State saved = SavedPlayer();
            saved.health = 1;
            saved.cards = new[] { "saved-card" };
            saved.inventory = new WeaponInventory().Capture(0);
            Corrupt(saved, fault);
            CheckpointData world = World(SavedHero(JsonUtility.ToJson(saved)));

            Assert.Throws<InvalidOperationException>(() => controller.Apply(world));

            AssertUnchangedWorld();
            Assert.That(checkpoint.CaptureCheckpoint(), Is.EqualTo(before));
        }

        [TestCaseSource(nameof(InvalidModules))]
        public void DirectModuleRestoreRejectsBeforeClearingInstalledModulesOrChangingOptics(string fault)
        {
            PlayerCheckpoint2D.State saved = SavedPlayer();
            Corrupt(saved, fault);
            string before = JsonUtility.ToJson(SavedPlayer());
            float range = flashlight.Range;
            float angle = flashlight.BeamAngle;

            Assert.Throws<InvalidOperationException>(() => upgrades.Restore(saved.modules, saved.moduleCount));

            Assert.That(JsonUtility.ToJson(SavedPlayer()), Is.EqualTo(before));
            Assert.That(flashlight.Range, Is.EqualTo(range));
            Assert.That(flashlight.BeamAngle, Is.EqualTo(angle));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void SupportedPlayerFormatsRestoreRepeatedLensWithTotalCountAboveUniqueIds(int format)
        {
            PlayerCheckpoint2D.State saved = SavedPlayer();
            saved.format = format;
            saved.modules = new[] { Module("repeatable-lens", .6f, 4) };
            saved.moduleCount = 3;
            saved.hasFlashlightState = format == 3;
            saved.flashlightOwned = false;
            saved.light = false;

            checkpoint.RestoreCheckpoint(JsonUtility.ToJson(saved));

            Assert.That(upgrades.InstalledCount, Is.EqualTo(3));
            Assert.That(upgrades.HasModule("live-lens"), Is.False);
            Assert.That(upgrades.Capture()[0].id, Is.EqualTo("repeatable-lens"));
            Assert.That(upgrades.Capture()[0].bonus, Is.EqualTo(new Vector2(.6f, 4)));
            Assert.That(flashlight.Range, Is.EqualTo(4.8f).Within(.001f));
            Assert.That(flashlight.IsOwned, Is.EqualTo(format < 3));
            Assert.That(gun.Ammo, Is.EqualTo(6));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void LegacyNullModulesWithZeroCountRestoreBaseOptics(int format)
        {
            PlayerCheckpoint2D.State saved = SavedPlayer();
            saved.format = format;
            saved.modules = null;
            saved.moduleCount = 0;
            checkpoint.RestoreCheckpoint(JsonUtility.ToJson(saved));
            Assert.That(upgrades.Capture(), Is.Empty);
            Assert.That(upgrades.InstalledCount, Is.Zero);
            Assert.That(flashlight.Range, Is.EqualTo(4.2f).Within(.001f));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void DirectPlayerRestoreRejectsDuplicatesInEverySupportedFormatWithoutChanges(int format)
        {
            string before = checkpoint.CaptureCheckpoint();
            PlayerCheckpoint2D.State saved = SavedPlayer();
            saved.format = format;
            saved.health = 1;
            saved.cards = new[] { "saved-card" };
            Corrupt(saved, "duplicate");
            Assert.Throws<InvalidOperationException>(() => checkpoint.RestoreCheckpoint(JsonUtility.ToJson(saved)));
            Assert.That(checkpoint.CaptureCheckpoint(), Is.EqualTo(before));
        }

        [Test]
        public void ActualRepeatableInstallsSurviveIsolatedDiskSaveDeathAndApply()
        {
            for (int i = 0; i < 3; i++)
            {
                Assert.That(upgrades.TryInstall("repeatable", "Test lens", .2f, 1, out _, true), Is.True);
            }
            string expectedModules = JsonUtility.ToJson(SavedPlayer());
            float expectedRange = flashlight.Range;
            CheckpointData saved = World(hero.Capture());
            string folder = Path.Combine(Path.GetTempPath(), "EmergencyShift-payload-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new CheckpointStore(Path.Combine(folder, "slot.json"));
                store.Write(saved);
                Assert.That(life.TakeDamage(2), Is.True);
                Assert.That(life.IsDead, Is.True);
                upgrades.Restore(null, 0);
                cards.Restore(null);
                gun.RestoreState(false, 0, gun.Definition);
                hero.transform.position = Vector3.one * 50;

                controller.Apply(store.Read());

                Assert.That(life.IsDead, Is.False);
                Assert.That(checkpoint.CaptureCheckpoint(), Is.EqualTo(expectedModules));
                Assert.That(upgrades.InstalledCount, Is.EqualTo(4));
                Assert.That(upgrades.Capture().Length, Is.EqualTo(2));
                Assert.That(flashlight.Range, Is.EqualTo(expectedRange).Within(.001f));
                Assert.That(hero.transform.position, Is.EqualTo(new Vector3(4, 5, 0)));
            }
            finally
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
        }

        [TestCase(-1)]
        [TestCase(999)]
        [TestCase(int.MaxValue)]
        public void ApplyRejectsUnknownStalkerStateBeforePreparingAnyEntity(int state)
        {
            StalkerCheckpoint2D stalker = Create("Stalker checkpoint").AddComponent<StalkerCheckpoint2D>();
            PersistentEntity2D entity = stalker.GetComponent<PersistentEntity2D>();
            entity.AssignId("stalker");
            entity.transform.position = new Vector3(7, 8, 0);
            var savedEntity = new CheckpointEntityState
            {
                id = entity.Id,
                active = true,
                position = Vector3.one * 90,
                rotation = Quaternion.identity,
                parts = new[] { new CheckpointPart { key = stalker.SaveKey, json = StalkerJson((ShadowStalker2D.BehaviourState)state) } }
            };
            CheckpointData world = World(SavedHero(checkpoint.CaptureCheckpoint()), savedEntity);
            Assert.Throws<InvalidOperationException>(() => controller.Apply(world));
            AssertUnchangedWorld();
            Assert.That(entity.transform.position, Is.EqualTo(new Vector3(7, 8, 0)));
        }

        [TestCase(-1)]
        [TestCase(999)]
        public void DirectStalkerRestoreRejectsUnknownStateBeforeResolvingActor(int state)
        {
            StalkerCheckpoint2D stalker = Create("Stalker checkpoint").AddComponent<StalkerCheckpoint2D>();
            Assert.Throws<InvalidOperationException>(() => stalker.RestoreCheckpoint(StalkerJson((ShadowStalker2D.BehaviourState)state)));
        }

        [TestCaseSource(nameof(KnownStalkerStates))]
        public void ExistingStalkerStatesIncludingDeadAndLegacyEyePayloadRemainValid(ShadowStalker2D.BehaviourState state)
        {
            StalkerCheckpoint2D stalker = Create("Stalker checkpoint").AddComponent<StalkerCheckpoint2D>();
            Assert.DoesNotThrow(() => stalker.ValidateCheckpoint(StalkerJson(state)));
            Assert.DoesNotThrow(() => stalker.ValidateCheckpoint("{\"format\":1,\"initialized\":false}"));
        }

        private static Array KnownStalkerStates => Enum.GetValues(typeof(ShadowStalker2D.BehaviourState));

        private static string StalkerJson(ShadowStalker2D.BehaviourState state)
        {
            return JsonUtility.ToJson(new StalkerCheckpoint2D.State
            {
                format = 1,
                initialized = true,
                visualRotation = Quaternion.identity,
                actor = new ShadowStalker2D.Snapshot
                {
                    health = state == ShadowStalker2D.BehaviourState.Dead ? 0 : 1,
                    state = state,
                    melee = new EnemyMeleeAttack2D.Snapshot(),
                    eyeBurned = true
                }
            });
        }

        private PlayerCheckpoint2D.State SavedPlayer() => JsonUtility.FromJson<PlayerCheckpoint2D.State>(checkpoint.CaptureCheckpoint());

        private CheckpointEntityState SavedHero(string json)
        {
            CheckpointEntityState saved = hero.Capture();
            saved.position = new Vector3(80, 80, 0);
            foreach (CheckpointPart part in saved.parts)
            {
                if (part.key == checkpoint.SaveKey)
                {
                    part.json = json;
                }
            }
            return saved;
        }

        private CheckpointData World(params CheckpointEntityState[] rest)
        {
            var entities = new CheckpointEntityState[rest.Length + 1];
            entities[0] = first.Capture();
            entities[0].position = Vector3.one * 90;
            entities[0].active = true;
            Array.Copy(rest, 0, entities, 1, rest.Length);
            return new CheckpointData
            {
                levelId = definition.LevelId,
                levelRevision = definition.SaveRevision,
                checkpointId = "new-checkpoint",
                elapsedSeconds = 4,
                entities = entities
            };
        }

        private void AssertUnchangedWorld()
        {
            Assert.That(first.gameObject.activeSelf, Is.False);
            Assert.That(first.transform.position, Is.EqualTo(new Vector3(1, 2, 0)));
            Assert.That(hero.transform.position, Is.EqualTo(new Vector3(4, 5, 0)));
            Assert.That(run.ElapsedSeconds, Is.EqualTo(12));
            Assert.That(controller.LastCheckpointId, Is.Null);
        }

        private GameObject Create(string name)
        {
            var obj = new GameObject(name);
            SceneManager.MoveGameObjectToScene(obj, scene);
            return obj;
        }

        private static FlashlightUpgrades.ModuleState Module(string id, float range, float angle) =>
            new FlashlightUpgrades.ModuleState { id = id, bonus = new Vector2(range, angle) };

        private static void Corrupt(PlayerCheckpoint2D.State saved, string fault)
        {
            saved.modules = new[] { Module("saved-lens", .1f, 1) };
            saved.moduleCount = 1;
            switch (fault)
            {
                case "duplicate":
                    saved.modules = new[] { Module("lens", .1f, 0), Module("lens", .1f, 0) };
                    saved.moduleCount = 2;
                    break;
                case "null-entry":
                    saved.modules[0] = null;
                    break;
                case "null-id":
                    saved.modules[0].id = null;
                    break;
                case "empty-id":
                    saved.modules[0].id = "";
                    break;
                case "whitespace-id":
                    saved.modules[0].id = " \t";
                    break;
                case "negative-range":
                    saved.modules[0].bonus.x = -1;
                    break;
                case "negative-angle":
                    saved.modules[0].bonus.y = -1;
                    break;
                case "nan-range":
                    saved.modules[0].bonus.x = float.NaN;
                    break;
                case "nan-angle":
                    saved.modules[0].bonus.y = float.NaN;
                    break;
                case "infinite-range":
                    saved.modules[0].bonus.x = float.PositiveInfinity;
                    break;
                case "infinite-angle":
                    saved.modules[0].bonus.y = float.NegativeInfinity;
                    break;
                case "zero-bonus":
                    saved.modules[0].bonus = Vector2.zero;
                    break;
                case "negative-count":
                    saved.moduleCount = -1;
                    break;
                case "count-below-unique":
                    saved.moduleCount = 0;
                    break;
                case "empty-with-count":
                    saved.modules = Array.Empty<FlashlightUpgrades.ModuleState>();
                    break;
                case "null-with-count":
                    saved.modules = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(fault));
            }
        }

        private static void Set(object owner, string field, object value) =>
            owner.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(owner, value);

        private static void Awake(object owner) =>
            owner.GetType().GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(owner, null);

        private sealed class TestClock : IGameClock
        {
            public double Time => 0;
        }
    }
}
