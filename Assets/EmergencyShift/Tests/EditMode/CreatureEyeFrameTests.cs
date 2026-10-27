using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class CreatureEyeFrameTests
    {
        [TestCase(.165f, 0, true)]
        [TestCase(.166f, 0, true)]
        [TestCase(.1665f, 0, true)]
        [TestCase(.16666663f, 0, true)]
        [TestCase(.167f, 1, false)]
        [TestCase(.5f, 2, false)]
        [TestCase(1f, 3, false)]
        public void PupilAndLidSelectTheSameOpenFrame(float closure, int expectedEyeFrame, bool expectedPupil)
        {
            var fixture = new GameObject("Eye frame threshold regression");
            var texture = new Texture2D(4, 1);
            var frames = new Sprite[4];
            try
            {
                for (int index = 0; index < frames.Length; index++)
                {
                    frames[index] = Sprite.Create(texture, new Rect(index, 0, 1, 1), Vector2.one * .5f);
                }
                var source = fixture.AddComponent<EyeFrameTestSource>();
                var body = fixture.AddComponent<SpriteRenderer>();
                var irisObject = new GameObject("Iris");
                irisObject.transform.SetParent(fixture.transform);
                var iris = irisObject.AddComponent<SpriteRenderer>();
                var pupilObject = new GameObject("Pupil");
                pupilObject.transform.SetParent(fixture.transform);
                var pupil = pupilObject.AddComponent<SpriteRenderer>();
                var animator = fixture.AddComponent<CreatureSpriteAnimator2D>();
                animator.Configure(source, fixture.transform, body, iris, pupil, new[] { frames[0] }, frames);
                typeof(CreatureSpriteAnimator2D).GetField("eyeFrames", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(animator, frames);
                MethodInfo tick = typeof(CreatureSpriteAnimator2D).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
                source.EyeClosure = closure;
                tick.Invoke(animator, null);
                Assert.That(iris.sprite, Is.SameAs(frames[expectedEyeFrame]));
                Assert.That(pupil.enabled, Is.EqualTo(expectedPupil), "An open white eye must include its pupil.");

                source.Dead = true;
                tick.Invoke(animator, null);
                Assert.That(iris.enabled, Is.False);
                Assert.That(pupil.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(fixture);
                foreach (Sprite frame in frames)
                {
                    Object.DestroyImmediate(frame);
                }
                Object.DestroyImmediate(texture);
            }
        }
    }

    public sealed class EyeFrameTestSource : EnemyPresentationSource2D
    {
        public float EyeClosure;
        public bool Dead;
        public override float Closure => EyeClosure;
        public override bool IsDead => Dead;
        public override float AttackProgress => 0;
        public override float DeathTime => 0;
        public override IThreatLight2D LightSource => null;
        public override LayerMask BlockingGeometry => 0;
    }
}
