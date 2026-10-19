using System;
using EmergencyShift.Core.Composition;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Selects a scene-owned region; geometry stays in the adapter and ties retain registration order.</summary>
    public sealed class GroundAudioSurfaceQuery2D
    {
        private readonly SceneRegistry<IGroundAudioRegion> regions;

        public GroundAudioSurfaceQuery2D(SceneRegistry<IGroundAudioRegion> regions)
        {
            this.regions = regions ?? throw new ArgumentNullException(nameof(regions));
        }

        public GroundSoundSurface At(Vector2 point)
        {
            IGroundAudioRegion best = null;
            foreach (IGroundAudioRegion region in regions.Members)
            {
                if (region != null && region.Contains(point) && (best == null || region.Priority > best.Priority))
                {
                    best = region;
                }
            }

            return best != null ? best.Surface : GroundSoundSurface.Concrete;
        }
    }
}
