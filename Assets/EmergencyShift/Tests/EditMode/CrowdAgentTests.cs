using System.Collections.Generic;
using System.Reflection;
using EmergencyShift.Core.Composition;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Tests
{
    public sealed class CrowdAgentTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private SceneRegistry<CrowdAgent2D> registry;
        private Scene previewScene;
        private float previousFixedDelta;

        [SetUp]
        public void CreateIsolatedCrowd()
        {
            registry = new SceneRegistry<CrowdAgent2D>();
            previewScene = EditorSceneManager.NewPreviewScene();
            previousFixedDelta = Time.fixedDeltaTime;
            Time.fixedDeltaTime = .02f;
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (GameObject item in objects)
            {
                Object.DestroyImmediate(item);
            }
            objects.Clear();
            registry.Dispose();
            EditorSceneManager.ClosePreviewScene(previewScene);
            Time.fixedDeltaTime = previousFixedDelta;
        }

        [Test]
        public void DefaultCrowdDoesNotApplyTheNewForwardYield()
        {
            CrowdAgent2D follower = Agent(Vector2.zero, false);
            Agent(Vector2.up * .7f);
            Vector2 desired = Vector2.up * 2.4f;
            Assert.That(follower.LimitForwardVelocity(desired, 0), Is.EqualTo(desired));
        }

        [Test]
        public void DefaultPhysicalSpacingDoesNotRepelADistantNeighbour()
        {
            CrowdAgent2D follower = Agent(Vector2.zero, false, 0);
            Agent(Vector2.up * 1.2f, false, 0);
            Vector2 desired = Vector2.up * 2.4f;
            Assert.That(follower.Adjust(desired, 0), Is.EqualTo(desired));
        }

        [Test]
        public void StationaryNeighbourStopsForwardMotionAtSilhouetteSpacing()
        {
            CrowdAgent2D follower = Agent(Vector2.zero);
            Agent(Vector2.up * 1.3f);
            Assert.That(follower.LimitForwardVelocity(Vector2.up * 2.4f, 0), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void MovingNeighbourAllowsFollowingWithoutASidewaysComponent()
        {
            CrowdAgent2D follower = Agent(Vector2.zero);
            CrowdAgent2D leader = Agent(Vector2.up * 1.3f);
            leader.GetComponent<Rigidbody2D>().linearVelocity = Vector2.up * 1.2f;
            Vector2 limited = follower.LimitForwardVelocity(Vector2.up * 2.4f, 0);
            Assert.That(limited.x, Is.Zero);
            Assert.That(limited.y, Is.EqualTo(1.2f).Within(.001f));
        }

        [Test]
        public void OncomingNeighbourConsumesTheRemainingForwardClearance()
        {
            CrowdAgent2D follower = Agent(Vector2.zero);
            CrowdAgent2D oncoming = Agent(Vector2.up * 1.32f);
            oncoming.GetComponent<Rigidbody2D>().linearVelocity = Vector2.down * 1.2f;
            Vector2 limited = follower.LimitForwardVelocity(Vector2.up * 2.4f, 0);
            Assert.That(limited.sqrMagnitude, Is.LessThan(.000001f),
                "The oncoming actor already consumes the .02-unit gap within this fixed step.");
        }

        [TestCase(0, -1)]
        [TestCase(1.31f, .1f)]
        public void NeighboursBehindOrOutsideTheForwardCorridorDoNotBlock(float x, float y)
        {
            CrowdAgent2D follower = Agent(Vector2.zero);
            Agent(new Vector2(x, y));
            Vector2 desired = Vector2.up * 2.4f;
            Assert.That(follower.LimitForwardVelocity(desired, 0), Is.EqualTo(desired));
        }

        private CrowdAgent2D Agent(Vector2 position, bool yielding = true, float personalRadius = .65f)
        {
            var item = new GameObject("Temporary crowd test actor");
            SceneManager.MoveGameObjectToScene(item, previewScene);
            objects.Add(item);
            var body = item.AddComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.position = position;
            item.AddComponent<CircleCollider2D>().radius = .44f;
            var agent = item.AddComponent<CrowdAgent2D>();
            var serialized = new SerializedObject(agent);
            serialized.FindProperty("personalRadius").floatValue = personalRadius;
            serialized.FindProperty("yieldToNeighbours").boolValue = yielding;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(CrowdAgent2D).GetMethod("Awake", flags).Invoke(agent, null);
            typeof(CrowdAgent2D).GetMethod("Construct", flags).Invoke(agent, new object[]{registry});
            return agent;
        }
    }
}
