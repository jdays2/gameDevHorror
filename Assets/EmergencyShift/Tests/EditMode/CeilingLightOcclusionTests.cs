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
                ownWall.AddComponent<FullHeightLightBlocker2D>();
                var otherWall = new GameObject("Other scene wall");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(otherWall, second);
                otherWall.AddComponent<FullHeightLightBlocker2D>();
                var found = new System.Collections.Generic.List<BoxCollider2D>();
                FullHeightLightBlocker2D.CollectFor(owner.transform, found);
                Assert.That(found, Has.Count.EqualTo(1));
                Assert.That(found[0].gameObject, Is.EqualTo(ownWall));
                int version = FullHeightLightBlocker2D.RegistryVersion;
                ownWall.SetActive(false);
                found.Clear();
                FullHeightLightBlocker2D.CollectFor(owner.transform, found);
                Assert.That(found, Is.Empty);
                Assert.That(FullHeightLightBlocker2D.RegistryVersion, Is.GreaterThan(version));
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
