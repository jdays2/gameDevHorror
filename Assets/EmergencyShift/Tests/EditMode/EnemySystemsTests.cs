using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace EmergencyShift.Tests
{
    public sealed class EnemySystemsTests
    {
        [Test]
        public void OverkillAndRepeatedKillEmitOnlyOneDeath()
        {
            var obj = new GameObject("Health test");
            try
            {
                var health = obj.AddComponent<EnemyHealth2D>();
                health.Initialize(2);
                int deaths = 0;
                health.Died += () => deaths++;
                Assert.That(health.TakeDamage(99), Is.True);
                health.Kill();
                Assert.That(health.TakeDamage(1), Is.False);
                Assert.That(health.Current, Is.Zero);
                Assert.That(deaths, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(obj);
            }
        }

        [Test]
        public void SharedDefinitionDoesNotShareCurrentHealth()
        {
            var config = ScriptableObject.CreateInstance<EnemyDefinition>();
            var first = new GameObject("First");
            var second = new GameObject("Second");
            try
            {
                config.maximumHealth = 3;
                var a = first.AddComponent<EnemyHealth2D>();
                var b = second.AddComponent<EnemyHealth2D>();
                a.Initialize(config.maximumHealth);
                b.Initialize(config.maximumHealth);
                a.TakeDamage(1);
                Assert.That(a.Current, Is.EqualTo(2));
                Assert.That(b.Current, Is.EqualTo(3));
                Assert.That(config.maximumHealth, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void PerceptionDoesNotSeeTargetThroughBlockingGeometry()
        {
            var observer = new GameObject("Observer");
            var target = new GameObject("Target");
            var wall = new GameObject("Wall");
            try
            {
                target.transform.position = new Vector3(4, 0, 0);
                wall.layer = 8;
                wall.transform.position = new Vector3(2, 0, 0);
                wall.AddComponent<BoxCollider2D>().size = new Vector2(.2f, 2);
                var senses = observer.AddComponent<EnemyPerception2D>();
                senses.Configure(target.transform, 1 << 8, null);
                Physics2D.SyncTransforms();
                Assert.That(senses.Within(Vector2.zero, 5), Is.True);
                Assert.That(senses.CanSeeTarget(Vector2.zero), Is.False);
                wall.transform.position = new Vector3(2, 3, 0);
                Physics2D.SyncTransforms();
                Assert.That(senses.CanSeeTarget(Vector2.zero), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(observer);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(wall);
            }
        }

        [Test]
        public void PatrolAreaTooSmallForBodyDoesNotMove()
        {
            var obj = new GameObject("Route");
            var config = ScriptableObject.CreateInstance<EnemyPatrolProfile>();
            try
            {
                var patrol = obj.AddComponent<EnemyPatrol2D>();
                var fields = new SerializedObject(patrol);
                fields.FindProperty("profile").objectReferenceValue = config;
                fields.FindProperty("patrolEnabled").boolValue = true;
                fields.FindProperty("areaSize").vector2Value = new Vector2(.2f, .2f);
                fields.ApplyModifiedPropertiesWithoutUndo();
                patrol.Initialize(Vector2.zero);
                Assert.That(patrol.Step(Vector2.zero, .3f, 1 << 8, .02f), Is.EqualTo(Vector2.zero));
            }
            finally
            {
                Object.DestroyImmediate(obj);
                Object.DestroyImmediate(config);
            }
        }
    }
}
