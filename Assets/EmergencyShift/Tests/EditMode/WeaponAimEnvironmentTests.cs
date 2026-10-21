using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Tests
{
    public sealed class WeaponAimEnvironmentTests
    {
        private Scene scene;
        private WeaponAimRig2D rig;
        private TestSource source;
        private WeaponAimEnvironment2D environment;

        [SetUp]
        public void SetUp()
        {
            scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("Aim environment fixture");
            SceneManager.MoveGameObjectToScene(root, scene);
            var pivot = new GameObject("Pivot").transform;
            pivot.SetParent(root.transform, false);
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(pivot, false);
            rig = new WeaponAimRig2D(root.transform, pivot, muzzle);
            source = new TestSource();
            environment = new WeaponAimEnvironment2D(rig, source, 1 << 8);
        }

        [TearDown]
        public void TearDown() => EditorSceneManager.ClosePreviewScene(scene);

        [TestCase(1f, 3f, .15f)]
        [TestCase(-3f, 1f, -.2f)]
        [TestCase(2f, -3f, .15f)]
        [TestCase(-2f, -3f, 0f)]
        public void RotatedMuzzleRayPassesThroughTargetWithLateralOffset(float x, float y, float lateral)
        {
            rig.Muzzle.localPosition = new Vector3(lateral, .5f, 0);
            source.Target = new Vector2(x, y);
            environment.UpdateRotation();
            Vector2 ray = source.Target - (Vector2)rig.Muzzle.position;
            Vector3 cross = Vector3.Cross(ray.normalized, rig.Pivot.up);
            Assert.That(Mathf.Abs(cross.z), Is.LessThan(.0001f));
            Assert.That(Vector2.Dot(ray, rig.Pivot.up), Is.GreaterThan(0));
            Assert.That(environment.HasForwardAim, Is.True);
        }

        [Test]
        public void TargetUnderMuzzleCannotFireBackwardsIntoTheOwner()
        {
            rig.Muzzle.localPosition = new Vector3(0, .4f, 0);
            source.Target = new Vector2(0, .2f);
            environment.UpdateRotation();
            Assert.That(environment.HasForwardAim, Is.False);
            Assert.That(environment.TryBuildShot(10, out _, out _), Is.False);
        }

        [Test]
        public void LegacyAimUsesFlashlightDirectionWithoutAnInputDevice()
        {
            source.UsesTarget = false;
            source.FallbackDirection = Vector2.right;
            environment.UpdateRotation();
            Assert.That(Vector2.Distance(rig.Pivot.up, Vector2.right), Is.LessThan(.0001f));
        }

        private sealed class TestSource : IWeaponAimSource2D
        {
            public bool UsesTarget { get; set; } = true;
            public bool HasTarget => true;
            public Vector2 Target { get; set; }
            public Vector2 FallbackDirection { get; set; } = Vector2.up;
        }
    }
}
