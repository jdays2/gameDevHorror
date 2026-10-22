#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class ProbeCancellationTests
    {
        [UnityTest]
        public IEnumerator DisabledProbesReleaseOwnedInputAndFixtureWithoutResettingUnownedTimeScale()
        {
            float previousScale = Time.timeScale;
            var root = new GameObject("Native probe cancellation checks");
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>("Temporary cancellation keyboard");
            GameObject fixture = new GameObject("Temporary sleeper probe fixture");
            try
            {
                Time.timeScale = .5f;
                var audio = (MonoBehaviour)root.AddComponent(Type.GetType("EmergencyShift.AdaptiveAudioProbe, EmergencyShift.Development", true));
                audio.enabled = false;
                Assert.That(Time.timeScale, Is.EqualTo(.5f), "A probe that never began owns no time-scale override.");
                var sandbox = (MonoBehaviour)root.AddComponent(Type.GetType("EmergencyShift.SandboxRegressionProbe, EmergencyShift.Development", true));
                Set(sandbox, "testKeyboard", keyboard);
                sandbox.enabled = false;
                Assert.That(keyboard.added, Is.False);
                var sleeper = (MonoBehaviour)root.AddComponent(Type.GetType("EmergencyShift.SleeperRegressionProbe, EmergencyShift.Development", true));
                Set(sleeper, "fixture", fixture);
                sleeper.enabled = false;
                yield return null;
                Assert.That(fixture == null, Is.True);
            }
            finally
            {
                UnityEngine.Object.Destroy(root);
                if (fixture != null)
                {
                    UnityEngine.Object.Destroy(fixture);
                }

                if (keyboard.added)
                {
                    InputSystem.RemoveDevice(keyboard);
                }

                Time.timeScale = previousScale;
            }
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
#endif
