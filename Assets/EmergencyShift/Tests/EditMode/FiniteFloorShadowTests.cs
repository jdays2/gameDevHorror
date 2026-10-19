using System;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift.Tests
{
    public sealed class FiniteFloorShadowTests
    {
        private static readonly Vector2[] Box =
        {
            new Vector2(1, -.2f), new Vector2(1.4f, -.2f), new Vector2(1.4f, .2f), new Vector2(1, .2f)
        };

        private static byte Sample(FloorShadowMask mask, float x, float y)
        {
            int column = Mathf.Clamp((int)((x / 10 + .5f) * mask.Resolution), 0, mask.Resolution - 1);
            int row = Mathf.Clamp((int)((y / 10 + .5f) * mask.Resolution), 0, mask.Resolution - 1);
            return mask.Pixels[row * mask.Resolution + column].r;
        }

        [Test]
        public void ShortShadowEndsWhileTallShadowContinuesAndOverlapsCannotEraseIt()
        {
            var mask = new FloorShadowMask(128);
            mask.Clear();
            mask.Project(Box, Vector2.zero, Matrix4x4.identity, 5, .6f, .16f);
            Assert.That(Sample(mask, 1.65f, 0), Is.Zero);
            Assert.That(Sample(mask, 2.4f, 0), Is.EqualTo(255));
            mask.Project(Box, Vector2.zero, Matrix4x4.identity, 5, 2.8f, .16f);
            Assert.That(Sample(mask, 2.4f, 0), Is.Zero);
            mask.Project(Box, Vector2.zero, Matrix4x4.identity, 5, .6f, .16f);
            Assert.That(Sample(mask, 2.4f, 0), Is.Zero);
            Assert.That(Sample(mask, 4.6f, 0), Is.EqualTo(255));
            Assert.That(Sample(mask, 0, 1), Is.EqualTo(255));
        }

        [Test]
        public void ReversingTheOutlineKeepsTheSameProjection()
        {
            var first = new FloorShadowMask(128);
            var second = new FloorShadowMask(128);
            var reverse = (Vector2[])Box.Clone();
            Array.Reverse(reverse);
            first.Clear();
            second.Clear();
            first.Project(Box, Vector2.zero, Matrix4x4.identity, 5, .6f, .16f);
            second.Project(reverse, Vector2.zero, Matrix4x4.identity, 5, .6f, .16f);
            for (int i = 0; i < first.Pixels.Length; i++)
            {
                Assert.That(Mathf.Abs(first.Pixels[i].r - second.Pixels[i].r), Is.LessThanOrEqualTo(1));
            }
        }

        [Test]
        public void WallProjectionDoesNotRegainLightInsideTheLightRange()
        {
            var mask = new FloorShadowMask(128);
            mask.Clear();
            mask.Project(Box, Vector2.zero, Matrix4x4.identity, 5, 15, .001f);
            Assert.That(Sample(mask, 4.6f, 0), Is.Zero);
        }

        [Test]
        public void SourceSegmentStopsAtTheSilhouetteAndParallelAimStaysFree()
        {
            Assert.That(LightOriginPlacement2D.FirstIntersection(Vector2.zero, new Vector2(2, 0), Box), Is.EqualTo(.5f).Within(.0001f));
            Assert.That(LightOriginPlacement2D.FirstIntersection(new Vector2(0, 1), new Vector2(2, 1), Box), Is.EqualTo(1));
            Assert.That(LightOriginPlacement2D.Contains(new Vector2(1.2f, 0), Box), Is.True);
        }

        [Test]
        public void ProfileEvaluationDoesNotChangeSharedTuning()
        {
            var profile = ScriptableObject.CreateInstance<FloorShadowProfile>();
            try
            {
                string before = JsonUtility.ToJson(profile);
                Assert.That(profile.Length(EnvironmentHeightClass.Low), Is.LessThan(profile.Length(EnvironmentHeightClass.Tall)));
                Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(before));
                profile.lowLength = float.NaN;
                Assert.Throws<InvalidOperationException>(() => profile.Validate());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void SourceKeepsClearanceEvenWhenItsAuthoredPointIsJustOutsideTheSilhouette()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var go = new GameObject("Shape", typeof(SpriteRenderer), typeof(ShadowCaster2D));
            SceneManager.MoveGameObjectToScene(go, scene);
            try
            {
                var height = go.AddComponent<EnvironmentHeight2D>();
                height.SetBakedContours(new[]
                {
                    new FloorShadowContour2D { caster = go.GetComponent<ShadowCaster2D>(), frame = go.transform, points = Box }
                });
                Vector2 safe = LightOriginPlacement2D.Clamp(Vector2.zero, new Vector2(.98f, 0), scene, .04f, .45f, 0);
                Assert.That(safe.x, Is.EqualTo(.96f).Within(.001f));
                Assert.That(safe.y, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
