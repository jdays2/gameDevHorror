using System;
using EmergencyShift.Core.Randomness;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Immutable simulation tuning. Assets, sprites and audio belong to Unity adapters.</summary>
    public class WeaponSettings
    {
        public string ContentId { get; }
        public int SelectionSlot { get; }
        public int Damage { get; }
        public int MaximumAmmo { get; }
        public float SecondsBetweenShots { get; }
        public float Range { get; }
        public bool Automatic { get; }
        public virtual int ProjectileCount => 1;

        public WeaponSettings(string contentId, int selectionSlot, int damage, int maximumAmmo,
            float secondsBetweenShots, float range, bool automatic)
        {
            if (string.IsNullOrWhiteSpace(contentId) || selectionSlot < 0 || damage < 1 || maximumAmmo < 1
                || !float.IsFinite(secondsBetweenShots) || secondsBetweenShots < .05f
                || !float.IsFinite(range) || range < .5f)
            {
                throw new ArgumentException("Invalid weapon simulation settings.");
            }

            ContentId = contentId;
            SelectionSlot = selectionSlot;
            Damage = damage;
            MaximumAmmo = maximumAmmo;
            SecondsBetweenShots = secondsBetweenShots;
            Range = range;
            Automatic = automatic;
        }

        protected WeaponSettings(WeaponSettings source)
            : this(source.ContentId, source.SelectionSlot, source.Damage, source.MaximumAmmo,
                source.SecondsBetweenShots, source.Range, source.Automatic)
        {
        }

        public virtual WeaponCycle CreateCycle(int cartridges) => new WeaponCycle(this, cartridges);

        public virtual Vector2 ProjectileDirection(Vector2 heading, int index, IRandomSource random)
        {
            if (index != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return heading;
        }
    }
}
