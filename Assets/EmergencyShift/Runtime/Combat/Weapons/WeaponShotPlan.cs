using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>One committed shot; gameplay directions are fixed before cosmetic callbacks.</summary>
    public sealed class WeaponShotPlan
    {
        public WeaponShotRequest Request { get; }
        public int VolleyId { get; }
        public int Damage { get; }
        public float Range { get; }
        public bool EjectsOnShot { get; }
        public ReadOnlyCollection<Vector2> Directions { get; }

        internal WeaponShotPlan(WeaponShotRequest request, int volleyId, WeaponSettings settings,
            bool ejectsOnShot, IEnumerable<Vector2> directions)
        {
            Request = request;
            VolleyId = volleyId;
            Damage = settings.Damage;
            Range = settings.Range;
            EjectsOnShot = ejectsOnShot;
            Directions = new List<Vector2>(directions).AsReadOnly();
        }
    }
}
