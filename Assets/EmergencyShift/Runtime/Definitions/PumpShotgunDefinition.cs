using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Shared shotgun tuning. Each owned weapon gets its own cycle and ammunition.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Weapons/Pump shotgun")]
    public sealed class PumpShotgunDefinition : WeaponDefinition
    {
        [Min(1)]
        public int magazineCapacity = 4;
        [Min(2)]
        public int pellets = 8;
        [Range(1, 80)]
        public float spreadDegrees = 18;
        [Min(.05f)]
        public float pumpSeconds = .85f;
        [Min(0)]
        public float pumpCueDelay = .16f;
        [Min(.05f)]
        public float shellInsertSeconds = .8f;
        public override int ProjectileCount => pellets;
        public override Vector3 HeldScale => new Vector3(1.15f, 1.8f, 1);

        public override WeaponSettings ToSettings()
        {
            Validate();
            var magazine = new MagazineWeaponSettings(base.ToSettings(), magazineCapacity, shellInsertSeconds);
            return new PumpShotgunSettings(magazine, pellets, spreadDegrees, pumpSeconds, pumpCueDelay);
        }

        public override void Validate()
        {
            base.Validate();
            if (magazineCapacity < 1 || magazineCapacity > maximumAmmo || pellets < 2 || pellets > 32 ||
                !float.IsFinite(spreadDegrees) || spreadDegrees <= 0 || spreadDegrees > 80 ||
                !float.IsFinite(pumpSeconds) || pumpSeconds < .05f ||
                !float.IsFinite(pumpCueDelay) || pumpCueDelay < 0 || pumpCueDelay >= pumpSeconds ||
                !float.IsFinite(shellInsertSeconds) || shellInsertSeconds < .05f || automatic)
            {
                throw new System.InvalidOperationException("Invalid pump shotgun tuning: " + name);
            }
        }
    }
}
