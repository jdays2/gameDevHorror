using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift.Tests
{
    public sealed class LocalLampAssetTests
    {
        [TestCase("Assets/EmergencyShift/Prefabs/Environment/HospitalLamp01.prefab", true)]
        [TestCase("Assets/EmergencyShift/Prefabs/Environment/HospitalLamp01.prefab", false)]
        [TestCase("Assets/EmergencyShift/Prefabs/World/MorgueCeilingLamp.prefab", true)]
        [TestCase("Assets/EmergencyShift/Prefabs/World/MorgueCeilingLamp.prefab", false)]
        public void PersistentPrefabCallbacksDoNotChangeSharedView(string sourcePath, bool disabling)
        {
            string temporaryPath = "Assets/__EmergencyShiftLampAssetTests-" + Guid.NewGuid().ToString("N") + ".prefab";
            Assert.That(AssetDatabase.AssetPathToGUID(temporaryPath), Is.Empty, "Never overwrite an existing test/user asset.");
            Assert.That(AssetDatabase.CopyAsset(sourcePath, temporaryPath), Is.True);
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(temporaryPath);
                var lamp = prefab.GetComponent<LocalLamp2D>();
                Assert.That(EditorUtility.IsPersistent(lamp), Is.True);
                Assert.That(lamp.gameObject.scene.IsValid(), Is.False);
                using (var serialized = new SerializedObject(lamp))
                {
                    var source = (Light2D)serialized.FindProperty("source").objectReferenceValue;
                    var bulb = (SpriteRenderer)serialized.FindProperty("bulb").objectReferenceValue;
                    source.enabled = true;
                    source.intensity = 2.25f;
                    bulb.enabled = true;
                    if (disabling)
                    {
                        typeof(LocalLamp2D).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(lamp, null);
                    }
                    else
                    {
                        lamp.RefreshAppearance();
                    }
                    Assert.That(source.enabled, Is.True, "An asset has no live scene light to clean up.");
                    Assert.That(source.intensity, Is.EqualTo(2.25f), "Shared native data must remain unchanged.");
                    Assert.That(bulb.enabled, Is.True);
                }
            }
            finally
            {
                AssetDatabase.DeleteAsset(temporaryPath);
            }
        }
    }
}
