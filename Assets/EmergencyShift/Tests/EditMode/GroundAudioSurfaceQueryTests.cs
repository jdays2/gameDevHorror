using EmergencyShift.Core.Composition;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class GroundAudioSurfaceQueryTests
    {
        private sealed class Region : IGroundAudioRegion
        {
            public GroundSoundSurface Surface { get; }
            public int Priority { get; }
            public bool CoversOrigin { get; set; } = true;

            public Region(GroundSoundSurface surface, int priority)
            {
                Surface = surface;
                Priority = priority;
            }

            public bool Contains(Vector2 point) => CoversOrigin && point == Vector2.zero;
        }

        [Test]
        public void HighestPriorityWinsAndEqualPriorityKeepsFirstRegion()
        {
            using (var regions = new SceneRegistry<IGroundAudioRegion>())
            {
                regions.Register(new Region(GroundSoundSurface.Wood, 0));
                regions.Register(new Region(GroundSoundSurface.Tile, 2));
                regions.Register(new Region(GroundSoundSurface.Concrete, 2));
                Assert.That(new GroundAudioSurfaceQuery2D(regions).At(Vector2.zero), Is.EqualTo(GroundSoundSurface.Tile));
            }
        }

        [Test]
        public void UncoveredAndRemovedRegionsRevealTheUnderlyingSurface()
        {
            using (var regions = new SceneRegistry<IGroundAudioRegion>())
            {
                regions.Register(new Region(GroundSoundSurface.Wood, 0));
                var top = new Region(GroundSoundSurface.Tile, 1);
                regions.Register(top);
                var query = new GroundAudioSurfaceQuery2D(regions);
                top.CoversOrigin = false;
                Assert.That(query.At(Vector2.zero), Is.EqualTo(GroundSoundSurface.Wood));
                top.CoversOrigin = true;
                Assert.That(query.At(Vector2.zero), Is.EqualTo(GroundSoundSurface.Tile));
                regions.Remove(top);
                Assert.That(query.At(Vector2.zero), Is.EqualTo(GroundSoundSurface.Wood));
                regions.Register(top);
                Assert.That(query.At(Vector2.zero), Is.EqualTo(GroundSoundSurface.Tile));
            }
        }

        [Test]
        public void IndependentScopesResolveTheirOwnSurfaceAtTheSamePoint()
        {
            using (var first = new SceneRegistry<IGroundAudioRegion>())
            using (var second = new SceneRegistry<IGroundAudioRegion>())
            {
                first.Register(new Region(GroundSoundSurface.Wood, 0));
                second.Register(new Region(GroundSoundSurface.Tile, 100));
                Assert.That(new GroundAudioSurfaceQuery2D(first).At(Vector2.zero), Is.EqualTo(GroundSoundSurface.Wood));
                Assert.That(new GroundAudioSurfaceQuery2D(second).At(Vector2.zero), Is.EqualTo(GroundSoundSurface.Tile));
            }
        }

        [Test]
        public void OutsideAllRegionsUsesConcreteFallback()
        {
            using (var regions = new SceneRegistry<IGroundAudioRegion>())
            {
                regions.Register(new Region(GroundSoundSurface.Wood, 0));
                Assert.That(new GroundAudioSurfaceQuery2D(regions).At(Vector2.one), Is.EqualTo(GroundSoundSurface.Concrete));
            }
        }

        [Test]
        public void DisposingOneScopeDoesNotClearAnotherScopesSurface()
        {
            var first = new SceneRegistry<IGroundAudioRegion>();
            using (var second = new SceneRegistry<IGroundAudioRegion>())
            {
                first.Register(new Region(GroundSoundSurface.Wood, 0));
                second.Register(new Region(GroundSoundSurface.Tile, 0));
                first.Dispose();
                Assert.That(new GroundAudioSurfaceQuery2D(first).At(Vector2.zero), Is.EqualTo(GroundSoundSurface.Concrete));
                Assert.That(new GroundAudioSurfaceQuery2D(second).At(Vector2.zero), Is.EqualTo(GroundSoundSurface.Tile));
            }
        }
    }
}
