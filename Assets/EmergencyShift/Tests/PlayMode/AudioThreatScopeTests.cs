using System;
using System.Collections;
using EmergencyShift.Core.Composition;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.AudioMood;
using EmergencyShift.Features.AudioMood.Config;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zenject;

namespace EmergencyShift.Tests
{
    public sealed class AudioThreatScopeTests
    {
        private Scene first;
        private Scene second;
        private SceneRegistry<AudioThreatEmitter2D> sourcesA;
        private SceneRegistry<AudioThreatEmitter2D> sourcesB;
        private float originalScale;
        private AudioThreatSignalControllerFactory signals;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            originalScale = Time.timeScale;
            Time.timeScale = 1;
            first = SceneManager.CreateScene("Threat A " + Guid.NewGuid().ToString("N"));
            second = SceneManager.CreateScene("Threat B " + Guid.NewGuid().ToString("N"));
            sourcesA = new SceneRegistry<AudioThreatEmitter2D>();
            sourcesB = new SceneRegistry<AudioThreatEmitter2D>();
            signals = new AudioThreatSignalControllerFactory(new AudioThreatSignalSettings(.02f, .25f, 2, .4f), new UnityGameClock());
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = originalScale;
            if (first.IsValid() && first.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(first);
            }

            if (second.IsValid() && second.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(second);
            }

            sourcesA.Dispose();
            sourcesB.Dispose();
        }

        [UnityTest]
        public IEnumerator InheritedInjectionRegistersOnceAndDisableClearsPersonalCue()
        {
            AudioThreatProbeSource a = Source(first, sourcesA);
            Source(second, sourcesB);
            int firstObservation = a.ObservationId;
            Assert.That(firstObservation, Is.GreaterThan(0));
            a.Construct(sourcesA, signals);
            int reboundObservation = a.ObservationId;
            Assert.That(reboundObservation, Is.GreaterThan(firstObservation));
            Assert.That(sourcesA.Members.Count, Is.EqualTo(1));
            Assert.That(sourcesB.Members.Count, Is.EqualTo(1));
            a.Emit();
            Assert.Throws<ArgumentNullException>(() => a.Construct(null, signals));
            Assert.That(sourcesA.Members.Count, Is.EqualTo(1));
            Assert.That(a.HeardUntil, Is.GreaterThan(Time.time));
            Assert.That(a.UrgentUntil, Is.GreaterThan(Time.time));
            a.enabled = false;
            Assert.That(sourcesA.Members.Count, Is.Zero);
            Assert.That(a.HeardUntil, Is.EqualTo(float.NegativeInfinity));
            Assert.That(a.UrgentUntil, Is.EqualTo(float.NegativeInfinity));
            a.enabled = true;
            Assert.That(a.ObservationId, Is.GreaterThan(reboundObservation));
            Assert.That(sourcesA.Members.Count, Is.EqualTo(1));
            Assert.That(sourcesB.Members.Count, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator FrozenSceneUnloadDoesNotRemoveSiblingThreats()
        {
            Source(first, sourcesA).Emit();
            AudioThreatProbeSource b = Source(second, sourcesB);
            b.Emit();
            Time.timeScale = 0;
            yield return SceneManager.UnloadSceneAsync(first);
            Assert.That(sourcesA.Members.Count, Is.Zero);
            Assert.That(sourcesB.Members.Count, Is.EqualTo(1));
            Assert.That(sourcesB.Members[0], Is.SameAs(b));
        }

        private AudioThreatProbeSource Source(Scene scene, SceneRegistry<AudioThreatEmitter2D> registry)
        {
            var owner = new GameObject("Scoped threat source");
            owner.SetActive(false);
            SceneManager.MoveGameObjectToScene(owner, scene);
            var source = owner.AddComponent<AudioThreatProbeSource>();
            var scope = new DiContainer();
            scope.Bind<SceneRegistry<AudioThreatEmitter2D>>().FromInstance(registry);
            scope.Bind<AudioThreatSignalControllerFactory>().FromInstance(signals);
            scope.Inject(source);
            owner.SetActive(true);
            return source;
        }
    }
}
