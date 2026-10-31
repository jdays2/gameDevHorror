using System;
using System.Collections.Generic;
using System.Reflection;
using EmergencyShift.Features.Scorpion.Config;
using EmergencyShift.Features.Scorpion.View;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class ScorpionAudioHitTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<Object> temporary = new List<Object>();
        private readonly Dictionary<string, AudioSource> sources = new Dictionary<string, AudioSource>();
        private Scene previewScene;
        private ScorpionPresentationSet presentation;
        private ScorpionAudio2D feedback;
        private Transform listener;

        [SetUp]
        public void CreateIsolatedViewWithoutStartingTheBrainOrAudioPlayback()
        {
            previewScene = EditorSceneManager.NewPreviewScene();
            var actorObject = new GameObject("Temporary Scorpion audio mix");
            SceneManager.MoveGameObjectToScene(actorObject, previewScene);
            actorObject.SetActive(false);
            temporary.Add(actorObject);
            var actor = actorObject.AddComponent<Scorpion2D>();
            feedback = actorObject.AddComponent<ScorpionAudio2D>();
            presentation = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ScorpionPresentationSet>(
                "Assets/EmergencyShift/Data/Enemies/Scorpion/Presentation.asset"));
            temporary.Add(presentation);
            var config = new SerializedObject(presentation);
            config.FindProperty("hit").objectReferenceValue = presentation.Alarm;
            config.FindProperty("hitVolume").floatValue = .75f;
            config.ApplyModifiedPropertiesWithoutUndo();
            Set(actor, "presentation", presentation);
            Set(feedback, "actor", actor);
            var listenerObject = new GameObject("Temporary mix listener");
            SceneManager.MoveGameObjectToScene(listenerObject, previewScene);
            listener = listenerObject.transform;
            temporary.Add(listenerObject);
            Set(actor, "target", listener);
            foreach (string name in new[] { "voice", "claws", "contact", "presence", "detection" })
            {
                var sourceObject = new GameObject(name);
                SceneManager.MoveGameObjectToScene(sourceObject, previewScene);
                sourceObject.transform.SetParent(actorObject.transform, false);
                var source = sourceObject.AddComponent<AudioSource>();
                sources.Add(name, source);
                Set(feedback, name, source);
            }
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (Object item in temporary)
            {
                Object.DestroyImmediate(item);
            }
            temporary.Clear();
            sources.Clear();
            EditorSceneManager.ClosePreviewScene(previewScene);
        }

        [Test]
        public void HurtVoiceUsesItsOwnGainWithoutBoostingTheOtherChannels()
        {
            sources["voice"].clip = presentation.Hit;
            Mix();
            Assert.That(sources["voice"].volume, Is.EqualTo(presentation.HitVolume).Within(.0001f));
            Assert.That(sources["claws"].volume, Is.EqualTo(presentation.VoiceVolume).Within(.0001f));
            Assert.That(sources["contact"].volume, Is.EqualTo(presentation.StepVolume).Within(.0001f));
            Assert.That(sources["presence"].volume, Is.EqualTo(presentation.SqueakVolume).Within(.0001f));
            Assert.That(sources["detection"].volume, Is.EqualTo(presentation.DetectionVolume).Within(.0001f));
        }

        [Test]
        public void DeathRetainsItsSharedBankGainInsteadOfTheHurtGain()
        {
            sources["voice"].clip = presentation.Hit;
            Set(feedback, "deathPlayed", true);
            Mix();
            Assert.That(sources["voice"].volume,
                Is.EqualTo(presentation.VoiceVolume * presentation.DeathSounds.Volume).Within(.0001f));
        }

        [Test]
        public void HurtVoiceStillRespectsItsHearingRadius()
        {
            sources["voice"].clip = presentation.Hit;
            listener.position = Vector2.up * (presentation.SoundRadius + 1);
            Mix();
            Assert.That(sources["voice"].volume, Is.Zero);
            listener.position = Vector3.zero;
            Mix();
            Assert.That(sources["voice"].volume, Is.EqualTo(presentation.HitVolume).Within(.0001f));
        }

        [Test]
        public void FatalHitCannotReplaceTheDeathVoiceWithTheInsectPainClip()
        {
            AudioClip death = presentation.DeathSounds.Clips[0];
            sources["voice"].clip = death;
            Set(feedback, "deathPlayed", true);
            // The isolated actor has no initialized living health, matching the fatal-hit guard.
            typeof(ScorpionAudio2D).GetMethod("OnHit", PrivateInstance).Invoke(feedback, null);
            Assert.That(sources["voice"].clip, Is.SameAs(death));
            Assert.That(feedback.HitsPlayed, Is.Zero);
        }

        [TestCase(-.1f)]
        [TestCase(1.1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void HurtGainRejectsInvalidSharedConfig(float gain)
        {
            var serialized = new SerializedObject(presentation);
            serialized.FindProperty("hitVolume").floatValue = gain;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => presentation.Validate());
        }

        private void Mix() => typeof(ScorpionAudio2D).GetMethod("UpdateMix", PrivateInstance).Invoke(feedback, null);

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, PrivateInstance).SetValue(target, value);
    }
}
