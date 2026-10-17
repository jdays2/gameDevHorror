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
                GoreVariant variant = profile.ChooseVariant();
                Assert.That(variant.restorationOnly, Is.False);
                Assert.That(variant.replaceOriginalBody, Is.True);
                int legs = 0;
                int flesh = 0;
                var usedLegs = new System.Collections.Generic.HashSet<int>();
                foreach (GorePiece piece in variant.pieces)
                {
                    if (piece.spriteIndex < 5)
                    {
                        legs++;
                        Assert.That(usedLegs.Add(piece.spriteIndex), Is.True, "Repeated anatomical leg");
                    }
                    if (piece.spriteIndex >= 10)
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
                Assert.That(Array.Find(profile.variants, variant => variant.id == "radial").restorationOnly, Is.True);
            }
        }

        [Test]
        public void AProfileWithoutNewDeathCompositionsFailsInsteadOfSpawningExtraLegs()
        {
            var profile = ScriptableObject.CreateInstance<GoreProfile>();
            try
            {
                profile.variants = new[] { new GoreVariant { id = "old", restorationOnly = true } };
                Assert.Throws<InvalidOperationException>(() => profile.ChooseVariant());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }
    }
}
