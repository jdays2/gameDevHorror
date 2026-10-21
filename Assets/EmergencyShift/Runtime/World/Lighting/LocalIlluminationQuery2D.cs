using System;
using EmergencyShift.Core.Composition;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmergencyShift
{
    /// <summary>Native visual light queries. These sources never become defensive AI lights.</summary>
    public sealed class LocalIlluminationQuery2D
    {
        private readonly SceneRegistry<LocalLamp2D> lamps;
        private readonly SceneRegistry<ShotEffect2D> effects;

        public LocalIlluminationQuery2D(SceneRegistry<LocalLamp2D> lamps, SceneRegistry<ShotEffect2D> effects)
        {
            this.lamps = lamps ?? throw new ArgumentNullException(nameof(lamps));
            this.effects = effects ?? throw new ArgumentNullException(nameof(effects));
        }

        public bool AnyLampIlluminates(Scene owner, Vector2 point, LayerMask opaque)
        {
            for (int index = 0; index < lamps.Members.Count; index++)
            {
                LocalLamp2D lamp = lamps.Members[index];
                if (lamp != null && lamp.gameObject.scene == owner && lamp.Illuminates(point, opaque))
                {
                    return true;
                }
            }

            return false;
        }

        public bool AnyIlluminates(Scene owner, Vector2 point, LayerMask opaque)
        {
            if (AnyLampIlluminates(owner, point, opaque))
            {
                return true;
            }

            for (int index = 0; index < effects.Members.Count; index++)
            {
                ShotEffect2D effect = effects.Members[index];
                if (effect != null && effect.gameObject.scene == owner && effect.Illuminates(point, opaque))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
