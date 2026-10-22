using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class GoreCompositionTests
    {
        [TestCase("Stalker")]
        [TestCase("Sleeper")]
        public void NewDeathsChooseAnatomicalBodiesWithAtMostSixLegsAndMoreFlesh(string species)
        {
            var profile = AssetDatabase.LoadAssetAtPath<GoreProfile>("Assets/EmergencyShift/Data/Gore/" + species + "Rupture.asset");
            string before = EditorJsonUtility.ToJson(profile);
            for (int i = 0; i < 96; i++)
            {
                GoreVariant variant = profile.ChooseVariant(new Core.Randomness.SystemRandomSource(new System.Random(i + 15)));
                Assert.That(variant.RestorationOnly, Is.False);
                Assert.That(variant.ReplaceOriginalBody, Is.True);
                int legs = 0;
                int flesh = 0;
                var usedLegs = new System.Collections.Generic.HashSet<int>();
                for (int pieceIndex = 0; pieceIndex < variant.PieceCount; pieceIndex++)
                {
                    GorePiece piece = variant.PieceAt(pieceIndex);
                    if (piece.SpriteIndex < 5)
                    {
                        legs++;
                        Assert.That(usedLegs.Add(piece.SpriteIndex), Is.True, "Repeated anatomical leg");
                    }
                    if (piece.SpriteIndex >= 10)
                    {
                        flesh++;
                    }
                }
                Assert.That(legs, Is.LessThanOrEqualTo(6));
                Assert.That(flesh, Is.InRange(8, 10));
            }
            Assert.That(EditorJsonUtility.ToJson(profile), Is.EqualTo(before));
            if (species == "Stalker")
            {
                Assert.That(profile.FindVariant("radial").RestorationOnly, Is.True);
            }
        }

        [Test]
        public void AProfileWithoutNewDeathCompositionsFailsInsteadOfSpawningExtraLegs()
        {
            var profile = ScriptableObject.CreateInstance<GoreProfile>();
            try
            {
                typeof(GoreProfile).GetField("variants", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(profile, new[] { new GoreVariant("old", true) });
                Assert.Throws<InvalidOperationException>(() => profile.ChooseVariant(new Core.Randomness.SystemRandomSource(new System.Random(15))));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }
    }
}
