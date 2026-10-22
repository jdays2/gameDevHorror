using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Tests
{
    public sealed class BloodPoolProfileValidationTests
    {
        private BloodPoolProfile profile;
        private Texture2D texture;
        private Sprite sprite;

        [SetUp]
        public void SetUp()
        {
            texture = new Texture2D(2, 2);
            sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * .5f);
            profile = ScriptableObject.CreateInstance<BloodPoolProfile>();
            Set(profile, "variants", new[] { new BloodPoolVariant("fixture", sprite) });
            Set(profile, "dropletSprites", new[] { sprite });
            Assert.That(profile.IsValid(), Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(profile);
            UnityEngine.Object.DestroyImmediate(sprite);
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static IEnumerable<TestCaseData> InvalidNumbers()
        {
            string[] fields =
            {
                "canvasSize", "growSeconds", "initialScale", "scaleRange.x", "scaleRange.y",
                "dropletDistance.x", "dropletDistance.y", "dropletSize.x", "dropletSize.y",
                "dropletAspect.x", "dropletAspect.y", "arrivalSeconds.x", "arrivalSeconds.y",
                "poolTint.r", "poolTint.g", "poolTint.b", "poolTint.a",
                "dropletTint.r", "dropletTint.g", "dropletTint.b", "dropletTint.a"
            };
            float[] numbers = { float.NaN, float.PositiveInfinity, float.NegativeInfinity };
            string[] names = { "NaN", "PositiveInfinity", "NegativeInfinity" };
            foreach (string field in fields)
            {
                for (int i = 0; i < numbers.Length; i++)
                {
                    yield return new TestCaseData(field, numbers[i]).SetName("Rejects_" + field.Replace('.', '_') + "_" + names[i]);
                }
            }
        }

        [TestCaseSource(nameof(InvalidNumbers))]
        public void NonFiniteProfileNumberIsRejected(string field, float value)
        {
            if (field == "canvasSize")
            {
                Set(profile.VariantAt(0), "canvasSize", value);
            }
            else if (!field.Contains('.'))
            {
                typeof(BloodPoolProfile).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(profile, value);
            }
            else
            {
                string[] parts = field.Split('.');
                FieldInfo member = typeof(BloodPoolProfile).GetField(parts[0], BindingFlags.Instance | BindingFlags.NonPublic);
                object vector = member.GetValue(profile);
                vector.GetType().GetField(parts[1]).SetValue(vector, value);
                member.SetValue(profile, vector);
            }

            Assert.That(profile.IsValid(), Is.False);
        }

        [Test]
        public void FiniteHdrTintIsKeptWithoutClampingTheAuthoredColor()
        {
            var tint = new Color(2, .2f, .3f, .7f);
            Set(profile, "poolTint", tint);
            Assert.That(profile.IsValid(), Is.True);
            Assert.That(profile.PoolTint, Is.EqualTo(tint));
        }

        [Test]
        public void InvalidProfileIsRejectedBeforeChangingAnyRenderer()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Blood validation fixture");
                SceneManager.MoveGameObjectToScene(root, scene);
                var pool = root.AddComponent<BloodPool2D>();
                var image = new GameObject("Untouched renderer", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                image.transform.SetParent(root.transform, false);
                image.sprite = sprite;
                image.color = Color.magenta;
                image.transform.localScale = new Vector3(1, 2, 1);
                image.transform.localPosition = new Vector3(.4f, .3f, 0);
                Set(pool, "profile", profile);
                Set(pool, "patches", new[] { image });
                Set(pool, "droplets", Array.Empty<SpriteRenderer>());
                Set(profile, "growSeconds", float.PositiveInfinity);
                Assert.Throws<InvalidOperationException>(() => pool.Initialize(1, 45, 0));
                Assert.That(image.color, Is.EqualTo(Color.magenta));
                Assert.That(image.transform.localScale, Is.EqualTo(new Vector3(1, 2, 1)));
                Assert.That(image.transform.localPosition, Is.EqualTo(new Vector3(.4f, .3f, 0)));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
