using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class EnemyMotorGeometryTests
    {
        [TestCase(2, .9f, false)]
        [TestCase(.5f, .6f, true)]
        [TestCase(1, .9f, true)]
        public void SteeringUsesScaledBodyRatherThanUnscaledColliderRadius(float scale, float wallX, bool straight)
        {
            var actor = new GameObject("Scaled motor fixture", typeof(Rigidbody2D), typeof(CircleCollider2D));
            var wall = new GameObject("Steering obstruction");
            try
            {
                Vector2 origin = new Vector2(200, 200);
                actor.transform.position = origin;
                actor.transform.localScale = new Vector3(scale, scale, 1);
                actor.GetComponent<Rigidbody2D>().gravityScale = 0;
                var shape = actor.GetComponent<CircleCollider2D>();
                shape.radius = .4f;
                var motor = actor.AddComponent<EnemyMotor2D>();
                typeof(EnemyMotor2D).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(motor, null);
                motor.Configure(1 << 8);
                wall.layer = 8;
                wall.transform.position = origin + Vector2.right * wallX;
                wall.AddComponent<BoxCollider2D>().size = new Vector2(.1f, 4);
                Physics2D.SyncTransforms();
                Assert.That(motor.Radius, Is.EqualTo(shape.bounds.extents.x).Within(.0001));
                Assert.That(Physics2D.Distance(shape, wall.GetComponent<Collider2D>()).distance, Is.GreaterThan(0));
                Vector2 chosen = motor.ClearDirection(Vector2.right);
                Assert.That(chosen == Vector2.right, Is.EqualTo(straight), $"scale={scale}, chosen={chosen}");
            }
            finally
            {
                Object.DestroyImmediate(actor);
                Object.DestroyImmediate(wall);
            }
        }
    }
}
