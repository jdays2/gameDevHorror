using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class CeilingLightOcclusionTests
    {
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
