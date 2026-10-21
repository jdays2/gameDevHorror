using EmergencyShift.Core.Composition;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class WorldProjectionTests
    {
        [TestCase(1, 0)]
        [TestCase(-1, 0)]
        [TestCase(0, 1)]
        [TestCase(0, -1)]
        public void OrthographicProjectionUsesCameraCoordinatesWithoutAFlashlight(float x, float y)
        {
            var owner = new GameObject("Projection only");
            try
            {
                var camera = owner.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5;
                camera.aspect = 1;
                camera.transform.position = Vector3.back * 10;
                var projection = new CameraWorldProjection2D(camera);
                Assert.That(projection.TryProjectViewportOffset(new Vector2(3, 7), new Vector2(x, y), out Vector2 offset), Is.True);
                Assert.That(offset.x, Is.EqualTo(x * .1f).Within(.0001));
                Assert.That(offset.y, Is.EqualTo(y * .1f).Within(.0001));
                camera.transform.rotation = Quaternion.Euler(0, 0, 90);
                Assert.That(projection.TryProjectViewportOffset(Vector2.zero, new Vector2(x, y), out offset), Is.True);
                Assert.That(offset.x, Is.EqualTo(y * .1f).Within(.0001));
                Assert.That(offset.y, Is.EqualTo(-x * .1f).Within(.0001));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void MissingCameraAndInvalidGeometryCannotProduceHudOffsets()
        {
            var projection = new CameraWorldProjection2D(null);
            Assert.That(projection.TryProjectViewportOffset(Vector2.zero, Vector2.one, out _), Is.False);
            var owner = new GameObject("Invalid projection");
            try
            {
                projection = new CameraWorldProjection2D(owner.AddComponent<Camera>());
                Assert.That(projection.TryProjectViewportOffset(Vector2.zero, new Vector2(float.NaN, 0), out _), Is.False);
                Assert.That(projection.TryProjectViewportOffset(Vector2.one * float.MaxValue, Vector2.one * float.MaxValue, out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }
    }
}
