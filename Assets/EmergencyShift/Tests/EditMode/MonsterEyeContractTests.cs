using System;
using System.Linq;
using EmergencyShift.Core.Randomness;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class MonsterEyeContractTests
    {
        private static StalkerSettings Settings() => new StalkerSettings(1.5f, 3.8f, 1.05f, .2f, 1.05f, 3,
            3.2f, .45f, .8f, .09f, 1.6f, 1.9f, .3f, 10, .35f);

        [Test]
        public void ApproachWarningIsReversibleAndCannotCreateBurnOrSound()
        {
            var eye = new StalkerEyeModel(Settings());
            int sounds = 0;
            eye.BurnStarted += () => sounds++;
            eye.Step(true, 2.5f, true, 2);
            Assert.That(eye.Warning, Is.GreaterThan(.2f));
            Assert.That(eye.Damage, Is.Zero);
            Assert.That(eye.Ready, Is.False);
            eye.Step(false, 2.5f, true, .5f);
            Assert.That(eye.Warning, Is.Zero);
            Assert.That(sounds, Is.Zero);
        }

        [Test]
        public void ABriefCloseSweepCannotSkipTheWarningAndOnlyThresholdCrossingSounds()
        {
            var eye = new StalkerEyeModel(Settings());
            int sounds = 0;
            eye.BurnStarted += () => sounds++;
            eye.Step(true, 1.5f, true, .3f);
            eye.Step(false, 1.5f, true, .05f);
            eye.Step(true, 1.5f, true, .4f);
            Assert.That(sounds, Is.Zero);
            Assert.That(eye.Damage, Is.Zero);
            eye.Step(true, 1.5f, true, .1f);
            Assert.That(sounds, Is.EqualTo(1));
            Assert.That(eye.Damage, Is.InRange(.15f, .18f));
            eye.Step(true, 1.5f, true, 1);
            Assert.That(sounds, Is.EqualTo(1));
            Assert.That(eye.Damage, Is.EqualTo(1));
        }

        [Test]
        public void BurnDoseUsesOnlyTimeAfterThresholdEvenInALongFrame()
        {
            var single = new StalkerEyeModel(Settings());
            var stepped = new StalkerEyeModel(Settings());
            single.Step(true, 1.5f, true, .6f);
            for (int i = 0; i < 30; i++)
            {
                stepped.Step(true, 1.5f, true, .02f);
            }

            Assert.That(single.Damage, Is.EqualTo(.5f).Within(.00001));
            Assert.That(stepped.Damage, Is.EqualTo(single.Damage).Within(.00001));
        }

        [Test]
        public void RecoveryIsGradualPausesUnderTheBeamAndNewContactSoundsAgain()
        {
            var eye = new StalkerEyeModel(Settings());
            int sounds = 0;
            eye.BurnStarted += () => sounds++;
            eye.Step(true, 1.5f, true, 1);
            eye.Step(false, 4, true, 2);
            Assert.That(eye.Damage, Is.EqualTo(.8f).Within(.00001));
            eye.Step(true, 2.8f, true, 3);
            Assert.That(eye.Damage, Is.EqualTo(.8f).Within(.00001));
            Assert.That(sounds, Is.EqualTo(1));
            eye.Step(true, 1.5f, true, .5f);
            Assert.That(sounds, Is.EqualTo(2));
            Assert.That(eye.Damage, Is.GreaterThan(.8f));
            eye.Step(false, 4, true, 11);
            Assert.That(eye.Damage, Is.Zero);
        }

        [TestCase("Stalker")]
        [TestCase("Sleeper")]
        public void WeightedCompositionsGiveThirtyFivePercentEyesAndReplaceOnlyWithSoftTissue(string species)
        {
            var profile = AssetDatabase.LoadAssetAtPath<GoreProfile>("Assets/EmergencyShift/Data/Gore/" + species + "Rupture.asset");
            GoreVariant[] active = Enumerable.Range(0, profile.VariantCount).Select(profile.VariantAt).Where(value => !value.RestorationOnly).ToArray();
            int totalWeight = active.Sum(value => value.SelectionWeight);
            int eyes = 0;
            for (int ticket = 0; ticket < totalWeight; ticket++)
            {
                GoreVariant selected = profile.ChooseVariant(new TicketRandom(ticket));
                eyes += Enumerable.Range(0, selected.PieceCount).Count(i => selected.PieceAt(i).EyeSurface);
            }

            Assert.That(eyes / (float)totalWeight, Is.EqualTo(.35f).Within(.00001));
            foreach (GoreVariant flesh in active.Where(value => value.Id.EndsWith("-flesh03", StringComparison.Ordinal)))
            {
                GoreVariant withEye = profile.FindVariant(flesh.Id.Replace("-flesh03", "-eye03"));
                int[] fleshIndices = Enumerable.Range(0, flesh.PieceCount).Select(i => flesh.PieceAt(i).SpriteIndex).ToArray();
                int[] eyeIndices = Enumerable.Range(0, withEye.PieceCount).Select(i => withEye.PieceAt(i).SpriteIndex).ToArray();
                CollectionAssert.AreEqual(eyeIndices.Where(index => index < 5), fleshIndices.Where(index => index < 5), "No extra limbs.");
                CollectionAssert.DoesNotContain(fleshIndices, 7, "The no-eye body cannot contain a painted-in eye.");
                CollectionAssert.DoesNotContain(fleshIndices, 9);
                Assert.That(fleshIndices.Count(index => index >= 10) - eyeIndices.Count(index => index >= 10), Is.InRange(2, 4));
            }
        }

        [Test]
        public void LegacyRemainsStaySupportedAndNewOcularPayloadIsVersionedAndValidated()
        {
            var saved = new RemainsSnapshot { parts = new[] { new RemainsPartState
                { body = new RemainsSpriteState { rotation = Quaternion.identity, scale = Vector3.one } } } };
            Assert.DoesNotThrow(() => RemainsSnapshotValidation.Validate(saved, 1, 0));
            saved.version = RemainsSnapshot.CurrentVersion;
            saved.parts[0].hasEyeSurface = true;
            saved.parts[0].eyeDamage = .6f;
            Assert.DoesNotThrow(() => RemainsSnapshotValidation.Validate(saved, 1, 0));
            saved.parts[0].eyeDamage = float.NaN;
            Assert.Throws<InvalidOperationException>(() => RemainsSnapshotValidation.Validate(saved, 1, 0));
            saved.parts[0].eyeDamage = .6f;
            saved.version = 0;
            Assert.Throws<InvalidOperationException>(() => RemainsSnapshotValidation.Validate(saved, 1, 0));
            saved.version = RemainsSnapshot.CurrentVersion + 1;
            Assert.Throws<InvalidOperationException>(() => RemainsSnapshotValidation.Validate(saved, 1, 0));
        }

        [Test]
        public void MissingEyeMaterialRejectsRestoreBeforeCreatingOrMovingRemains()
        {
            var owner = new GameObject("Ocular restore validation");
            try
            {
                var record = owner.AddComponent<RemainsRecord2D>();
                record.Configure(null, new Sprite[1], null, null, null, "test");
                owner.transform.position = new Vector3(3, 4, 0);
                var saved = new RemainsSnapshot { version = RemainsSnapshot.CurrentVersion,
                    parts = new[] { new RemainsPartState { hasEyeSurface = true, eyeDamage = .5f,
                        body = new RemainsSpriteState { rotation = Quaternion.identity, scale = Vector3.one } } } };
                Assert.Throws<InvalidOperationException>(() => record.Restore(saved));
                Assert.That(owner.transform.position, Is.EqualTo(new Vector3(3, 4, 0)));
                Assert.That(owner.transform.childCount, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        private sealed class TicketRandom : IRandomSource
        {
            private readonly int ticket;
            public TicketRandom(int ticket) => this.ticket = ticket;
            public int NextInt(int minimumInclusive, int maximumExclusive) => ticket;
            public double NextUnit() => 0;
        }
    }
}
