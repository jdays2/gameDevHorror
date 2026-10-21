using EmergencyShift.Core.Randomness;
using EmergencyShift.Core.Time;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class EnemyNavigationSafetyTests
    {
        [Test]
        public void OversizedRouteCannotOverflowTheBoundedGrid()
        {
            var wall = new GameObject("Bounded path obstruction");
            try
            {
                wall.layer = 8;
                wall.transform.position = Vector2.one;
                wall.AddComponent<BoxCollider2D>().size = Vector2.one;
                Physics2D.SyncTransforms();
                var clock = new Clock();
                var path = new EnemyChasePathFactory(clock, new NavigationWorkBudget2D(clock),
                    new RandomSourceFactory(new System.Random(15))).Create();
                Vector2 direction = Vector2.one;
                Assert.DoesNotThrow(() => direction = path.Direction(Vector2.zero, Vector2.one * 39312.5f, .3f, 1 << 8));
                Assert.That(direction, Is.EqualTo(Vector2.zero));
                Assert.That(path.WaypointCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(wall);
            }
        }

        private sealed class Clock : IGameClock, IFrameClock
        {
            public double Time => 0;
            public int Frame => 0;
        }
    }
}
