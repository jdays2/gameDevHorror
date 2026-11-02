#if UNITY_EDITOR
using System.Collections;
using EmergencyShift.Features.WorldAudio.Config;
using EmergencyShift.Features.WorldAudio.View;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class WorldAudioBusTests
    {
        [UnityTest]
        public IEnumerator MixerLeasesPreserveBaselineAndCannotUnmuteAnotherLevel()
        {
            yield return null;
            WorldAudioProfile profile = AssetDatabase.LoadAssetAtPath<WorldAudioProfile>("Assets/EmergencyShift/Data/Audio/WorldDeathTransition.asset");
            profile.Validate();
            var mixer = profile.WorldOutput.audioMixer;
            string parameter = profile.AttenuationParameter;
            Assert.That(mixer.GetFloat(parameter, out float original), Is.True);
            var owner = new GameObject("World audio route fixture");
            var world = owner.AddComponent<AudioSource>();
            var ui = owner.AddComponent<AudioSource>();
            world.volume = .37f;
            ui.volume = .63f;
            try
            {
                Assert.That(mixer.SetFloat(parameter, -12), Is.True);
                using (var bus = new WorldAudioBus(profile))
                {
                    bus.Route(world);
                    Assert.That(world.volume, Is.EqualTo(.37f));
                    Assert.That(ui.volume, Is.EqualTo(.63f));
                    Assert.That(ui.outputAudioMixerGroup, Is.Null);
                    using (var first = bus.AcquireGain())
                    using (var second = bus.AcquireGain())
                    {
                        first.Apply(0);
                        second.Apply(.5f);
                        first.Dispose();
                        Assert.That(bus.Gain, Is.EqualTo(.5f));
                        Assert.That(mixer.GetFloat(parameter, out float partial), Is.True);
                        Assert.That(partial, Is.LessThan(-12));
                    }
                    Assert.That(mixer.GetFloat(parameter, out float restored), Is.True);
                    Assert.That(restored, Is.EqualTo(-12));
                }
            }
            finally
            {
                mixer.SetFloat(parameter, original);
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

    }
}
#endif
