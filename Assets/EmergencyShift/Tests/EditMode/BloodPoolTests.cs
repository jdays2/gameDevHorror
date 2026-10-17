using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class BloodPoolTests
    {
        private const string PrefabPath = "Assets/EmergencyShift/Prefabs/Enemies/BloodPool.prefab";
        private readonly List<GameObject> temporary = new List<GameObject>();

        private GameObject Own(GameObject instance)
        {
            temporary.Add(instance);
            return instance;
        }

        private BloodPool2D Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<BloodPool2D>();

        [TearDown]
        public void Cleanup()
        {
            foreach (GameObject instance in temporary)
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }
            temporary.Clear();
        }

        [TestCase("fresh-01")]
        [TestCase("ejected-01")]
        [TestCase("clotted-01")]
        public void JsonRoundTripRetainsTheChosenShapeAndEverySettledTransform(string variantId)
        {
            var first = Own(Object.Instantiate(Prefab.gameObject)).GetComponent<BloodPool2D>();
            first.Initialize(1.35f, 413, 0);
            first.RestoreVariant(variantId);
            var record = Own(new GameObject("Blood test record")).AddComponent<RemainsRecord2D>();
            record.Configure(Prefab, new Sprite[0], null, null, null, "test");
            record.RegisterPool(first);
            RemainsSnapshot saved = JsonUtility.FromJson<RemainsSnapshot>(JsonUtility.ToJson(record.Capture()));

            var restored = Own(new GameObject("Restored blood test record")).AddComponent<RemainsRecord2D>();
            restored.Configure(Prefab, new Sprite[0], null, null, null, "test");
            restored.Restore(saved);
            var second = restored.GetComponentInChildren<BloodPool2D>();
            Assert.That(second.VariantId, Is.EqualTo(variantId));
            Assert.That(second.enabled, Is.False);
            var a = first.GetComponentsInChildren<SpriteRenderer>(true);
            var b = second.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.That(b.Length, Is.EqualTo(a.Length));
            for (int i = 0; i < a.Length; i++)
            {
                Assert.That(b[i].sprite, Is.SameAs(a[i].sprite));
                Assert.That(b[i].enabled, Is.EqualTo(a[i].enabled));
                Assert.That(b[i].color, Is.EqualTo(a[i].color));
                Assert.That(b[i].transform.localPosition, Is.EqualTo(a[i].transform.localPosition));
                Assert.That(b[i].transform.localScale, Is.EqualTo(a[i].transform.localScale));
                Assert.That(b[i].transform.localRotation, Is.EqualTo(a[i].transform.localRotation));
            }
        }

        [Test]
        public void IndependentDeathsWithTheSameSeedDoNotMutateTheSharedProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<BloodPoolProfile>("Assets/EmergencyShift/Data/Blood/ProceduralPools01.asset");
            string before = EditorJsonUtility.ToJson(profile);
            var first = Own(Object.Instantiate(Prefab.gameObject)).GetComponent<BloodPool2D>();
            var second = Own(Object.Instantiate(Prefab.gameObject)).GetComponent<BloodPool2D>();
            first.Initialize(1.35f, 47, 0);
            second.Initialize(1.35f, 47, 0);
            Assert.That(second.VariantId, Is.EqualTo(first.VariantId));
            var a = first.GetComponentsInChildren<SpriteRenderer>(true);
            var b = second.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < a.Length; i++)
            {
                Assert.That(b[i].transform.localPosition, Is.EqualTo(a[i].transform.localPosition));
                Assert.That(b[i].transform.localScale, Is.EqualTo(a[i].transform.localScale));
                Assert.That(b[i].transform.localRotation, Is.EqualTo(a[i].transform.localRotation));
            }
            string alternate = second.VariantId == "clotted-01" ? "fresh-01" : "clotted-01";
            first.RestoreVariant(alternate);
            Assert.That(second.VariantId, Is.Not.EqualTo(first.VariantId));
            Assert.That(EditorJsonUtility.ToJson(profile), Is.EqualTo(before));
        }

        [Test]
        public void OldSnapshotsKeepOriginalBloodSpritesAndUnknownNewVariantsFailExplicitly()
        {
            var first = Own(Object.Instantiate(Prefab.gameObject)).GetComponent<BloodPool2D>();
            var source = first.GetComponentsInChildren<SpriteRenderer>(true);
            var states = new RemainsSpriteState[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                states[i] = new RemainsSpriteState();
                states[i].Capture(source[i]);
            }
            var old = new RemainsSnapshot { hasPool = true, pool = states, parts = new RemainsPartState[0] };
            var record = Own(new GameObject("Legacy blood test")).AddComponent<RemainsRecord2D>();
            record.Configure(Prefab, new Sprite[0], null, null, null, "old");
            record.Restore(old);
            var restored = record.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < source.Length; i++)
            {
                Assert.That(restored[i].sprite, Is.SameAs(source[i].sprite));
            }
            Assert.That(first.SupportsVariant("missing-content"), Is.False);
            Assert.Throws<System.InvalidOperationException>(() => first.RestoreVariant("missing-content"));
        }
    }
}
