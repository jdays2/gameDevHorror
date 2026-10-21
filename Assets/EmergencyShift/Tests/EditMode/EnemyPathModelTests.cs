using System;
using EmergencyShift.Core.Randomness;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Enemies.Logic;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class EnemyPathModelTests
    {
        [Test]
        public void ClearRouteNeedsNeitherBuildQuotaNorGridAndArrivalStops()
        {
            var schedule = new Schedule();
            var model = new EnemyChasePathModel(new Geometry { WallEnabled = false }, schedule);
            Assert.That(model.Direction(Vector2.zero, Vector2.right * 3, .2f, 256), Is.EqualTo(Vector2.right));
            Assert.That(model.Direction(Vector2.zero, Vector2.right * .1f, .2f, 256), Is.EqualTo(Vector2.zero));
            Assert.That(schedule.Attempts, Is.Zero);
            Assert.That(model.WaypointCount, Is.Zero);
        }

        [Test]
        public void CachedBoundedPathFindsAWayAroundWallWithoutRebuildingEachStep()
        {
            var clock = new Clock();
            var budget = new Budget { Remaining = 500 };
            var schedule = new EnemyPathSchedule(clock, budget, new Random(0));
            var geometry = new Geometry();
            var model = new EnemyChasePathModel(geometry, schedule);
            Vector2 position = Vector2.zero;
            Vector2 goal = Vector2.right * 3;
            bool detoured = false;
            var trace = new System.Text.StringBuilder();
            for (int frame = 0; frame < 150 && Vector2.Distance(position, goal) > .12f; frame++)
            {
                Vector2 direction = model.Direction(position, goal, .15f, 256);
                trace.AppendLine($"{frame}: {position} => {direction}, waypoints {model.WaypointCount}");
                Assert.That(geometry.IsBlocked(position, .15f, direction, .06f, 256), Is.False);
                position += direction * .06f;
                detoured |= Mathf.Abs(position.y) > 1;
                clock.Time += .04;
            }

            Assert.That(Vector2.Distance(position, goal), Is.LessThanOrEqualTo(.12f), trace.ToString());
            Assert.That(detoured, Is.True);
            Assert.That(500 - budget.Remaining, Is.LessThan(20));
        }

        [Test]
        public void MovingGoalInvalidatesCachedRouteAndDirectSightClearsIt()
        {
            var schedule = new Schedule();
            var geometry = new Geometry();
            var model = new EnemyChasePathModel(geometry, schedule);
            model.Direction(Vector2.zero, Vector2.right * 3, .15f, 256);
            Assert.That(model.WaypointCount, Is.GreaterThan(0));
            model.Direction(Vector2.zero, Vector2.right * 7, .15f, 256);
            Assert.That(schedule.Invalidations, Is.EqualTo(1));
            Assert.That(model.WaypointCount, Is.Zero);
            model.Direction(Vector2.zero, Vector2.right * 7, .15f, 256);
            Assert.That(schedule.Builds, Is.EqualTo(2));
            geometry.WallEnabled = false;
            Assert.That(model.Direction(Vector2.zero, Vector2.right * 7, .15f, 256), Is.EqualTo(Vector2.right));
            Assert.That(model.WaypointCount, Is.Zero);
        }

        [TestCase(float.NaN, .2f)]
        [TestCase(float.PositiveInfinity, .2f)]
        [TestCase(3, -1)]
        [TestCase(3, float.NaN)]
        public void InvalidInputIsRejectedBeforeGeometryOrQuota(float goal, float radius)
        {
            var geometry = new Geometry();
            var schedule = new Schedule();
            var model = new EnemyChasePathModel(geometry, schedule);
            Assert.That(model.Direction(Vector2.zero, Vector2.right * goal, radius, 256), Is.EqualTo(Vector2.zero));
            Assert.That(schedule.Attempts + geometry.Queries, Is.Zero);
        }

        [TestCase(39312.5f)]
        [TestCase(1000000)]
        public void OversizedGridDoesNotSampleCellsOrAllocateAWaypointRoute(float extent)
        {
            var geometry = new Geometry { ForceBlocked = true };
            var model = new EnemyChasePathModel(geometry, new Schedule());
            Assert.That(model.Direction(Vector2.zero, Vector2.one * extent, .2f, 256), Is.EqualTo(Vector2.zero));
            Assert.That(geometry.StandChecks, Is.Zero);
            Assert.That(model.WaypointCount, Is.Zero);
        }

        [Test]
        public void FiniteEndpointsCannotSendAnOverflowedDisplacementToGeometry()
        {
            var geometry = new Geometry();
            var schedule = new Schedule();
            var model = new EnemyChasePathModel(geometry, schedule);
            Vector2 direction = model.Direction(Vector2.left * 1e19f, Vector2.right * 1e19f, .2f, 256);
            Assert.That(direction, Is.EqualTo(Vector2.zero));
            Assert.That(geometry.Queries + schedule.Attempts, Is.Zero);
        }

        [Test]
        public void TwoActorsDoNotShareRouteCursorOrInvalidation()
        {
            var geometry = new Geometry();
            var first = new EnemyChasePathModel(geometry, new Schedule());
            var second = new EnemyChasePathModel(geometry, new Schedule());
            first.Direction(Vector2.zero, Vector2.right * 3, .15f, 256);
            second.Direction(Vector2.zero, Vector2.right * 3, .15f, 256);
            int secondCount = second.WaypointCount;
            first.Direction(Vector2.zero, Vector2.right * 7, .15f, 256);
            Assert.That(first.WaypointCount, Is.Zero);
            Assert.That(second.WaypointCount, Is.EqualTo(secondCount));
        }

        [Test]
        public void ScheduleUsesSharedQuotaButPersonalTimeAndJitter()
        {
            var clock = new Clock();
            var budget = new Budget();
            var first = new EnemyPathSchedule(clock, budget, new Random(0));
            var second = new EnemyPathSchedule(clock, budget, new Random(.5));
            Assert.That(first.TryBeginBuild(), Is.True);
            first.CompleteBuild();
            Assert.That(second.TryBeginBuild(), Is.True);
            second.CompleteBuild();
            Assert.That(new EnemyPathSchedule(clock, budget, new Random(0)).TryBeginBuild(), Is.False);
            budget.Remaining = 2;
            clock.Time = .649;
            Assert.That(first.TryBeginBuild(), Is.False);
            clock.Time = .65;
            Assert.That(first.TryBeginBuild(), Is.True);
            Assert.That(second.TryBeginBuild(), Is.False);
            clock.Time = .711;
            Assert.That(second.TryBeginBuild(), Is.True);
        }

        [Test]
        public void InvalidClockDoesNotReserveAndInvalidationAllowsAnEarlyRetry()
        {
            var clock = new Clock { Time = double.NaN };
            var budget = new Budget();
            var schedule = new EnemyPathSchedule(clock, budget, new Random(0));
            Assert.That(schedule.TryBeginBuild(), Is.False);
            Assert.That(budget.Remaining, Is.EqualTo(2));
            clock.Time = 0;
            Assert.That(schedule.TryBeginBuild(), Is.True);
            schedule.CompleteBuild();
            Assert.That(schedule.TryBeginBuild(), Is.False);
            schedule.Invalidate();
            Assert.That(schedule.TryBeginBuild(), Is.True);
        }

        private sealed class Schedule : IEnemyPathSchedule
        {
            public int Attempts;
            public int Builds;
            public int Invalidations;
            private bool ready = true;
            public bool TryBeginBuild()
            {
                Attempts++;
                return ready;
            }

            public void CompleteBuild()
            {
                Builds++;
                ready = false;
            }

            public void Invalidate()
            {
                Invalidations++;
                ready = true;
            }
        }

        private sealed class Geometry : IEnemyPathGeometry
        {
            public bool WallEnabled = true;
            public bool ForceBlocked;
            public int Queries;
            public int StandChecks;
            public bool CanStand(Vector2 position, float radius, int solids)
            {
                StandChecks++;
                return !ForceBlocked && (!WallEnabled || DistanceToWall(position) > radius * radius);
            }

            public bool LineClear(Vector2 from, Vector2 to, int solids) =>
                !IsBlocked(from, 0, (to - from).normalized, Vector2.Distance(from, to), solids);
            public bool IsBlocked(Vector2 position, float radius, Vector2 direction, float distance, int solids)
            {
                Queries++;
                if (ForceBlocked)
                {
                    return true;
                }

                if (!WallEnabled)
                {
                    return false;
                }

                Rect rect = Wall;
                Vector2 end = position + direction * distance;
                if (rect.Contains(position) || rect.Contains(end))
                {
                    return true;
                }

                var bounds = new Bounds(rect.center, new Vector3(rect.width, rect.height, 2));
                if (bounds.IntersectRay(new Ray(position, direction), out float hit) && hit <= distance)
                {
                    return true;
                }

                // Exact rounded corner clearance for a circle; an expanded square incorrectly traps it.
                float nearest = Mathf.Min(DistanceToWall(position), DistanceToWall(end));
                nearest = Mathf.Min(nearest, DistanceToSegment(rect.min, position, end));
                nearest = Mathf.Min(nearest, DistanceToSegment(rect.max, position, end));
                nearest = Mathf.Min(nearest, DistanceToSegment(new Vector2(rect.xMin, rect.yMax), position, end));
                nearest = Mathf.Min(nearest, DistanceToSegment(new Vector2(rect.xMax, rect.yMin), position, end));
                return nearest <= radius * radius;
            }

            private static Rect Wall => Rect.MinMaxRect(1, -1, 1.2f, 1);
            private static float DistanceToWall(Vector2 point)
            {
                Rect rect = Wall;
                var closest = new Vector2(Mathf.Clamp(point.x, rect.xMin, rect.xMax), Mathf.Clamp(point.y, rect.yMin, rect.yMax));
                return (point - closest).sqrMagnitude;
            }

            private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
            {
                Vector2 delta = end - start;
                float t = delta.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(point - start, delta) / delta.sqrMagnitude) : 0;
                return (point - (start + delta * t)).sqrMagnitude;
            }
        }

        private sealed class Clock : IGameClock
        {
            public double Time { get; set; }
        }

        private sealed class Budget : IEnemyPathBuildBudget
        {
            public int Remaining = 2;
            public bool TryReserve() => Remaining-- > 0;
        }

        private sealed class Random : IRandomSource
        {
            private readonly double unit;
            public Random(double unit) => this.unit = unit;
            public double NextUnit() => unit;
            public int NextInt(int min, int max) => min;
        }
    }
}
