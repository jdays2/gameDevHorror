using System;
using EmergencyShift.Core.Composition;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class ShotVisualBoundaryTests
    {
        [TestCase("flashSeconds", float.NaN)]
        [TestCase("flashSeconds", 0)]
        [TestCase("flashRadius", float.PositiveInfinity)]
        [TestCase("flashRadius", 0)]
        [TestCase("flashIntensity", float.NaN)]
        [TestCase("flashIntensity", -1)]
        [TestCase("muzzleSize", float.NegativeInfinity)]
        [TestCase("muzzleSize", 0)]
        [TestCase("plumeSeconds", float.NaN)]
        [TestCase("plumeSeconds", 0)]
        [TestCase("plumeWidth", float.NaN)]
        [TestCase("plumeWidth", 0)]
        [TestCase("plumeLength", float.PositiveInfinity)]
        [TestCase("plumeLength", 0)]
        [TestCase("impactSeconds", float.NaN)]
        [TestCase("impactSeconds", 0)]
        public void InvalidShotScalarRejectsAtConfigurationBoundary(string field, float value)
        {
            var style = Object.Instantiate(PistolStyle());
            try
            {
                var data = new SerializedObject(style);
                data.FindProperty(field).floatValue = value;
                data.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<InvalidOperationException>(() => style.Validate());
            }
            finally
            {
                Object.DestroyImmediate(style);
            }
        }

        [Test]
        public void ShotValidationIncludesNestedProjectileWithoutMutatingSharedAsset()
        {
            var style = Object.Instantiate(PistolStyle());
            var projectile = Object.Instantiate(style.Projectile);
            try
            {
                var data = new SerializedObject(style);
                data.FindProperty("projectile").objectReferenceValue = projectile;
                data.ApplyModifiedPropertiesWithoutUndo();
                var projectileData = new SerializedObject(projectile);
                projectileData.FindProperty("bulletSpeed").floatValue = float.NaN;
                projectileData.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<InvalidOperationException>(() => style.Validate());
                Assert.DoesNotThrow(() => PistolStyle().Validate());
            }
            finally
            {
                Object.DestroyImmediate(style);
                Object.DestroyImmediate(projectile);
            }
        }

        [TestCase(typeof(ShotVisualProfile))]
        [TestCase(typeof(ProjectileVisualProfile))]
        public void ProfilesExposeOnlyReadonlyPropertiesAndPreserveAllAuthoredAssets(Type type)
        {
            Assert.That(type.GetFields(System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public), Is.Empty);
            foreach (var property in type.GetProperties(System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
            {
                Assert.That(property.GetSetMethod(), Is.Null, property.Name);
            }

            string[] ids = AssetDatabase.FindAssets("t:" + type.Name, new[] { "Assets/EmergencyShift" });
            Assert.That(ids, Is.Not.Empty);
            foreach (string id in ids)
            {
                Object profile = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(id), type);
                string before = EditorJsonUtility.ToJson(profile);
                type.GetMethod("Validate").Invoke(profile, null);
                Assert.That(EditorJsonUtility.ToJson(profile), Is.EqualTo(before));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InvalidShotConfigRejectsBeforeChangingNativeEffect(bool missingProjectile)
        {
            var style = Object.Instantiate(PistolStyle());
            var owner = new GameObject("Unconfigured effect");
            owner.SetActive(false);
            var light = owner.AddComponent<Light2D>();
            var effect = owner.AddComponent<ShotEffect2D>();
            effect.Construct(new SceneRegistry<ShotEffect2D>());
            effect.Configure(light, Array.Empty<SpriteRenderer>());
            var data = new SerializedObject(style);
            if (missingProjectile)
            {
                data.FindProperty("projectile").objectReferenceValue = null;
            }
            else
            {
                data.FindProperty("flashSeconds").floatValue = float.NaN;
            }

            data.ApplyModifiedPropertiesWithoutUndo();
            float intensity = light.intensity;
            Vector3 scale = owner.transform.localScale;
            try
            {
                Assert.Throws<InvalidOperationException>(() => effect.Play(style, false, Vector2.up));
                Assert.That(light.intensity, Is.EqualTo(intensity));
                Assert.That(owner.transform.localScale, Is.EqualTo(scale));
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(style);
            }
        }

        private static ShotVisualProfile PistolStyle() => AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/PistolDefinition.asset").Visuals;
    }
}
