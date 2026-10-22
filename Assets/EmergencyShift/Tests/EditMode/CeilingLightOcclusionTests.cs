using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class CeilingLightOcclusionTests
    {
        [Test]
        public void AutomaticBlockersStayInTheirSceneAndUnregisterWhenDisabled()
        {
            var first = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var second = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try
            {
                var owner = new GameObject("Lamp owner");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, first);
                var ownWall = new GameObject("Own full-height wall");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ownWall, first);
                var ownMarker = ownWall.AddComponent<FullHeightLightBlocker2D>();
                var otherWall = new GameObject("Other scene wall");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(otherWall, second);
                var otherMarker = otherWall.AddComponent<FullHeightLightBlocker2D>();
                using var registry = new Core.Composition.SceneRegistry<FullHeightLightBlocker2D>();
                ownMarker.Construct(registry);
                otherMarker.Construct(registry);
                var mask = owner.AddComponent<CeilingLightOcclusion2D>();
                mask.Construct(registry);
                mask.SetCeilingLighting(true);
                Assert.That(mask.BlockerCount, Is.EqualTo(1));
                ownWall.SetActive(false);
                mask.SetCeilingLighting(true);
                Assert.That(mask.BlockerCount, Is.Zero);
                Assert.That(registry.Members, Has.Count.EqualTo(1));
                ownWall.SetActive(true);
                mask.SetCeilingLighting(true);
                Assert.That(mask.BlockerCount, Is.EqualTo(1));
                ownWall.GetComponent<BoxCollider2D>().enabled = false;
                Assert.That(CeilingLightOcclusion2D.VisibleDistance(Vector2.zero, Vector2.right, 5,
                    new[] { ownMarker.Shape }), Is.EqualTo(5));
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(second);
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(first);
            }
        }

        [Test]
        public void CeilingLightIgnoresLowDeskButStopsAtWallAndMovingDoor()
        {
            var wall = new GameObject("Full height wall");
            var desk = new GameObject("Low desk");
            try
            {
                var blocker = wall.AddComponent<BoxCollider2D>();
                blocker.size = new Vector2(.3f, 4);
                wall.transform.position = new Vector3(3, 0, 0);
                desk.AddComponent<BoxCollider2D>().size = Vector2.one;
                desk.transform.position = new Vector3(1, 0, 0);
                var blockers = new[] { blocker };
                Assert.That(CeilingLightOcclusion2D.VisibleDistance(Vector2.zero, Vector2.right, 5, blockers), Is.EqualTo(2.85f).Within(.001f));
                wall.transform.position = new Vector3(3, 4, 0);
                Assert.That(CeilingLightOcclusion2D.VisibleDistance(Vector2.zero, Vector2.right, 5, blockers), Is.EqualTo(5));
                wall.transform.position = Vector3.zero;
                Assert.That(CeilingLightOcclusion2D.VisibleDistance(Vector2.zero, Vector2.right, 5, blockers), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(wall);
                Object.DestroyImmediate(desk);
            }
        }

        [Test]
        public void PreviewWithoutCompositionFindsNewMarkersAndReleasesDisabledOnes()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try
            {
                var owner = new GameObject("Standalone preview lamp");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, scene);
                var mask = owner.AddComponent<CeilingLightOcclusion2D>();
                mask.SetCeilingLighting(true);
                Assert.That(mask.BlockerCount, Is.Zero);
                var wall = new GameObject("New preview wall");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wall, scene);
                var marker = wall.AddComponent<FullHeightLightBlocker2D>();
                mask.SetCeilingLighting(true);
                Assert.That(mask.BlockerCount, Is.EqualTo(1));
                marker.enabled = false;
                mask.SetCeilingLighting(true);
                Assert.That(mask.BlockerCount, Is.Zero);
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void RotatedScaledAndDisabledBlockersUseWorldDistance()
        {
            var wall = new GameObject("Rotated full height wall");
            try
            {
                var blocker = wall.AddComponent<BoxCollider2D>();
                blocker.size = new Vector2(4, .3f);
                wall.transform.SetPositionAndRotation(new Vector3(3, 0, 0), Quaternion.Euler(0, 0, 90));
                wall.transform.localScale = Vector3.one * 2;
                Assert.That(CeilingLightOcclusion2D.VisibleDistance(Vector2.zero, Vector2.right, 8, new[] { blocker }), Is.EqualTo(2.7f).Within(.001f));
                blocker.enabled = false;
                Assert.That(CeilingLightOcclusion2D.VisibleDistance(Vector2.zero, Vector2.right, 8, new[] { blocker }), Is.EqualTo(8));
            }
            finally
            {
                Object.DestroyImmediate(wall);
            }
        }
    }
}
