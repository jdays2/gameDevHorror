#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.PlayerInput.Logic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class DialogueSessionLifetimeTests
    {
        private Scene scene;
        private DialogueSession session;
        private DialogueView view;
        private DialogueInteraction2D interaction;
        private PlayerInteraction actor;
        private DialogueSequence sequence;
        private GameplayInputBlock block;
        private WorldMotionPause pause;
        private Canvas hud;
        private AimReticle2D reticle;
        private TestInput input;
        private float originalScale;
        private bool originalCursor;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            originalScale = Time.timeScale;
            originalCursor = Cursor.visible;
            Time.timeScale = .6f;
            Cursor.visible = false;
            scene = SceneManager.CreateScene("Dialogue lifetime " + Guid.NewGuid().ToString("N"));
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/UI/DialogueUI.prefab");
            GameObject root = UnityEngine.Object.Instantiate(prefab);
            SceneManager.MoveGameObjectToScene(root, scene);
            session = root.GetComponent<DialogueSession>();
            view = root.GetComponentInChildren<DialogueView>(true);
            hud = Create("HUD").AddComponent<Canvas>();
            GameObject marker = Create("Inactive reticle fixture");
            marker.SetActive(false);
            reticle = marker.AddComponent<AimReticle2D>();
            Set(session, "gameplayHud", hud);
            Set(session, "reticle", reticle);
            block = new GameplayInputBlock();
            pause = new WorldMotionPause(new RealScale());
            input = new TestInput();
            MethodInfo construct = typeof(DialogueSession).GetMethod("Construct", BindingFlags.NonPublic | BindingFlags.Instance);
            // The old signature also permits running this regression against the pre-fix source.
            object[] dependencies = construct.GetParameters().Length == 3
                ? new object[] { input, block, pause }
                : new object[] { new DialogueControllerFactory(block, pause, new RealFrame()), input };
            construct.Invoke(session, dependencies);
            sequence = AssetDatabase.LoadAssetAtPath<DialogueSequence>("Assets/EmergencyShift/Data/Dialogue/SandboxConversation.asset");
            sequence.Validate();
            interaction = Create("Conversation fixture").AddComponent<DialogueInteraction2D>();
            Set(interaction, "sequence", sequence);
            Set(interaction, "session", session);
            actor = Create("Interaction fixture").AddComponent<PlayerInteraction>();
            Set(actor, "inputBlock", block);
            actor.gameObject.SetActive(false);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (scene.IsValid() && scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }

            pause?.Dispose();
            Time.timeScale = originalScale;
            Cursor.visible = originalCursor;
        }

        [UnityTest]
        public IEnumerator DisabledComponentCannotOfferOrStartConversation()
        {
            session.enabled = false;
            actor.gameObject.SetActive(true);
            bool available = interaction.IsAvailable(actor);
            actor.gameObject.SetActive(false);
            Assert.That(available, Is.False);
            Assert.That(session.TryBegin(sequence), Is.False);
            AssertUnchanged();
            yield return null;
            AssertUnchanged();
        }

        [UnityTest]
        public IEnumerator InactiveRootCannotStartConversation()
        {
            session.gameObject.SetActive(false);
            Assert.That(session.TryBegin(sequence), Is.False);
            AssertUnchanged();
            yield return null;
            AssertUnchanged();
        }

        [UnityTest]
        public IEnumerator ReenabledSessionCompletesBothBranchesAndFencesConfirmation()
        {
            for (int choice = 0; choice < 2; choice++)
            {
                session.enabled = false;
                session.enabled = true;
                Assert.That(session.TryBegin(sequence), Is.True);
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(hud.enabled, Is.False);
                Assert.That(reticle.enabled, Is.False);
                Assert.That(view.IsVisible, Is.True);
                string entryId = session.LineId;
                session.Advance();
                Assert.That(session.LineId, Is.EqualTo(entryId));
                int beforeFork = sequence.lines.Length;
                while (!session.RequiresChoice && session.IsOpen && beforeFork-- > 0)
                {
                    yield return null;
                    view.AdvanceButton.onClick.Invoke();
                }

                Assert.That(session.RequiresChoice, Is.True);
                yield return null;
                view.ChoiceButtons[choice].onClick.Invoke();
                Assert.That(session.RequiresChoice, Is.False);
                int remaining = sequence.lines.Length + 1;
                while (session.IsOpen && remaining-- > 0)
                {
                    yield return null;
                    view.AdvanceButton.onClick.Invoke();
                }

                Assert.That(session.IsOpen, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(.6f));
                Assert.That(view.IsVisible, Is.False);
                input.Held = true;
                yield return null;
                yield return null;
                Assert.That(block.IsBlocked, Is.True);
                input.Held = false;
                yield return null;
                yield return null;
                AssertUnchanged();
            }
        }

        [UnityTest]
        public IEnumerator DisablingAnOpenSessionRestoresPresentationAndKeepsOtherOwners()
        {
            Assert.That(session.TryBegin(sequence), Is.True);
            using (pause.Acquire())
            using (block.Acquire())
            {
                session.enabled = false;
                Assert.That(session.IsOpen, Is.False);
                Assert.That(block.IsBlocked, Is.True);
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(hud.enabled, Is.True);
                Assert.That(reticle.enabled, Is.True);
                Assert.That(Cursor.visible, Is.False);
                Assert.That(view.IsVisible, Is.False);
                session.Cancel();
                yield return null;
                Assert.That(block.IsBlocked, Is.True);
            }

            AssertUnchanged();
        }

        private void AssertUnchanged()
        {
            Assert.That(session.IsOpen, Is.False);
            Assert.That(block.IsBlocked, Is.False);
            Assert.That(pause.IsActive, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(.6f));
            Assert.That(hud.enabled, Is.True);
            Assert.That(reticle.enabled, Is.True);
            Assert.That(Cursor.visible, Is.False);
            Assert.That(view.IsVisible, Is.False);
        }

        private GameObject Create(string name)
        {
            var result = new GameObject(name);
            SceneManager.MoveGameObjectToScene(result, scene);
            return result;
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        }

        private sealed class RealFrame : IFrameClock
        {
            public int Frame => Time.frameCount;
        }

        private sealed class RealScale : IWorldTimeScale
        {
            public float Scale { get => Time.timeScale; set => Time.timeScale = value; }
        }

        private sealed class TestInput : IPlayerInput
        {
            public bool Held;
            public Vector2 Movement => Vector2.zero;
            public Vector2 PointerPosition => Vector2.zero;
            public bool HasPointer => false;
            public bool FirePressed => false;
            public bool FireHeld => false;
            public bool InteractPressed => false;
            public bool ReloadPressed => false;
            public bool SelectPistolPressed => false;
            public bool SelectShotgunPressed => false;
            public bool ToggleLightPressed => false;
            public bool RestartPressed => false;
            public bool RefillAmmoPressed => false;
            public bool DialogueAdvancePressed => false;
            public bool PausePressed => false;
            public bool ConfirmationHeld => Held;
        }
    }
}
#endif
