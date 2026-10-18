using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift.Tests
{
    public sealed class EnvironmentHeightShadowTests
    {
        private static int[] Targets(ShadowCaster2D caster)
        {
            var layers = new SerializedObject(caster).FindProperty("m_ApplyToSortingLayers");
            return Enumerable.Range(0, layers.arraySize).Select(i => layers.GetArrayElementAtIndex(i).intValue).ToArray();
        }

        [Test]
        public void BinShadowsFloorAndLowSurfacesButCannotCoverCabinet()
        {
            var bin = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Environment/InteriorProps01/waste-bin.prefab");
            var cabinet = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Environment/InteriorProps01/personnel-locker.prefab");
            int[] targets = Targets(bin.GetComponent<ShadowCaster2D>());
            Assert.That(bin.GetComponent<EnvironmentHeight2D>().heightClass, Is.EqualTo(EnvironmentHeightClass.Low));
            Assert.That(cabinet.GetComponent<EnvironmentHeight2D>().heightClass, Is.EqualTo(EnvironmentHeightClass.Tall));
            Assert.That(targets, Has.Member(SortingLayer.NameToID(EnvironmentShadowLayers.Floor)));
            Assert.That(targets, Has.Member(bin.GetComponent<SpriteRenderer>().sortingLayerID));
            Assert.That(targets, Has.No.Member(cabinet.GetComponent<SpriteRenderer>().sortingLayerID));
            Assert.That(targets, Has.No.Member(SortingLayer.NameToID(EnvironmentShadowLayers.Furniture)));
        }

        [Test]
        public void TallWallsStillShadowEverySurfaceAndCorePrefabLightsReachThem()
        {
            var wall = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/World/Wall.prefab");
            int[] layers = SortingLayer.layers.Select(l => l.id).ToArray();
            Assert.That(Targets(wall.GetComponent<ShadowCaster2D>()), Is.EquivalentTo(layers));
            foreach (string path in new[]
            {
                "Assets/EmergencyShift/Prefabs/Player/Player.prefab",
                "Assets/EmergencyShift/Prefabs/Levels/LevelRig.prefab",
                "Assets/EmergencyShift/Prefabs/Environment/HospitalLamp01.prefab",
                "Assets/EmergencyShift/Prefabs/World/EmergencyLamp.prefab",
                "Assets/EmergencyShift/Prefabs/Combat/PistolShotEffect.prefab"
            })
            {
                foreach (var light in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Light2D>(true))
                {
                    Assert.That(light.targetSortingLayers, Is.SupersetOf(layers), path + " / " + light.name);
                }
            }
        }

        [Test]
        public void DeadVisualMovesBelowFurnitureWithoutChangingLiveActorLayer()
        {
            var go = new GameObject("Visual", typeof(SortingGroup));
            try
            {
                var renderer = new GameObject("Body", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                renderer.transform.SetParent(go.transform);
                Assert.That(go.GetComponent<SortingGroup>().sortingLayerID, Is.Zero);
                RemainsVisualStyle.Apply(go.transform);
                Assert.That(go.GetComponent<SortingGroup>().sortingLayerName, Is.EqualTo(EnvironmentShadowLayers.Floor));
                Assert.That(renderer.sortingLayerName, Is.EqualTo(EnvironmentShadowLayers.Floor));
                Assert.That(SortingLayer.GetLayerValueFromName(EnvironmentShadowLayers.Floor), Is.LessThan(SortingLayer.GetLayerValueFromName(EnvironmentShadowLayers.Low)));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
